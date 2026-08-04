using UnityEngine;

public class WaypointMover : MonoBehaviour, ISwitchable
{
    public Transform[] waypoints;
    public float speed = 3f;

    bool carryPlayer = false;
    [SerializeField] bool isMove = true;

    int index;
    int dir = 1;

    void Update()
    {
        if (!isMove) return;
        if (waypoints == null || waypoints.Length < 2) return;

        Vector3 before = transform.position;

        Transform target = waypoints[index];
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    
        if(carryPlayer)
        {
            Vector3 delta = transform.position - before;
            PlayerController.instance.cc.Move(delta);
        }

        if (Vector3.Distance(transform.position, target.position) < 0.01f)
        {
            if (waypoints.Length <= index + dir || index + dir < 0) dir = -dir;
            index += dir;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player")) carryPlayer = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) carryPlayer = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.gray;
        for(int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);

            if (i < waypoints.Length - 1) Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }

    public void SetState()
    {
        isMove = true;
    }
}
