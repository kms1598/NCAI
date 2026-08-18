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

    // 컷씬용: 방 중심 대신 플레이어를 따라가고, 회전 입력을 막습니다.
    bool followPlayer;
    public bool FollowPlayer => followPlayer;

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

        PlayerController pc = PlayerController.instance;
        if (pc == null) return;

        if (followPlayer)
        {
            // 컷씬: 플레이어 위치를 피벗으로 씁니다.
            Vector3 playerPos = pc.transform.position;
            lastGroundY = playerPos.y;
            if (!smoothedYInit)
            {
                smoothedY = lastGroundY;
                smoothedYInit = true;
            }
            smoothedY = Mathf.SmoothDamp(smoothedY, lastGroundY, ref camYVel, 0.15f);

            Vector3 pivot = playerPos;
            pivot.y = smoothedY + heightOffset;
            ApplyPose(pivot);
            return;
        }

        if (pc.cc != null && pc.cc.isGrounded)
            lastGroundY = pc.transform.position.y;

        if (!smoothedYInit)
        {
            smoothedY = lastGroundY;
            smoothedYInit = true;
        }
        smoothedY = Mathf.SmoothDamp(smoothedY, lastGroundY, ref camYVel, 0.3f);

        Vector3 roomPivot = roomCenter;
        roomPivot.y = smoothedY + heightOffset;
        ApplyPose(roomPivot);
    }

    public void SetRoom(Vector3 center, bool isTall)
    {
        roomCenter = center;
        if (PlayerController.instance != null)
        {
            lastGroundY = PlayerController.instance.transform.position.y;
            smoothedY = lastGroundY;
            smoothedYInit = true;
            camYVel = 0f;
        }

        ApplyPoseNow();
    }

    /// <summary>컷씬 진입: 키보드 회전 잠금 + 플레이어 추적.</summary>
    public void SetFollowPlayer(bool on)
    {
        followPlayer = on;

        // 끌 때는 여기서 roomCenter로 붙이지 않습니다.
        // 복귀 연출에서 SetRoom으로 맞춘 뒤에 LateUpdate가 따라갑니다.
        if (!on || PlayerController.instance == null) return;

        lastGroundY = PlayerController.instance.transform.position.y;
        smoothedY = lastGroundY;
        smoothedYInit = true;
        camYVel = 0f;
        ApplyPoseNow();
    }

    void ApplyPoseNow()
    {
        Vector3 pivot;
        if (followPlayer && PlayerController.instance != null)
        {
            pivot = PlayerController.instance.transform.position;
            pivot.y = smoothedY + heightOffset;
        }
        else
        {
            pivot = roomCenter;
            pivot.y = smoothedY + heightOffset;
        }

        ApplyPose(pivot);
    }

    void ApplyPose(Vector3 pivot)
    {
        Quaternion rot = Quaternion.Euler(pitch, currentYaw, 0);
        Vector3 offset = rot * new Vector3(0, 0, -distance);
        transform.position = pivot + offset;
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
    }

    public void OnRotateLeft(InputValue v)
    {
        if (followPlayer) return;
        if (v.isPressed) dirIndex = (dirIndex + 3) % 4;
    }

    public void OnRotateRight(InputValue v)
    {
        if (followPlayer) return;
        if (v.isPressed) dirIndex = (dirIndex + 1) % 4;
    }
}
