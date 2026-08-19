using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 승화 상태로 닿으면 커튼 Close → Screen 준비 → 커튼 Open → 영상 재생,
/// 끝나면 다시 커튼 Close 후 EndingScene으로 넘어갑니다.
/// </summary>
public class SublimationObstacle : SublimationGate
{
    [Tooltip("씬에 둔 VideoPlayer입니다. Render Texture를 Screen RawImage에 연결해 두세요.")]
    [SerializeField] VideoPlayer videoPlayer;
    [Tooltip("비활성 상태인 Screen UI(RawImage 등)입니다. 커튼이 닫힌 뒤에 켭니다.")]
    [SerializeField] GameObject screenUI;
    [Tooltip("VideoPlayer.clip이 비어 있을 때 쓸 클립입니다.")]
    [SerializeField] VideoClip videoClip;
    [Tooltip("한 번만 재생할지 여부입니다.")]
    [SerializeField] bool playOnce = true;
    [Tooltip("Play 시작 시 바로 영상→엔딩 흐름을 테스트합니다. 본편 전에는 끄세요.")]
    [SerializeField] bool playOnStart;
    [Tooltip("영상 끝으로 판정할 여유(초)입니다.")]
    [SerializeField] float endEpsilon = 0.08f;

    bool played;
    bool goingToEnding;
    bool playbackStarted;

    protected override void Awake()
    {
        base.Awake();

        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;
    }

    void Start()
    {
        if (playOnStart)
            StartCoroutine(PlayOnStartRoutine());
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    protected override void OnSublimationTouch(PlayerController pc)
    {
        StartCoroutine(PlayEndingSequence());
    }

    IEnumerator PlayOnStartRoutine()
    {
        while (SceneLoader.IsLoading)
            yield return null;

        yield return null;
        yield return PlayEndingSequence();
    }

    IEnumerator PlayEndingSequence()
    {
        if (playOnce && played) yield break;
        if (goingToEnding) yield break;

        if (videoPlayer == null)
        {
            Debug.LogWarning($"{name}: VideoPlayer가 없어 영상을 재생하지 못했습니다.", this);
            yield break;
        }

        if (videoClip != null)
            videoPlayer.clip = videoClip;

        if (videoPlayer.clip == null)
        {
            Debug.LogWarning($"{name}: VideoClip이 없습니다. VideoPlayer 또는 videoClip 슬롯을 확인하세요.", this);
            yield break;
        }

        played = true;
        playbackStarted = false;

        // 1) 커튼 닫기 — 스크린/영상이 보이기 전에 가립니다.
        if (UILoadingPanel.instance == null)
        {
            Debug.LogWarning($"{name}: UILoadingPanel.instance가 없습니다. Load Canvas에 UILoadingPanel이 있는지 확인하세요.", this);
        }
        else
        {
            Coroutine close = UILoadingPanel.instance.CloseAndHold();
            if (close != null) yield return close;
        }

        // 2) 커튼이 닫힌 동안 Screen을 켜고 첫 프레임까지 준비합니다.
        if (screenUI != null)
            screenUI.SetActive(true);

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.Stop();
        videoPlayer.time = 0;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;

        // 첫 프레임을 그려 둔 뒤 멈춥니다. Open 때 검은 화면이 안 나오게 합니다.
        videoPlayer.Play();
        videoPlayer.Pause();
        videoPlayer.time = 0;

        // 3) 커튼 열기 — 준비된 Screen/영상이 드러납니다.
        if (UILoadingPanel.instance != null)
        {
            Coroutine open = UILoadingPanel.instance.Open();
            if (open != null) yield return open;
        }

        // 4) 본 재생
        videoPlayer.Play();

        float startDeadline = Time.unscaledTime + 5f;
        while (!videoPlayer.isPlaying && Time.unscaledTime < startDeadline)
            yield return null;

        if (!videoPlayer.isPlaying)
        {
            Debug.LogWarning($"{name}: 영상 재생을 시작하지 못했습니다.", this);
            yield return GoToEndingRoutine();
            yield break;
        }

        playbackStarted = true;

        double length = videoPlayer.clip != null ? videoPlayer.clip.length : videoPlayer.length;
        if (length <= 0.01)
            length = videoPlayer.length;

        while (!goingToEnding)
        {
            double t = videoPlayer.time;

            if (length > 0.01 && t >= length - endEpsilon)
                break;

            if (playbackStarted && !videoPlayer.isPlaying && t > 0.25)
                break;

            yield return null;
        }

        if (!goingToEnding)
            yield return GoToEndingRoutine();
    }

    void OnVideoFinished(VideoPlayer source)
    {
        if (!playbackStarted || goingToEnding) return;
        StartCoroutine(GoToEndingRoutine());
    }

    IEnumerator GoToEndingRoutine()
    {
        if (goingToEnding) yield break;
        goingToEnding = true;

        // 5) 영상 종료 → 커튼 닫기 → EndingScene
        if (UILoadingPanel.instance != null)
        {
            Coroutine close = UILoadingPanel.instance.CloseAndHold();
            if (close != null) yield return close;
        }

        if (screenUI != null)
            screenUI.SetActive(false);

        SceneLoader.Load(GameScene.Ending);
    }
}
