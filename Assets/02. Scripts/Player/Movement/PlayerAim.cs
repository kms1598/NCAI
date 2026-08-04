using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    public bool useFixedHeight = false;
    public float aimHeight = 0f;

    private PlayerController pc;
    private Camera cachedCam;
    private Transform cachedCamTransform;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
    }

    void Update()
    {
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
            Vector3 dir = hit - transform.position;
            dir.y = 0;
            if(0.001f < dir.sqrMagnitude)
            {
                pc.aimDir = dir.normalized;
                transform.rotation = Quaternion.LookRotation(pc.aimDir, Vector3.up);
            }
        }
    }
}
