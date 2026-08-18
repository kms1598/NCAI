using UnityEngine;

public class PlatformAbility : IAbility
{
    public ActivationType Activation => ActivationType.Toggle;

    public float GaugeCost => 0f;

    public float DrainPerSecond => 5f;

    bool active;
    public bool IsActive => active;


    public MovementStats GetStats()
    {
        return new MovementStats
        {
            moveSpeed = 3 * 0.9f,
            jumpHeight = 0f,
            dashSpeed = 0f,
            dashDuration = 0f,
            canJump = false,
            canDash = false
        };
    }

    public void OnActiveUpdate(PlayerController pc) { }

    public void OnEquip(PlayerController pc) { }
    public void Fire(PlayerController pc)
    {
        SetActive(pc, !active);
        // 소리는 SetActive가 아니라 여기서 냅니다. 형태 전환으로 해제될 때는
        // AbilityManager가 이미 전환음을 내므로 소리가 겹치면 안 됩니다.
        AudioManager.Play(SFXKey.CharacterChange);
    }

    public void OnUnequip(PlayerController pc)
    {
        if (active) SetActive(pc, false);
    }

    void SetActive(PlayerController pc, bool on)
    {
        active = on;
        if (on)
        {
            pc.platformPrefab.SetActive(true);
            pc.gameObject.layer = LayerMask.NameToLayer("PlatformImmune");
        }
        else
        {
            pc.platformPrefab.SetActive(false);
            pc.gameObject.layer = LayerMask.NameToLayer("Player");
        }
        // Anim: Platform — 발판 모드 on·off
        pc.Anim?.SetPlatform(on);
    }
}
