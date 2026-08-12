using UnityEngine;

public class SublimationAbility : IAbility
{
    public ActivationType Activation => ActivationType.Instant;

    public float GaugeCost => 0f;

    public float DrainPerSecond => 0f;

    public bool ProvidesStats => false;

    public bool IsActive => false;


    public MovementStats GetStats()
    {
        return new MovementStats
        {
            moveSpeed = 3f,
            jumpHeight = 1.5f * 2,
            dashSpeed = 20f * 1.1f,
            dashDuration = 0.1f,
            canJump = true,
            canDash = true
        };
    }

    public void OnActiveUpdate(PlayerController pc) { }

    public void OnEquip(PlayerController pc)
    {
        pc.gameObject.layer = LayerMask.NameToLayer("Sublimation");
    }
    public void Fire(PlayerController pc) { }

    public void OnUnequip(PlayerController pc)
    {
        pc.gameObject.layer = LayerMask.NameToLayer("Player");
    }
}
