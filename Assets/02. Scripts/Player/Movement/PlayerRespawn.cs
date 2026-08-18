using System;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    const float RespawnCooldown = 0.75f;

    Vector3 currentSpawnPos;
    float lastRespawnTime = -999f;
    float ignoreDeathUntil;

    public static event Action OnRespawn;

    /// <summary>텔레포트 직후처럼 잠깐 사망 판정을 무시할 때 씁니다.</summary>
    public void IgnoreDeathFor(float seconds)
    {
        ignoreDeathUntil = Mathf.Max(ignoreDeathUntil, Time.time + seconds);
    }

    public bool CanRespawn =>
        Time.time >= ignoreDeathUntil && Time.time >= lastRespawnTime + RespawnCooldown;

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
        // OnTriggerStay가 매 프레임 들어오면 죽는 소리가 무한 재생됩니다.
        if (!CanRespawn) return;

        lastRespawnTime = Time.time;

        AudioManager.Play(SFXKey.PlayerDie);

        PlayerController.instance.cc.enabled = false;
        transform.position = currentSpawnPos;
        Physics.SyncTransforms();
        PlayerController.instance.cc.enabled = true;
        PlayerController.instance.ResetVelocity();

        // 리스폰 직후에도 같은 데스존에 겹쳐 있으면 Stay가 바로 다시 옵니다.
        IgnoreDeathFor(RespawnCooldown);

        PlayerController.instance.Anim?.PlayRespawn();
        OnRespawn?.Invoke();
    }
}
