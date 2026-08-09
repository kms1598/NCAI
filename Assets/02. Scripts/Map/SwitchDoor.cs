using UnityEngine;

public class SwitchDoor : MonoBehaviour, ISwitchable
{
    bool isOpen = false;
    public string targetRoomId;
    public Transform targetSpawn;
    public GameObject closedDoor;
    public GameObject openDoor;


    public void SetState()
    {
        isOpen = true;
        closedDoor.SetActive(false);
        openDoor.SetActive(true);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!isOpen) return;

        other.GetComponent<PlayerRespawn>()?.SetSpawn(targetSpawn.position);

        RoomManager.instance.SwitchRoom(targetRoomId, targetSpawn.position);
    }

    private void OnDrawGizmosSelected()
    {
        if (targetSpawn == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, targetSpawn.position);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
