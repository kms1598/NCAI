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
    [Tooltip("씬이 바뀌어도 패널을 유지합니다. 씬 전환 연출에 쓰려면 켜 두세요.")]
    [SerializeField] bool persistAcrossScenes = true;

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

        if (persistAcrossScenes) KeepAlive();
    }

    void OnDestroy()
    {
        // 비워 두지 않으면 씬과 함께 사라진 뒤에도 instance가 파괴된 오브젝트를 가리킵니다.
        // 그 상태로 Close()를 부르면 MissingReferenceException이 납니다.
        if (instance == this) instance = null;
    }

    /// <summary>
    /// 씬이 바뀌어도 살아남게 합니다.
    /// 이게 없으면 씬을 덮은 채로 전환하다가 패널이 이전 씬과 함께 사라져 화면이 번쩍입니다.
    /// </summary>
    void KeepAlive()
    {
        // DontDestroyOnLoad는 루트 오브젝트에만 걸립니다.
        // 다른 UI가 붙은 Canvas의 자식으로 두면 그 UI까지 따라와 다음 씬에서 겹칩니다.
        if (transform.parent != null)
        {
            Debug.LogWarning($"{name}: 씬 전환 중에도 남으려면 이 패널이 루트 오브젝트여야 합니다. " +
                             "다른 UI와 분리된 별도 Canvas로 만들어 주세요.", this);
            return;
        }

        DontDestroyOnLoad(gameObject);
        // Screen Space - Camera로 두면 이전 씬 카메라가 사라지는 순간 캔버스가 안 그려집니다.
        // Overlay로 바꿔야 씬이 바뀐 뒤 Open 연출이 보입니다.
        EnsureOverlayCanvas();
    }

    /// <summary>
    /// 씬 전환 후에도 항상 최상단에 그려지도록 Canvas를 Overlay로 맞춥니다.
    /// </summary>
    void EnsureOverlayCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        // 다른 UI보다 위에 덮여야 로딩 연출이 가려지지 않습니다.
        if (canvas.sortingOrder < 1000) canvas.sortingOrder = 1000;

        // 에디터에서 Scale이 (0,0,0)으로 저장된 경우가 있어, 그대로면 알파와 무관하게 안 보입니다.
        if (canvas.transform.localScale == Vector3.zero)
            canvas.transform.localScale = Vector3.one;

        // Screen Space Camera용으로 찌그러져 저장된 Loading 자식을 전체 화면으로 고칩니다.
        FixLoadingChildForOverlay();
    }

    /// <summary>연출 직전에 스케일/오버레이가 깨져 있으면 다시 맞춥니다.</summary>
    void EnsureReadyToShow()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureOverlayCanvas();

        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    void FixLoadingChildForOverlay()
    {
        if (canvasGroup == null) return;

        RectTransform rt = canvasGroup.transform as RectTransform;
        if (rt == null) return;

        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// Open 애니메이션을 재생하고 끝나면 페이드 아웃으로 화면을 보여 줍니다.
    /// 반환한 Coroutine을 yield return하면 연출이 끝날 때까지 기다릴 수 있습니다.
    /// </summary>
    public Coroutine Open()
    {
        EnsureReadyToShow();
        return Restart(OpenRoutine());
    }

    /// <summary>닫은 뒤 defaultHoldDuration만큼 기다렸다가 다시 엽니다.</summary>
    public Coroutine Close()
    {
        return Close(defaultHoldDuration);
    }

    /// <summary>
    /// 페이드 인 → Close 애니메이션 → holdDuration만큼 대기 → Open 순서로 진행합니다.
    /// </summary>
    public Coroutine Close(float holdDuration)
    {
        EnsureReadyToShow();
        return Restart(CloseRoutine(holdDuration));
    }

    /// <summary>
    /// 닫은 상태로 유지합니다. 여는 시점을 직접 정하고 싶을 때 쓰세요.
    /// 씬 전환처럼 화면을 덮은 동안 다른 일을 해야 할 때 이걸 씁니다.
    /// </summary>
    public Coroutine CloseAndHold()
    {
        EnsureReadyToShow();
        return Restart(CloseRoutine(-1f));
    }

    IEnumerator OpenRoutine()
    {
        SetBlocking(true);

        // CloseAndHold 직후면 알파가 1이어야 합니다.
        // 씬 교체로 패널이 바뀌었거나 시작 알파가 0이면 Open 애니가 안 보이므로 먼저 덮습니다.
        if (canvasGroup.alpha < 1f) canvasGroup.alpha = 1f;

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

    Coroutine Restart(IEnumerator routine)
    {
        // 꺼진 오브젝트에서는 코루틴을 시작할 수 없습니다.
        // 여기서 걸러 내지 않으면 씬 전환을 기다리던 쪽이 예외를 맞습니다.
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"{name}: 패널이 비활성 상태라 연출을 재생할 수 없습니다. " +
                             "오브젝트는 켜 두고 CanvasGroup의 Alpha를 0으로 숨기세요.", this);
            return null;
        }

        if (running != null) StopCoroutine(running);
        fadeTween?.Kill();

        running = StartCoroutine(routine);

        return running;
    }
}
