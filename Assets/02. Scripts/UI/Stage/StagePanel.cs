using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class StagePanel : MonoBehaviour
{
    public static StagePanel instance;

    [SerializeField] List<StageScriptable> stageScriptable = new List<StageScriptable>();

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

    void Awake()
    {
        if (instance == null) instance = this;
        if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
        StageScriptable data = FindStage(stageNumber);
        if (data == null)
        {
            Debug.LogWarning($"{name}: Stage {stageNumber} 데이터가 없습니다. stageScriptable 목록을 확인하세요.", this);
            return;
        }

        SetStageInfo(data.StageNumber, data.StageName);

        // 스테이지 BGM이 있으면 기본곡에서 페이드로 넘깁니다.
        if (data.Bgm != null && AudioManager.instance != null)
            AudioManager.instance.PlayBGM(data.Bgm);

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(StageStartRoutine());
    }

    StageScriptable FindStage(int stageNumber)
    {
        for (int i = 0; i < stageScriptable.Count; i++)
        {
            if (stageScriptable[i] != null && stageScriptable[i].StageNumber == stageNumber)
                return stageScriptable[i];
        }

        // 번호로 못 찾으면 리스트 인덱스로 한 번 더 시도합니다.
        if (0 <= stageNumber && stageNumber < stageScriptable.Count)
            return stageScriptable[stageNumber];

        return null;
    }

    IEnumerator StageStartRoutine()
    {
        KillTweens();

        panelTween = canvasGroup.DOFade(0f, 0f);
        // 알파가 1이면 DOFade로 0까지 내려, 이후 페이드 인이 보이게 합니다.
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
