using UnityEngine;

public class SublimationAbility : IAbility
{
    public ActivationType Activation => ActivationType.Instant;

    public float GaugeCost => 0f;

    public float DrainPerSecond => 0f;

    public bool IsActive => true;


    public MovementStats GetStats()
    {
        return new MovementStats
        {
            moveSpeed = 3f,
            jumpHeight = 3f,
            dashSpeed = 22f,
            dashDuration = 0.1f,
            canJump = true,
            canDash = true
        };
    }

    public void OnActiveUpdate(PlayerController pc) { }

    public void OnEquip(PlayerController pc)
    {
        pc.gameObject.layer = LayerMask.NameToLayer("Sublimation");
        AudioManager.Play(SFXKey.SublimationBallet);
    }

    public void Fire(PlayerController pc) { }

    public void OnUnequip(PlayerController pc)
    {
        pc.gameObject.layer = LayerMask.NameToLayer("Player");
    }
}
