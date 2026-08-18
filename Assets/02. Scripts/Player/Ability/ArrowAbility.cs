using UnityEngine;

public class ArrowAbility : IAbility
{
    public ActivationType Activation => ActivationType.Instant;

    public float GaugeCost => 5f;

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
    public void Fire(PlayerController pc)
    {
        if (pc.arrowPrefab == null) return;

        Vector3 dir = pc.aimDir;
        dir.y = 0;
        dir.Normalize();

        Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, -90, 0);

        var arrow = Object.Instantiate(pc.arrowPrefab, pc.transform.position + dir, rot);
        arrow.GetComponent<Transform>().position += Vector3.up;
        var rb = arrow.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 10f;
        // Anim: Shoot — 화살 발사
        pc.Anim?.PlayShoot();
        AudioManager.Play(SFXKey.Bow);
    }

    public void OnUnequip(PlayerController pc) { }
}
