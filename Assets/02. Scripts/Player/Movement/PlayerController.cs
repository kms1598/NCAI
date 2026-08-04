using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    public MovementStats baseStats = MovementStats.Default;

    public float gravity = -25f;

    public Transform activeCamera;

    private MovementStats currentStats;
    public MovementStats Stats => currentStats;

    public CharacterController cc;
    public Vector2 moveInput;
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
        currentState.Update();
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

    public void OnMove(InputValue v)
    {
        moveInput = v.Get<Vector2>();
    }

    public void OnJump(InputValue v)
    {
        if (v.isPressed) jumpPressed = true;
    }

    public void OnDash(InputValue v)
    {
        if (v.isPressed) dashPressed = true;
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
