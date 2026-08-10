using UnityEngine;

/// <summary>
/// PlayerController Animator 연동.
/// Animator Controller에 아래 파라미터를 만들어 두세요.
/// </summary>
/// - Speed (float), MoveX (float), MoveZ (float), MoveSpeedMultiplier (float)
/// - Jump, Dash, Land, Shoot, Transform, Respawn (trigger)
/// - AbilityIndex (int), IsCrawling (bool), IsPlatform (bool)
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    static readonly int Speed = Animator.StringToHash("Speed");
    static readonly int MoveX = Animator.StringToHash("MoveX");
    static readonly int MoveZ = Animator.StringToHash("MoveZ");
    static readonly int MoveSpeedMultiplier = Animator.StringToHash("MoveSpeedMultiplier");
    static readonly int Jump = Animator.StringToHash("Jump");
    static readonly int Dash = Animator.StringToHash("Dash");
    static readonly int Land = Animator.StringToHash("Land");
    static readonly int Shoot = Animator.StringToHash("Shoot");
    static readonly int Transform = Animator.StringToHash("Transform");
    static readonly int Respawn = Animator.StringToHash("Respawn");
    static readonly int AbilityIndex = Animator.StringToHash("AbilityIndex");
    static readonly int IsCrawling = Animator.StringToHash("IsCrawling");
    static readonly int IsPlatform = Animator.StringToHash("IsPlatform");

    [Tooltip("이동 블렌드 파라미터 감쇠 시간. 클수록 방향 전환이 부드럽습니다")]
    [SerializeField] float moveDampTime = 0.12f;

    Animator anim;
    PlayerController pc;

    void Awake()
    {
        anim = GetComponent<Animator>();
        pc = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (anim == null || pc == null) return;

        // 카메라 기준 이동 방향을 캐릭터 정면 기준으로 변환해 전후·좌우 걷기를 구분
        Vector3 worldMove = pc.GetCameraRelativeMove();
        Vector3 localMove = transform.InverseTransformDirection(worldMove);

        // MoveX / MoveZ — 2D 블렌드 트리 (앞·뒤·좌·우 걷기)
        // 방향 배율이 아닌 입력 방향을 넣어야 해당 클립이 온전한 가중치로 재생됩니다.
        anim.SetFloat(MoveX, localMove.x, moveDampTime, Time.deltaTime);
        anim.SetFloat(MoveZ, localMove.z, moveDampTime, Time.deltaTime);
        // Speed — Idle / Move 전환용
        anim.SetFloat(Speed, worldMove.magnitude, moveDampTime, Time.deltaTime);
        // 실제 이동 속도가 느려진 만큼 재생 속도를 낮춰 발 미끄러짐을 줄입니다.
        anim.SetFloat(MoveSpeedMultiplier, GetMoveSpeedMultiplier(worldMove), moveDampTime, Time.deltaTime);
    }

    float GetMoveSpeedMultiplier(Vector3 worldMove)
    {
        float rawSpeed = worldMove.magnitude;
        if (rawSpeed < 0.0001f) return 1f;

        return pc.GetDirectionalMove().magnitude / rawSpeed;
    }

    /// <summary>Jump — 점프 시작 (Ground → Jump)</summary>
    public void PlayJump()
    {
        anim?.SetTrigger(Jump);
    }

    /// <summary>Dash — 대시 시작 (DashState Enter)</summary>
    public void PlayDash()
    {
        anim?.SetTrigger(Dash);
    }

    /// <summary>Land — 착지 (Jump → Ground)</summary>
    public void PlayLand()
    {
        anim?.SetTrigger(Land);
    }

    /// <summary>Shoot — 화살 발사 (ArrowAbility)</summary>
    public void PlayShoot()
    {
        anim?.SetTrigger(Shoot);
    }

    /// <summary>Transform — 능력 형태 전환 (AbilityManager.Transform)</summary>
    public void PlayTransform(int index)
    {
        if (anim == null) return;
        anim.SetInteger(AbilityIndex, index);
        anim.SetTrigger(Transform);
    }

    /// <summary>Respawn — 리스폰 (PlayerRespawn)</summary>
    public void PlayRespawn()
    {
        anim?.SetTrigger(Respawn);
    }

    /// <summary>Crawl_Idle / Crawl_Move — 기어가기 on·off</summary>
    public void SetCrawling(bool on)
    {
        anim?.SetBool(IsCrawling, on);
    }

    /// <summary>Platform — 발판 모드 on·off</summary>
    public void SetPlatform(bool on)
    {
        anim?.SetBool(IsPlatform, on);
    }

    /// <summary>장착 능력 인덱스 동기화 (0=None … 4=Sublimation)</summary>
    public void SetAbilityIndex(int index)
    {
        anim?.SetInteger(AbilityIndex, index);
    }
}
