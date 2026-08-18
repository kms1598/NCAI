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
        if (isFloor && other.gameObject.layer == LayerMask.NameToLayer("PlatformImmune")) return;

        other.GetComponent<PlayerRespawn>()?.Respawn();
    }
}
