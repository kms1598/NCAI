using UnityEngine;

public class Door : MonoBehaviour
{
    public string targetRoomId;
    public Transform targetSpawn;

    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player")) return;

        AudioManager.Play(SFXKey.EnterDoor);
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
