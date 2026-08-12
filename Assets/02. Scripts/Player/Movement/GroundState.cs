using System;
using UnityEngine;

public class GroundState : PlayerState
{
    public GroundState(PlayerController pc) : base(pc) { }

    public override void Enter()
    {
        pc.dashUsedInAir = false;
        if (pc.velocity.y < 0) pc.velocity.y = -2f;
    }

    public override void Update()
    {
        ApplyHorizontalMove();

        if(pc.dashPressed && pc.Stats.canDash && PlayerController.instance.canDash)
        {
            pc.TransitionTo(pc.Dash);
            return;
        }

        if (pc.jumpPressed && pc.Stats.canJump)
        {
            pc.velocity.y = MathF.Sqrt(2f * -pc.gravity * pc.Stats.jumpHeight);
            // Anim: Jump — 점프 시작
            pc.Anim?.PlayJump();
            AudioManager.Play(SFXKey.Jump);
            pc.TransitionTo(pc.Jump);
            return;
        }

        if(!pc.cc.isGrounded)
        {
            pc.TransitionTo(pc.Jump);
            return;
        }

        pc.velocity.y += pc.gravity * Time.deltaTime;
        Move();
    }
}
