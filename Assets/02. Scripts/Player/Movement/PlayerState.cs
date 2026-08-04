using UnityEngine;

public abstract class PlayerState
{
    protected PlayerController pc;
    protected PlayerState(PlayerController pc) => this.pc = pc;

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }

    protected void ApplyHorizontalMove()
    {
        Vector3 move = pc.GetCameraRelativeMove() * pc.Stats.moveSpeed;
        pc.velocity.x = move.x;
        pc.velocity.z = move.z;
    }

    protected void Move()
    {
        pc.cc.Move(pc.velocity * Time.deltaTime);
    }

}
