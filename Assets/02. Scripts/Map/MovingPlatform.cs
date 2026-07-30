using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{

    [SerializeField] Transform[] waypoints;
    [SerializeField] float speed = 2f;
    [SerializeField] float waitTime = 0.5f;

    Rigidbody rb;
    Collider col;
    int index;
    int dir = 1;
    float waitTimer;
    readonly List<Rigidbody> riders = new List<Rigidbody>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.isKinematic = true;
    }

    void FixedUpdate()
    {
        if (waypoints.Length < 2) return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector3 target = waypoints[index].position;
        Vector3 newPos = Vector3.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        Vector3 delta = newPos - rb.position;

        rb.MovePosition(newPos);

        for (int i = riders.Count - 1; 0 <= i; i--)
        {
            if (riders[i] == null) { riders.RemoveAt(i); continue; }
            riders[i].MovePosition(riders[i].position + delta);
        }

        if (Vector3.Distance(newPos, target) < 0.01f)
        {
            waitTimer = waitTime;
            NextWaypoint();
        }
    }

    void NextWaypoint()
    {
        index = (index + 1) % waypoints.Length;
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.rigidbody == null) return;

        float topY = col.bounds.max.y;
        foreach (var contact in c.contacts)
        {
            if (topY - 0.05f < contact.point.y)
            {
                if (!riders.Contains(c.rigidbody))
                    riders.Add(c.rigidbody);
                break;
            }
        }
    }

    void OnCollisionExit(Collision c)
    {
        if (c.rigidbody != null)
            riders.Remove(c.rigidbody);
    }

    // 씬에서 경로 미리보기
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length - 1; i++)
            if (waypoints[i] && waypoints[i + 1])
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
    }
}