using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 로딩 연출 패널입니다. 다른 스크립트에서 UILoadingPanel.instance로 호출하세요.
/// </summary>
public class UILoadingPanel : MonoBehaviour
{
    public static UILoadingPanel instance;

    [SerializeField] Animator animator;
    [Tooltip("자식까지 함께 페이드하려면 CanvasGroup을 씁니다. 없으면 런타임에 붙입니다.")]
    [SerializeField] CanvasGroup canvasGroup;

    [Header("Animator 상태 이름")]
    [SerializeField] string openStateName = "Open";
    [SerializeField] string closeStateName = "Close";
    [SerializeField] int animatorLayer = 0;

    [Header("페이드")]
    [SerializeField] float fadeDuration = 0.3f;
    [Tooltip("Close()를 인자 없이 불렀을 때 닫힌 상태를 유지할 시간(초)입니다.")]
    [SerializeField] float defaultHoldDuration = 1f;

    Coroutine running;
    Tween fadeTween;

    void Awake()
    {
        if (instance == null) instance = this;
        if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (animator == null) animator = GetComponent<Animator>();

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 일시정지(timeScale 0) 중에도 연출이 돌아가야 합니다.
        if (animator != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    /// <summary>Open 애니메이션을 재생하고 끝나면 페이드 아웃으로 화면을 보여 줍니다.</summary>
    public void Open()
    {
        Restart(OpenRoutine());
    }

    /// <summary>닫은 뒤 defaultHoldDuration만큼 기다렸다가 다시 엽니다.</summary>
    public void Close()
    {
        Close(defaultHoldDuration);
    }

    /// <summary>
    /// 페이드 인 → Close 애니메이션 → holdDuration만큼 대기 → Open 순서로 진행합니다.
    /// </summary>
    public void Close(float holdDuration)
    {
        Restart(CloseRoutine(holdDuration));
    }

    /// <summary>닫은 상태로 유지합니다. 여는 시점을 직접 정하고 싶을 때 쓰세요.</summary>
    public void CloseAndHold()
    {
        Restart(CloseRoutine(-1f));
    }

    IEnumerator OpenRoutine()
    {
        SetBlocking(true);

        yield return PlayState(openStateName);
        yield return Fade(0f);

        SetBlocking(false);
    }

    IEnumerator CloseRoutine(float holdDuration)
    {
        SetBlocking(true);

        yield return Fade(1f);
        yield return PlayState(closeStateName);

        // 음수는 "열지 않고 유지"라는 뜻입니다. CloseAndHold가 이 경로를 씁니다.
        if (holdDuration < 0f) yield break;

        if (0f < holdDuration) yield return new WaitForSecondsRealtime(holdDuration);

        yield return OpenRoutine();
    }

    /// <summary>상태를 처음부터 재생하고 그 길이만큼 기다립니다.</summary>
    IEnumerator PlayState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) yield break;

        animator.Play(stateName, animatorLayer, 0f);
        // Play는 다음 평가 시점에 반영되므로, 길이를 읽기 전에 한 번 강제로 갱신합니다.
        animator.Update(0f);

        yield return new WaitForSecondsRealtime(animator.GetCurrentAnimatorStateInfo(animatorLayer).length);
    }

    IEnumerator Fade(float targetAlpha)
    {
        fadeTween?.Kill();
        // timeScale에 영향받지 않도록 SetUpdate(true)로 둡니다.
        fadeTween = canvasGroup.DOFade(targetAlpha, fadeDuration).SetEase(Ease.Linear).SetUpdate(true);

        yield return fadeTween.WaitForCompletion();
    }

    void SetBlocking(bool on)
    {
        canvasGroup.blocksRaycasts = on;
        canvasGroup.interactable = on;
    }

    void Restart(IEnumerator routine)
    {
        if (running != null) StopCoroutine(running);
        fadeTween?.Kill();

        running = StartCoroutine(routine);
    }
}
