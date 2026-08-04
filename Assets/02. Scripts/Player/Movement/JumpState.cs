using Unity.VisualScripting;
using UnityEngine;

public class JumpState : PlayerState
{
    public JumpState(PlayerController pc) : base(pc) { }

    public override void Update()
    {
        ApplyHorizontalMove();

        if(pc.dashPressed && pc.Stats.canDash && !pc.dashUsedInAir && PlayerController.instance.canDash)
        {
            pc.TransitionTo(pc.Dash);
            return;
        }

        pc.velocity.y += pc.gravity * Time.deltaTime;
        Move();

        if(pc.cc.isGrounded && pc.velocity.y <= 0f)
        {
            pc.TransitionTo(pc.Ground);
        }
    }
}
