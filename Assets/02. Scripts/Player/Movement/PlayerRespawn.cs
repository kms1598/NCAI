using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 currentSpawnPos;

    void Awake()
    {
        currentSpawnPos = transform.position;
    }

    public void SetSpawn(Vector3 pos)
    {
        currentSpawnPos = pos;
    }

    public void Respawn()
    {
        PlayerController.instance.cc.enabled = false;
        transform.position = currentSpawnPos;
        PlayerController.instance.cc.enabled = true;
        PlayerController.instance.ResetVelocity();
        // Anim: Respawn — 리스폰
        PlayerController.instance.Anim?.PlayRespawn();
    }
}
