using UnityEngine;

public class SendToSpawn : MonoBehaviour
{
    public bool isFloor = false;

    private void OnTriggerEnter(Collider other)
    {
        Collision(other);
    }

    private void OnTriggerStay(Collider other)
    {
        Collision(other);
    }

    void Collision(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (AbilityManager.instance != null && AbilityManager.instance.IsCutsceneTransition) return;
        if (isFloor && other.gameObject.layer == LayerMask.NameToLayer("PlatformImmune")) return;

        PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
        if (respawn == null || !respawn.CanRespawn) return;

        respawn.Respawn();
    }
}
