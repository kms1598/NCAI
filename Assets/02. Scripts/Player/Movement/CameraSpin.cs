using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSpin : MonoBehaviour
{
    public static CameraSpin instance;

    public float distance = 10f;
    public float pitch = 30f;
    public float heightOffset = 1f;

    [Range(0, 3)]
    public int dirIndex = 0; //동남서북에 해당하는 인덱스
    public float rotateSmooth = 0.2f;

    public Vector3 roomCenter;

    private float smoothedY;
    private bool smoothedYInit;

    public float currentYaw;
    private float targetYaw;
    private float yawVel;

    private float lastGroundY;
    private float camYVel;

    void Awake()
    {
        if (instance == null) instance = this;
        if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        targetYaw = dirIndex * 90f;
        currentYaw = targetYaw;
    }

    void Start()
    {
        if (PlayerController.instance != null)
        {
            lastGroundY = PlayerController.instance.transform.position.y;
            PlayerController.instance.activeCamera = transform;
        }
    }

    void LateUpdate()
    {
        targetYaw = dirIndex * 90f;
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVel, rotateSmooth);
        
        if(PlayerController.instance.cc.isGrounded)
        {
            lastGroundY = PlayerController.instance.transform.position.y;
        }
        if (!smoothedYInit)
        {
            smoothedY = lastGroundY;
            smoothedYInit = true;
        }
        smoothedY = Mathf.SmoothDamp(smoothedY, lastGroundY, ref camYVel, 0.3f);

        Vector3 pivot = roomCenter;
        pivot.y = smoothedY + heightOffset;

        Quaternion rot = Quaternion.Euler(pitch, currentYaw, 0);
        Vector3 offset = rot * new Vector3(0, 0, -distance);
        transform.position = pivot + offset;
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
    }

    public void SetRoom(Vector3 center, bool isTall)
    {
        roomCenter = center;
        if (PlayerController.instance != null)
        {
            lastGroundY = PlayerController.instance.transform.position.y;
            smoothedY = lastGroundY;
        }
    }

    public void OnRotateLeft(InputValue v)
    {
        if (v.isPressed) dirIndex = (dirIndex + 3) % 4;
    }
    public void OnRotateRight(InputValue v)
    {
        if (v.isPressed) dirIndex = (dirIndex + 1) % 4;
    }
}
