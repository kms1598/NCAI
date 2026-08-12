using UnityEngine;

public class PlatformAbility : IAbility
{
    public ActivationType Activation => ActivationType.Toggle;

    public float GaugeCost => 0f;

    public float DrainPerSecond => 5f;

    public bool ProvidesStats => true;

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
