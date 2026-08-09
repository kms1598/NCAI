using UnityEngine;

public class DashState : PlayerState
{
    private float timer;
    private Vector3 dashVelocity;
    public DashState(PlayerController pc) : base(pc) { }

    public override void Enter()
    {
        timer = pc.Stats.dashDuration;
        if (!pc.cc.isGrounded) pc.dashUsedInAir = true;

        Vector3 dir = pc.aimDir;
        dir.y = 0;
        dir.Normalize();

        if (dir.sqrMagnitude < 0.01f) dir = pc.transform.forward;
        dashVelocity = dir * pc. Stats.dashSpeed;
        pc.velocity = new Vector3(dashVelocity.x, 0f, dashVelocity.z);

        PlayerController.instance.MarkDashUsedTime();
        // Anim: Dash — 대시 연출
        pc.Anim?.PlayDash();
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        pc.velocity = new Vector3(dashVelocity.x, 0f, dashVelocity.z);
        Move();

        if(timer <= 0f)
        {
            pc.velocity.y = 0f;
            pc.TransitionTo(pc.Jump);
        }
    }
}
