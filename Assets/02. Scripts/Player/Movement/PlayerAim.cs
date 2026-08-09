using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    public bool useFixedHeight = false;
    public float aimHeight = 0f;

    [Header("Body Rotation")]
    [Tooltip("끄면 몸통은 고정되고 머리 IK만 조준점을 따라갑니다")]
    public bool rotateBodyToAim = true;
    [Tooltip("이 각도 이하에서는 몸통을 돌리지 않고 머리 IK만 사용")]
    public float bodyRotateDeadZone = 60f;
    [Tooltip("몸통 회전 속도(도/초). 0 이하면 즉시 회전")]
    public float bodyRotateSpeed = 540f;

    /// <summary>마우스 레이가 맞춘 월드 좌표. HeadLookAt IK 목표로 사용.</summary>
    public Vector3 AimWorldPoint { get; private set; }
    public bool HasValidAim { get; private set; }

    private PlayerController pc;
    private Camera cachedCam;
    private Transform cachedCamTransform;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
    }

    void Update()
    {
        HasValidAim = false;

        if (pc.activeCamera == null) return;

        if(cachedCamTransform != pc.activeCamera)
        {
            cachedCamTransform = pc.activeCamera;
            cachedCam = pc.activeCamera.GetComponent<Camera>();
        }

        if (cachedCam == null) return;

        Ray ray = cachedCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        float planeY = useFixedHeight ? aimHeight : transform.position.y;
        Plane plane = new Plane(Vector3.up, new Vector3(0, planeY, 0));

        if(plane.Raycast(ray, out float dist))
        {
            Vector3 hit = ray.GetPoint(dist);
            AimWorldPoint = hit;
            HasValidAim = true;

            Vector3 dir = hit - transform.position;
            dir.y = 0;
            if(0.001f < dir.sqrMagnitude)
            {
                pc.aimDir = dir.normalized;
                RotateBody(pc.aimDir);
            }
        }
    }

    void RotateBody(Vector3 aimDir)
    {
        if (!rotateBodyToAim) return;

        // 좁은 각도는 머리 IK만 사용해서 몸통을 고정
        if (Vector3.Angle(transform.forward, aimDir) <= bodyRotateDeadZone) return;

        Quaternion target = Quaternion.LookRotation(aimDir, Vector3.up);
        transform.rotation = bodyRotateSpeed <= 0f
            ? target
            : Quaternion.RotateTowards(transform.rotation, target, bodyRotateSpeed * Time.deltaTime);
    }
}
