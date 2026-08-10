using UnityEngine;

/// <summary>
/// 머리 IK로 PlayerAim 조준점을 바라봅니다.
/// Animator Layer의 IK Pass 활성화 + Humanoid 릭 필수.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerAim))]
public class HeadLookAt : MonoBehaviour
{
    [Header("Look Target")]
    [Tooltip("PlayerAim.AimWorldPoint를 우선 사용. 없을 때만 이 Transform 사용")]
    public Transform fallbackLookTarget;

    [Tooltip("조준점은 바닥 평면이라 그대로 보면 고개를 숙입니다. 눈높이만큼 올려 줍니다")]
    [SerializeField] float lookHeightOffset = 1.5f;

    [Header("Head IK Weight")]
    [Tooltip("머리만 돌리려면 Body는 0으로 둡니다")]
    [SerializeField, Range(0f, 1f)] float bodyWeight = 0f;
    [SerializeField, Range(0f, 1f)] float headWeight = 1f;
    [SerializeField, Range(0f, 1f)] float eyesWeight = 0f;
    [SerializeField, Range(0f, 1f)] float clampWeight = 0.5f;
    [Tooltip("IK 가중치 보간 속도. 값이 클수록 즉각적")]
    [SerializeField] float weightLerpSpeed = 8f;
    [Tooltip("이 각도를 넘으면 목이 꺾이므로 IK를 서서히 끕니다")]
    [SerializeField] float maxLookAngle = 120f;
    [SerializeField] int animatorLayer = 0;

    Animator animator;
    PlayerController pc;
    PlayerAim playerAim;

    float currentLookWeight;

    void Awake()
    {
        animator = GetComponent<Animator>();
        pc = GetComponent<PlayerController>();
        playerAim = GetComponent<PlayerAim>();
    }

    void Start()
    {
        // LookAt IK는 Humanoid 릭에서만 동작합니다.
        if (animator != null && !animator.isHuman)
            Debug.LogWarning($"{name}: Avatar가 Humanoid가 아니라 머리 LookAt IK가 동작하지 않습니다.", this);
    }

    Vector3 GetLookPosition()
    {
        if (playerAim != null && playerAim.HasValidAim)
            return playerAim.AimWorldPoint + Vector3.up * lookHeightOffset;

        if (fallbackLookTarget != null)
            return fallbackLookTarget.position;

        if (pc != null && pc.activeCamera != null)
            return pc.activeCamera.position;

        return transform.position + transform.forward + Vector3.up * lookHeightOffset;
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || layerIndex != animatorLayer) return;

        Vector3 lookPos = GetLookPosition();
        float targetWeight = GetTargetLookWeight(lookPos);

        currentLookWeight = Mathf.MoveTowards(currentLookWeight, targetWeight, weightLerpSpeed * Time.deltaTime);

        if (currentLookWeight <= 0.001f)
        {
            animator.SetLookAtWeight(0f);
            return;
        }

        animator.SetLookAtWeight(currentLookWeight, bodyWeight, headWeight, eyesWeight, clampWeight);
        animator.SetLookAtPosition(lookPos);
    }

    float GetTargetLookWeight(Vector3 lookPos)
    {
        if (playerAim != null && !playerAim.HasValidAim && fallbackLookTarget == null) return 0f;

        Vector3 toAim = lookPos - transform.position;
        toAim.y = 0;
        if (toAim.sqrMagnitude < 0.0001f) return 0f;

        float headAngle = Vector3.Angle(transform.forward, toAim.normalized);
        return 1f - Mathf.InverseLerp(maxLookAngle * 0.5f, maxLookAngle, headAngle);
    }
}
