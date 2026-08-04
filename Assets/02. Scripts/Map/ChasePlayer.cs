using UnityEngine;

public class ChasePlayer : MonoBehaviour
{
    public Transform player;
    public float speed = 5f;
    public float chaseRange = 3f;
    public float yTolerance = 0.001f;

    void Start()
    {
        player = PlayerController.instance.gameObject.transform;
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
}
