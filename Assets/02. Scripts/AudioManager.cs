using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BGM과 효과음을 한곳에서 재생합니다. 다른 스크립트에서 AudioManager.instance로 호출하세요.
/// 클립 이름 끝의 _01, _02 같은 번호는 같은 소리의 변형으로 묶입니다.
/// 그래서 PlaySFX("sfx_jump")처럼 번호를 떼고 부르면 그중 하나가 무작위로 나오고,
/// PlaySFX("sfx_jump_02")처럼 전체 이름을 주면 그 클립만 나옵니다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    const string MASTER_PREF = "audio.master";
    const string BGM_PREF = "audio.bgm";
    const string SFX_PREF = "audio.sfx";
    const string LogoSceneName = "LogoScene";

    [Header("클립 등록")]
    [Tooltip("06. Audio/BGM 폴더의 클립을 전체 선택해 한 번에 끌어다 놓으면 됩니다.")]
    [SerializeField] AudioClip[] bgmClips;
    [Tooltip("06. Audio/SFX 폴더의 클립을 전체 선택해 한 번에 끌어다 놓으면 됩니다.")]
    [SerializeField] AudioClip[] sfxClips;

    [Header("기본 BGM")]
    [Tooltip("로고를 제외한 씬에서 기본적으로 재생할 곡입니다. 비우면 아래 키로 찾습니다.")]
    [SerializeField] AudioClip defaultBgm;
    [Tooltip("defaultBgm이 비어 있을 때 bgmClips에서 찾을 이름입니다.")]
    [SerializeField] string defaultBgmKey = BGMKey.Main;

    [Header("음량")]
    [Range(0f, 1f)] [SerializeField] float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] float bgmVolume = 0.5f;
    [Range(0f, 1f)] [SerializeField] float sfxVolume = 1f;
    [Tooltip("음량을 PlayerPrefs에 저장해 다음 실행에도 유지합니다.")]
    [SerializeField] bool saveVolume = true;

    [Header("재생 설정")]
    [Tooltip("효과음이 동시에 겹칠 수 있는 최대 개수입니다. 넘치면 가장 오래 쓰인 채널을 재활용합니다.")]
    [SerializeField] int sfxChannelCount = 12;
    [Tooltip("BGM을 바꿀 때 겹쳐 넘기는 기본 시간(초)입니다.")]
    [SerializeField] float defaultFadeDuration = 1f;
    [Tooltip("효과음 음높이를 이 범위에서 무작위로 흔듭니다. (1, 1)이면 원음 그대로 재생합니다.")]
    [SerializeField] Vector2 sfxPitchRange = Vector2.one;

    readonly Dictionary<string, List<AudioClip>> bgmTable =
        new Dictionary<string, List<AudioClip>>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, List<AudioClip>> sfxTable =
        new Dictionary<string, List<AudioClip>>(StringComparer.OrdinalIgnoreCase);
    // 같은 변형이 연달아 나오지 않게 직전에 고른 번호를 기억합니다.
    readonly Dictionary<string, int> lastVariation =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    // 크로스페이드를 하려면 이전 곡과 새 곡이 잠깐 함께 울려야 하므로 두 개를 씁니다.
    /// <summary>음량 설정이 바뀔 때 다시 계산할 수 있도록 배율을 함께 들고 있습니다.</summary>
    class LoopChannel
    {
        public AudioSource source;
        public float scale;
    }

    readonly AudioSource[] bgmSources = new AudioSource[2];
    readonly float[] fadeStart = new float[2];
    // 이어지는 소리는 일회성 풀에 섞이면 다른 소리에 밀려 끊기므로 따로 둡니다.
    readonly List<LoopChannel> loops = new List<LoopChannel>();
    AudioSource[] sfxChannels;
    AudioClip targetBgm;
    int activeBgm;
    int nextChannel;
    Coroutine bgmFade;

    public float MasterVolume => masterVolume;
    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;
    public bool IsBGMPlaying => bgmSources[activeBgm] != null && bgmSources[activeBgm].isPlaying;
    /// <summary>지금 흐르고 있는 BGM 클립입니다. 없으면 null입니다.</summary>
    public AudioClip CurrentBGM => bgmSources[activeBgm] != null ? bgmSources[activeBgm].clip : null;

    float BgmTargetVolume => bgmVolume * masterVolume;

    void Awake()
    {
        if (instance == null) instance = this;
        if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        LoadVolume();
        BuildTable(bgmTable, bgmClips);
        BuildTable(sfxTable, sfxClips);
        CreateSources();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // 첫 씬은 sceneLoaded가 이미 지나간 뒤라 Start에서 한 번 맞춰 줍니다.
        ApplyBgmForScene(SceneManager.GetActiveScene().name, 0f);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyBgmForScene(scene.name, defaultFadeDuration);
    }

    /// <summary>
    /// 로고에서는 조용히 두고, 그 외 씬에서는 기본 BGM을 켭니다.
    /// 스테이지 연출로 바꾼 곡도 타이틀처럼 기본이 필요한 씬으로 돌아오면 다시 기본으로 돌아갑니다.
    /// </summary>
    void ApplyBgmForScene(string sceneName, float fadeDuration)
    {
        if (string.Equals(sceneName, LogoSceneName, StringComparison.OrdinalIgnoreCase))
        {
            StopBGM(fadeDuration);
            return;
        }

        PlayDefaultBGM(fadeDuration);
    }

    #region BGM

    /// <summary>로고를 제외한 씬에서 쓰는 기본 BGM을 재생합니다.</summary>
    public void PlayDefaultBGM()
    {
        PlayDefaultBGM(defaultFadeDuration);
    }

    public void PlayDefaultBGM(float fadeDuration)
    {
        if (defaultBgm != null)
        {
            PlayBGM(defaultBgm, fadeDuration);
            return;
        }

        PlayBGM(defaultBgmKey, fadeDuration);
    }

    /// <summary>기본 페이드 시간으로 BGM을 바꿉니다.</summary>
    public void PlayBGM(string key)
    {
        PlayBGM(key, defaultFadeDuration);
    }

    /// <summary>fadeDuration을 0으로 주면 즉시 바뀝니다.</summary>
    public void PlayBGM(string key, float fadeDuration)
    {
        PlayBGM(Resolve(bgmTable, key), fadeDuration);
    }

    /// <summary>등록하지 않은 클립을 직접 넘겨 재생할 때 씁니다. StageScriptable.Bgm처럼 씁니다.</summary>
    public void PlayBGM(AudioClip clip)
    {
        PlayBGM(clip, defaultFadeDuration);
    }

    /// <summary>등록하지 않은 클립을 직접 넘겨 재생할 때 씁니다.</summary>
    public void PlayBGM(AudioClip clip, float fadeDuration)
    {
        if (clip == null) return;

        // 같은 곡을 다시 요청하면 처음부터 되감지 않고 그대로 이어 갑니다.
        // 지금 재생 중인 클립이 아니라 목표 클립과 비교해야, 페이드 아웃 도중에 같은 곡을
        // 다시 요청했을 때 소리가 그대로 사라져 버리는 일이 없습니다.
        if (targetBgm == clip && bgmSources[activeBgm] != null && bgmSources[activeBgm].isPlaying) return;

        AudioSource incoming = bgmSources[1 - activeBgm];
        incoming.clip = clip;
        incoming.loop = true;
        incoming.volume = 0f;
        incoming.Play();

        targetBgm = clip;
        activeBgm = 1 - activeBgm;
        RunBgmFade(CrossFade(incoming, fadeDuration));
    }

    public void StopBGM()
    {
        StopBGM(defaultFadeDuration);
    }

    public void StopBGM(float fadeDuration)
    {
        targetBgm = null;
        RunBgmFade(CrossFade(null, fadeDuration));
    }

    /// <summary>일시정지 메뉴처럼 잠깐 멈췄다가 같은 지점에서 이어 갈 때 씁니다.</summary>
    public void PauseBGM()
    {
        bgmSources[activeBgm].Pause();
    }

    public void ResumeBGM()
    {
        bgmSources[activeBgm].UnPause();
    }

    /// <summary>
    /// to는 키우고 나머지 채널은 모두 줄여서 정리합니다.
    /// 앞선 페이드가 중간에 끊겨 남아 있던 소리도 이 규칙에 걸려 같이 사라집니다.
    /// </summary>
    IEnumerator CrossFade(AudioSource to, float duration)
    {
        for (int i = 0; i < bgmSources.Length; i++)
            fadeStart[i] = bgmSources[i] == to ? 0f : bgmSources[i].volume;

        // timeScale이 0인 일시정지 중에도 페이드가 진행돼야 합니다.
        // 목표 음량은 매 프레임 다시 읽어, 페이드 도중에 음량 슬라이더를 움직여도 바로 반영됩니다.
        if (duration <= 0f)
        {
            FinishCrossFade(to);
            yield break;
        }

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;

            if (to != null) to.volume = BgmTargetVolume * k;

            for (int i = 0; i < bgmSources.Length; i++)
            {
                if (bgmSources[i] == to) continue;

                bgmSources[i].volume = fadeStart[i] * (1f - k);
            }

            yield return null;
        }

        FinishCrossFade(to);
    }

    void FinishCrossFade(AudioSource to)
    {
        if (to != null) to.volume = BgmTargetVolume;

        for (int i = 0; i < bgmSources.Length; i++)
        {
            if (bgmSources[i] == to) continue;

            bgmSources[i].Stop();
            bgmSources[i].clip = null;
            bgmSources[i].volume = 0f;
        }

        bgmFade = null;
    }

    void RunBgmFade(IEnumerator routine)
    {
        if (bgmFade != null) StopCoroutine(bgmFade);

        bgmFade = StartCoroutine(routine);
    }

    #endregion

    #region SFX

    /// <summary>
    /// 2D 효과음을 재생하는 정적 단축 호출입니다.
    /// 씬에 AudioManager를 아직 두지 않았어도 조용히 넘어가므로, 부르는 쪽에서 null을 확인할 필요가 없습니다.
    /// </summary>
    public static void Play(string key, float volumeScale = 1f)
    {
        if (instance != null) instance.PlaySFX(key, volumeScale);
    }

    /// <summary>지정한 위치에서 3D 효과음을 재생하는 정적 단축 호출입니다.</summary>
    public static void PlayAt(string key, Vector3 position, float volumeScale = 1f)
    {
        if (instance != null) instance.PlaySFXAt(key, position, volumeScale);
    }

    /// <summary>화면 어디서 나든 같은 크기로 들리는 2D 효과음입니다. UI나 플레이어 소리에 씁니다.</summary>
    public AudioSource PlaySFX(string key, float volumeScale = 1f)
    {
        return Play(Resolve(sfxTable, key), volumeScale, false, Vector3.zero);
    }

    /// <summary>지정한 위치에서 나는 3D 효과음입니다. 거리에 따라 작아집니다.</summary>
    public AudioSource PlaySFXAt(string key, Vector3 position, float volumeScale = 1f)
    {
        return Play(Resolve(sfxTable, key), volumeScale, true, position);
    }

    /// <summary>등록하지 않은 클립을 직접 넘겨 재생할 때 씁니다.</summary>
    public AudioSource PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        return Play(clip, volumeScale, false, Vector3.zero);
    }

    public void StopAllSFX()
    {
        foreach (AudioSource channel in sfxChannels) channel.Stop();

        foreach (LoopChannel loop in loops) StopLoop(loop.source);
    }

    /// <summary>
    /// 발소리처럼 상태가 이어지는 동안 계속 나야 하는 소리입니다.
    /// 스스로 끝나지 않으므로, 돌려받은 AudioSource를 StopLoop에 넘겨 반드시 멈춰 주세요.
    /// 일회성 효과음 풀과 분리된 채널을 쓰기 때문에 다른 소리에 밀려 끊기지 않습니다.
    /// </summary>
    public AudioSource PlayLoop(string key, float volumeScale = 1f)
    {
        AudioClip clip = Resolve(sfxTable, key);
        if (clip == null) return null;

        LoopChannel loop = GetLoopChannel();
        loop.scale = volumeScale;

        AudioSource source = loop.source;
        source.clip = clip;
        source.loop = true;
        // 이어 붙는 지점이 티나지 않도록 루프에는 음높이를 흔들지 않습니다.
        source.pitch = 1f;
        source.spatialBlend = 0f;
        source.volume = Mathf.Clamp01(volumeScale * sfxVolume * masterVolume);
        source.Play();

        return source;
    }

    /// <summary>PlayLoop으로 받은 채널을 멈춥니다. null을 넘겨도 안전합니다.</summary>
    public void StopLoop(AudioSource source)
    {
        if (source == null) return;

        source.Stop();
        source.clip = null;
    }

    LoopChannel GetLoopChannel()
    {
        foreach (LoopChannel loop in loops)
        {
            if (!loop.source.isPlaying) return loop;
        }

        LoopChannel created = new LoopChannel { source = CreateSource($"Loop {loops.Count}") };
        loops.Add(created);

        return created;
    }

    AudioSource Play(AudioClip clip, float volumeScale, bool spatial, Vector3 position)
    {
        if (clip == null) return null;

        AudioSource channel = GetChannel();
        channel.Stop();
        channel.clip = clip;
        channel.loop = false;
        channel.volume = Mathf.Clamp01(volumeScale * sfxVolume * masterVolume);
        channel.pitch = UnityEngine.Random.Range(sfxPitchRange.x, sfxPitchRange.y);
        channel.spatialBlend = spatial ? 1f : 0f;

        if (spatial) channel.transform.position = position;
        else channel.transform.localPosition = Vector3.zero;

        channel.Play();

        return channel;
    }

    /// <summary>비어 있는 채널을 우선 고르고, 모두 쓰이는 중이면 가장 오래 잡고 있던 채널을 빼앗습니다.</summary>
    AudioSource GetChannel()
    {
        for (int i = 0; i < sfxChannels.Length; i++)
        {
            int index = (nextChannel + i) % sfxChannels.Length;

            if (sfxChannels[index].isPlaying) continue;

            nextChannel = (index + 1) % sfxChannels.Length;
            return sfxChannels[index];
        }

        AudioSource oldest = sfxChannels[nextChannel];
        nextChannel = (nextChannel + 1) % sfxChannels.Length;

        return oldest;
    }

    #endregion

    #region 음량

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    void ApplyVolume()
    {
        // 페이드 중이면 코루틴이 목표 음량까지 직접 끌고 가므로 건드리지 않습니다.
        if (bgmFade == null && bgmSources[activeBgm] != null) bgmSources[activeBgm].volume = BgmTargetVolume;

        // 이미 흐르고 있는 소리는 다시 재생될 일이 없으니 지금 음량을 고쳐 줘야 합니다.
        foreach (LoopChannel loop in loops)
        {
            if (loop.source.isPlaying) loop.source.volume = Mathf.Clamp01(loop.scale * sfxVolume * masterVolume);
        }

        if (!saveVolume) return;

        PlayerPrefs.SetFloat(MASTER_PREF, masterVolume);
        PlayerPrefs.SetFloat(BGM_PREF, bgmVolume);
        PlayerPrefs.SetFloat(SFX_PREF, sfxVolume);
    }

    void LoadVolume()
    {
        if (!saveVolume) return;

        masterVolume = PlayerPrefs.GetFloat(MASTER_PREF, masterVolume);
        bgmVolume = PlayerPrefs.GetFloat(BGM_PREF, bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(SFX_PREF, sfxVolume);
    }

    #endregion

    #region 초기화

    void CreateSources()
    {
        for (int i = 0; i < bgmSources.Length; i++)
        {
            bgmSources[i] = CreateSource($"BGM {i}");
            bgmSources[i].loop = true;
            // 배경음은 카메라 위치와 무관하게 같은 크기로 들려야 합니다.
            bgmSources[i].spatialBlend = 0f;
        }

        sfxChannels = new AudioSource[Mathf.Max(sfxChannelCount, 1)];

        for (int i = 0; i < sfxChannels.Length; i++) sfxChannels[i] = CreateSource($"SFX {i}");
    }

    AudioSource CreateSource(string sourceName)
    {
        GameObject holder = new GameObject(sourceName);
        holder.transform.SetParent(transform, false);

        AudioSource source = holder.AddComponent<AudioSource>();
        source.playOnAwake = false;

        return source;
    }

    void BuildTable(Dictionary<string, List<AudioClip>> table, AudioClip[] clips)
    {
        if (clips == null) return;

        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;

            // 전체 이름으로도, 번호를 뗀 이름으로도 찾을 수 있게 양쪽에 등록합니다.
            Add(table, clip.name, clip);

            string group = StripVariation(clip.name);
            if (group != clip.name) Add(table, group, clip);
        }
    }

    static void Add(Dictionary<string, List<AudioClip>> table, string key, AudioClip clip)
    {
        if (!table.TryGetValue(key, out List<AudioClip> list))
        {
            list = new List<AudioClip>();
            table[key] = list;
        }

        if (!list.Contains(clip)) list.Add(clip);
    }

    /// <summary>"sfx_jump_02"에서 "sfx_jump"를 얻습니다. 번호가 없으면 원래 이름을 그대로 돌려줍니다.</summary>
    static string StripVariation(string clipName)
    {
        int separator = clipName.LastIndexOf('_');
        if (separator <= 0 || separator == clipName.Length - 1) return clipName;

        for (int i = separator + 1; i < clipName.Length; i++)
        {
            if (!char.IsDigit(clipName[i])) return clipName;
        }

        return clipName.Substring(0, separator);
    }

    #endregion

    AudioClip Resolve(Dictionary<string, List<AudioClip>> table, string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (!table.TryGetValue(key, out List<AudioClip> list) || list.Count == 0)
        {
            Debug.LogWarning($"{name}: '{key}' 이름으로 등록된 오디오 클립이 없습니다.", this);
            return null;
        }

        if (list.Count == 1) return list[0];

        int index;

        if (lastVariation.TryGetValue(key, out int last) && last < list.Count)
        {
            // 직전 번호를 뺀 나머지에서 고릅니다.
            index = UnityEngine.Random.Range(0, list.Count - 1);
            if (last <= index) index++;
        }
        else
        {
            index = UnityEngine.Random.Range(0, list.Count);
        }

        lastVariation[key] = index;

        return list[index];
    }

    void OnValidate()
    {
        sfxChannelCount = Mathf.Max(sfxChannelCount, 1);
        defaultFadeDuration = Mathf.Max(defaultFadeDuration, 0f);
        sfxPitchRange.x = Mathf.Clamp(sfxPitchRange.x, 0.1f, 3f);
        sfxPitchRange.y = Mathf.Clamp(sfxPitchRange.y, sfxPitchRange.x, 3f);

        if (Application.isPlaying && instance == this) ApplyVolume();
    }
}
