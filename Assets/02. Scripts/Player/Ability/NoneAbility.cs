using UnityEngine;

public class NoneAbility : IAbility
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
            jumpHeight = 1.5f,
            dashSpeed = 20f,
            dashDuration = 0.2f,
            canJump = true,
            canDash = true
        };
    }

    public void OnActiveUpdate(PlayerController pc) { }

    public void OnEquip(PlayerController pc) { }
    public void Fire(PlayerController pc) { }

    public void OnUnequip(PlayerController pc) { }
}
