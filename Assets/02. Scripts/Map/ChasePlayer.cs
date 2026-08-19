using UnityEngine;

public class ChasePlayer : MonoBehaviour
{
    static readonly int Run = Animator.StringToHash("Run");

    Vector3 startPos;
    Quaternion startRot;
    public Transform player;
    public float speed = 5f;
    public float chaseRange = 3f;
    public float yTolerance = 1.5f;

    [Tooltip("추격을 시작할 때 낼 소리입니다. 적 종류에 따라 sfx_enemy_1~3 중에서 고르세요.")]
    [SerializeField] string chaseSfx = SFXKey.Enemy1;
    [Tooltip("추격 범위 경계에서 소리가 겹치지 않도록 두는 최소 간격(초)입니다.")]
    [SerializeField] float chaseSfxCooldown = 4f;
    [Tooltip("비우면 같은 오브젝트(또는 자식)에서 Animator를 찾습니다.")]
    [SerializeField] Animator animator;

    bool isInit = false;
    bool chasing = false;
    float lastChaseSfxTime = -999f;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        PlayerRespawn.OnRespawn += ResetSelf;
        if (isInit) ResetSelf();
    }

    void OnDisable()
    {
        PlayerRespawn.OnRespawn -= ResetSelf;
    }

    void Start()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;
        player = PlayerController.instance.gameObject.transform;
        isInit = true;
        SetRun(false);
    }

    void Update()
    {
        bool canChase = TryGetChaseDirection(out Vector3 dir);

        // 추격이 시작되는 순간에만 한 번 울립니다. 상태를 기억하지 않으면 매 프레임 소리가 겹칩니다.
        if (canChase && !chasing && lastChaseSfxTime + chaseSfxCooldown <= Time.time)
        {
            AudioManager.PlayAt(chaseSfx, transform.position);
            lastChaseSfxTime = Time.time;
        }

        if (chasing != canChase)
            SetRun(canChase);

        chasing = canChase;

        if (!canChase) return;

        transform.position += dir * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    void SetRun(bool on)
    {
        if (animator == null) return;
        animator.SetBool(Run, on);
    }

    bool TryGetChaseDirection(out Vector3 dir)
    {
        dir = Vector3.zero;

        if (player == null || PlayerController.instance == null) return false;
        if (!PlayerController.instance.cc.isGrounded) return false;
        if (yTolerance < Mathf.Abs(player.position.y - transform.position.y)) return false;

        Vector3 flat = player.position - transform.position;
        flat.y = 0;

        if (chaseRange < flat.magnitude) return false;
        if (flat.sqrMagnitude < 0.0001f) return false;

        dir = flat.normalized;
        return true;
    }

    void ResetSelf()
    {
        transform.localPosition = startPos;
        transform.localRotation = startRot;
        chasing = false;
        SetRun(false);
    }
}
