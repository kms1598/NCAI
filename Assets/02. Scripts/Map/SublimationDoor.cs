using UnityEngine;

public class SublimationDoor : SublimationGate
{
    public string targetRoomId;
    public Transform targetSpawn;

    protected override void OnSublimationTouch(PlayerController pc)
    {
        AudioManager.Play(SFXKey.EnterDoor);
        pc.GetComponent<PlayerRespawn>()?.SetSpawn(targetSpawn.position);

        RoomManager.instance.SwitchRoom(targetRoomId, targetSpawn.position);
    }

    void OnDrawGizmosSelected()
    {
        if (targetSpawn == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, targetSpawn.position);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}