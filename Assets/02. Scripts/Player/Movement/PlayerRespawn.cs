using System;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 currentSpawnPos;

    public static event Action OnRespawn;

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
        AudioManager.Play(SFXKey.PlayerDie);

        PlayerController.instance.cc.enabled = false;
        transform.position = currentSpawnPos;
        PlayerController.instance.cc.enabled = true;
        PlayerController.instance.ResetVelocity();
        // Anim: Respawn — 리스폰
        PlayerController.instance.Anim?.PlayRespawn();

        OnRespawn?.Invoke();
    }
}
