using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class StagePanel : MonoBehaviour
{
    public static StagePanel instance;

    [System.Serializable]
    public class StageEntry
    {
        [Tooltip("Stage Data 에셋입니다.")]
        public StageScriptable data;
        [Tooltip("첫 조각 획득 시 플레이어가 이동할 컷씬 스폰 오브젝트입니다.")]
        public Transform cutsceneSpawn;
        [Tooltip("카메라 기준점입니다. 비우면 cutsceneSpawn을 씁니다.")]
        public Transform cutsceneCameraCenter;
        public bool tallRoom;
        [Tooltip("이 스테이지 Timeline을 재생할 PlayableDirector입니다. 비우면 아래 공용 Director를 씁니다.")]
        public PlayableDirector timelineDirector;
    }

    [Tooltip("스테이지 Data와 컷씬 스폰을 같이 등록합니다.")]
    [SerializeField] List<StageEntry> stages = new List<StageEntry>();

    // 예전 List<StageScriptable> 직렬화를 stages로 옮기기 위한 필드입니다.
    [SerializeField, HideInInspector] List<StageScriptable> stageScriptable = new List<StageScriptable>();

    [Tooltip("스테이지별 Director가 없을 때 쓰는 공용 PlayableDirector입니다.")]
    [SerializeField] PlayableDirector sharedTimelineDirector;

    [SerializeField] TextMeshProUGUI stageNumberText;
    [SerializeField] TextMeshProUGUI stageNameText;

    [SerializeField] CanvasGroup canvasGroup;

    [Header("연출")]
    [SerializeField] float panelFadeDuration = 0.5f;
    [Tooltip("패널이 다 나타난 뒤, 텍스트가 뜨기까지 기다리는 시간(초)입니다.")]
    [SerializeField] float textDelay = 1f;
    [SerializeField] float textFadeDuration = 0.5f;
    [Tooltip("텍스트가 다 나타난 뒤, 패널이 사라지기까지 기다리는 시간(초)입니다.")]
    [SerializeField] float panelOutDelay = 1f;

    Tween panelTween;
    Tween numberTween;
    Tween nameTween;
    Coroutine running;
    PlayableDirector activeTimelineDirector;

    void Awake()
    {
        if (instance == null) instance = this;
        if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        MigrateLegacyStages();
    }

    void OnValidate()
    {
        MigrateLegacyStages();
    }

    void MigrateLegacyStages()
    {
        if (stageScriptable == null || stageScriptable.Count == 0) return;
        if (stages != null && stages.Count > 0) return;

        if (stages == null) stages = new List<StageEntry>();

        for (int i = 0; i < stageScriptable.Count; i++)
        {
            stages.Add(new StageEntry { data = stageScriptable[i] });
        }

        stageScriptable.Clear();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void SetStageInfo(int stageNumber, string stageName)
    {
        stageNumberText.text = "Stage " + stageNumber.ToString();
        stageNameText.text = stageName;
    }

    /// <summary>
    /// stageNumber에 맞는 StageScriptable을 찾아 연출을 재생합니다.
    /// AbilityManager.GetItem처럼 능력 인덱스를 넘기면 됩니다.
    /// </summary>
    public void StartStageStartAnimation(int stageNumber)
    {
        StageEntry entry = FindEntry(stageNumber);
        if (entry == null || entry.data == null)
        {
            Debug.LogWarning($"{name}: Stage {stageNumber} 데이터가 없습니다. stages 목록을 확인하세요.", this);
            return;
        }

        StageScriptable data = entry.data;
        SetStageInfo(data.StageNumber, data.StageName);

        if (data.Bgm != null && AudioManager.instance != null)
            AudioManager.instance.PlayBGM(data.Bgm);

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(StageStartRoutine());
    }

    /// <summary>컷씬 텔레포트용 스폰 Transform을 반환합니다. 없으면 null입니다.</summary>
    public Transform GetCutsceneSpawn(int stageNumber)
    {
        StageEntry entry = FindEntry(stageNumber);
        return entry != null ? entry.cutsceneSpawn : null;
    }

    /// <summary>컷씬 카메라 기준 Transform을 반환합니다. 없으면 스폰을 씁니다.</summary>
    public Transform GetCutsceneCameraCenter(int stageNumber)
    {
        StageEntry entry = FindEntry(stageNumber);
        if (entry == null) return null;
        if (entry.cutsceneCameraCenter != null) return entry.cutsceneCameraCenter;
        return entry.cutsceneSpawn;
    }

    public bool GetCutsceneTallRoom(int stageNumber)
    {
        StageEntry entry = FindEntry(stageNumber);
        return entry != null && entry.tallRoom;
    }

    /// <summary>
    /// Stage Data에 넣은 Timeline을 재생합니다.
    /// 컷씬 스폰으로 이동한 뒤 AbilityManager에서 호출합니다.
    /// </summary>
    public void PlayStageTimeline(int stageNumber)
    {
        StageEntry entry = FindEntry(stageNumber);
        if (entry == null || entry.data == null) return;

        TimelineAsset timeline = entry.data.Timeline;
        if (timeline == null) return;

        PlayableDirector director = entry.timelineDirector != null
            ? entry.timelineDirector
            : sharedTimelineDirector;

        if (director == null)
        {
            Debug.LogWarning($"{name}: Stage {stageNumber} Timeline은 있지만 PlayableDirector가 없습니다.", this);
            return;
        }

        StopActiveTimeline();

        if (director.playableAsset != timeline)
            director.playableAsset = timeline;

        director.time = 0;
        director.Evaluate();
        director.Play();
        activeTimelineDirector = director;
    }

    /// <summary>재생 중인 스테이지 Timeline을 멈춥니다.</summary>
    public void StopActiveTimeline()
    {
        if (activeTimelineDirector == null) return;

        if (activeTimelineDirector.state == PlayState.Playing)
            activeTimelineDirector.Stop();

        activeTimelineDirector = null;
    }

    StageEntry FindEntry(int stageNumber)
    {
        if (stages == null) return null;

        for (int i = 0; i < stages.Count; i++)
        {
            StageEntry entry = stages[i];
            if (entry != null && entry.data != null && entry.data.StageNumber == stageNumber)
                return entry;
        }

        if (0 <= stageNumber && stageNumber < stages.Count)
            return stages[stageNumber];

        return null;
    }

    IEnumerator StageStartRoutine()
    {
        KillTweens();

        panelTween = canvasGroup.DOFade(0f, 0f);
        if (stageNumberText != null && stageNumberText.color.a >= 1f)
            numberTween = stageNumberText.DOFade(0f, 0f);
        if (stageNameText != null && stageNameText.color.a >= 1f)
            nameTween = stageNameText.DOFade(0f, 0f);

        panelTween = canvasGroup.DOFade(1f, panelFadeDuration).SetEase(Ease.Linear);
        yield return panelTween.WaitForCompletion();

        yield return new WaitForSeconds(textDelay);

        if (stageNumberText != null && stageNumberText.color.a >= 1f)
            numberTween = stageNumberText.DOFade(0f, 0f);
        if (stageNameText != null && stageNameText.color.a >= 1f)
            nameTween = stageNameText.DOFade(0f, 0f);

        numberTween = stageNumberText.DOFade(1f, textFadeDuration).SetEase(Ease.Linear);
        nameTween = stageNameText.DOFade(1f, textFadeDuration).SetEase(Ease.Linear);
        yield return nameTween.WaitForCompletion();

        yield return new WaitForSeconds(panelOutDelay);

        panelTween = canvasGroup.DOFade(0f, panelFadeDuration).SetEase(Ease.Linear);
        yield return panelTween.WaitForCompletion();

        running = null;
    }

    void KillTweens()
    {
        panelTween?.Kill();
        numberTween?.Kill();
        nameTween?.Kill();
    }

    void OnDisable()
    {
        KillTweens();
    }
}
