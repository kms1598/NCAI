using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 이동 컨트롤러.
/// Animator 연동은 <see cref="PlayerAnimator"/> + 각 상태/능력 호출 지점 주석 참고.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    public MovementStats baseStats = MovementStats.Default;

    [Header("Directional Speed")]
    [Tooltip("캐릭터 정면 방향 이동 속도 배율")]
    public float forwardSpeedMultiplier = 1f;
    [Tooltip("뒷걸음질 속도 배율")]
    public float backwardSpeedMultiplier = 0.55f;
    [Tooltip("좌우 곁걸음 속도 배율")]
    public float strafeSpeedMultiplier = 0.75f;

    public float gravity = -25f;

    public Transform activeCamera;

    private MovementStats currentStats;
    public MovementStats Stats => currentStats;

    public CharacterController cc;
    public PlayerAnimator Anim { get; private set; }
    public PlayerFootstep Footstep { get; private set; }

    /// <summary>false면 이동·점프·대시·조준이 모두 멈춥니다. 인트로 연출 등에 사용.</summary>
    public bool CanControl { get; private set; } = true;

    public Vector2 moveInput;
    Vector2 rawMoveInput;
    public Vector3 velocity;
    public Vector3 aimDir;
    public bool jumpPressed;
    public bool dashPressed;
    public bool dashUsedInAir;
    const float DASH_COOLTIME = 1;
    float lastDashTime = -1f;
    public bool canDash => lastDashTime + DASH_COOLTIME <= Time.time;

    public GroundState Ground { get; private set; }
    public JumpState Jump { get; private set; }
    public DashState Dash { get; private set; }
    private PlayerState currentState;

    public GameObject arrowPrefab;
    public GameObject platformPrefab;

    void Awake()
    {
        if (instance == null) instance = this;
        if(instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        cc = GetComponent<CharacterController>();
        Anim = GetComponent<PlayerAnimator>();

        // 발소리는 별도 설정 없이도 동작해야 하므로, 컴포넌트를 안 붙여 뒀으면 여기서 붙입니다.
        // 걸음 간격이나 음량을 직접 조절하려면 인스펙터에서 PlayerFootstep을 추가하세요.
        Footstep = GetComponent<PlayerFootstep>();
        if (Footstep == null) Footstep = gameObject.AddComponent<PlayerFootstep>();

        currentStats = baseStats;
        aimDir = transform.forward;

        Ground = new GroundState(this);
        Jump = new JumpState(this);
        Dash = new DashState(this);
    }

    void Start()
    {
        TransitionTo(Ground);
    }

    void Update()
    {
        // 잠금 중에도 중력은 적용되도록 상태 갱신은 유지하고 입력만 무시합니다.
        moveInput = CanControl ? rawMoveInput : Vector2.zero;

        currentState.Update();
        jumpPressed = false;
        dashPressed = false;
    }

    public void SetControlEnabled(bool on)
    {
        CanControl = on;
        if (on) return;

        moveInput = Vector2.zero;
        jumpPressed = false;
        dashPressed = false;
    }

    public void TransitionTo(PlayerState next)
    {
        currentState?.Exit();
        currentState = next;
        currentState.Enter();
    }

    public void MarkDashUsedTime()
    {
        lastDashTime = Time.time;
    }

    public void SetStats(MovementStats s)
    {
        currentStats = s;
    }

    public void ResetStats()
    {
        currentStats = baseStats;
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }

    /// <summary>
    /// 카메라 기준 이동 방향(월드). W는 화면 위쪽, 즉 카메라가 바라보는 쪽입니다.
    /// </summary>
    public Vector3 GetCameraRelativeMove()
    {
        if (activeCamera == null) return new Vector3(moveInput.x, 0, moveInput.y);

        Vector3 forward = activeCamera.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = activeCamera.right;
        right.y = 0;
        right.Normalize();

        return Vector3.ClampMagnitude(forward * moveInput.y + right * moveInput.x, 1f);
    }

    /// <summary>
    /// 캐릭터 정면 기준으로 전후·좌우 배율을 적용한 이동 방향(월드 기준).
    /// 뒷걸음질과 곁걸음이 정면 이동보다 느려집니다.
    /// </summary>
    public Vector3 GetDirectionalMove()
    {
        Vector3 world = GetCameraRelativeMove();
        if (world.sqrMagnitude < 0.0001f) return Vector3.zero;

        Vector3 local = transform.InverseTransformDirection(world);
        local.z *= 0f <= local.z ? forwardSpeedMultiplier : backwardSpeedMultiplier;
        local.x *= strafeSpeedMultiplier;

        return transform.TransformDirection(local);
    }

    public void OnMove(InputValue v)
    {
        rawMoveInput = v.Get<Vector2>();
    }

    public void OnJump(InputValue v)
    {
        if (CanControl && v.isPressed) jumpPressed = true;
    }

    public void OnDash(InputValue v)
    {
        if (CanControl && v.isPressed) dashPressed = true;
    }

    public void OnSelect1(InputValue v)
    {
        if (v.isPressed) AbilityManager.instance.SelectSlot(1);
    }
    public void OnSelect2(InputValue v)
    {
        if (v.isPressed) AbilityManager.instance.SelectSlot(2);
    }
    public void OnSelect3(InputValue v)
    {
        if (v.isPressed) AbilityManager.instance.SelectSlot(3);
    }
    public void OnSelect4(InputValue v)
    {
        if (v.isPressed) AbilityManager.instance.SelectSlot(4);
    }

    public void OnTransform(InputValue v)
    {
        if (v.isPressed) AbilityManager.instance.Transform();
    }
    public void OnFire(InputValue v)
    {
        if (v.isPressed) AbilityManager.instance.FireCurrent();
    }
}
