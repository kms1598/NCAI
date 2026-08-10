using UnityEngine;

public class ChasePlayer : MonoBehaviour
{
    Vector3 startPos;
    Quaternion startRot;
    public Transform player;
    public float speed = 5f;
    public float chaseRange = 3f;
    public float yTolerance = 1.5f;
    bool isInit = false;

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
    }

    void Update()
    {
        if (player == null) return;

        bool grounded = PlayerController.instance.cc.isGrounded;

        if (!grounded) return;

        if (yTolerance < Mathf.Abs(player.position.y - transform.position.y)) return;

        Vector3 flat = player.position - transform.position;
        flat.y = 0;
        if (chaseRange < flat.magnitude) return;
        if (flat.sqrMagnitude < 0.0001f) return;

        Vector3 dir = flat.normalized;
        transform.position += dir * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    private void ResetSelf()
    {
        transform.localPosition = startPos;
        transform.localRotation = startRot;
    }
}
