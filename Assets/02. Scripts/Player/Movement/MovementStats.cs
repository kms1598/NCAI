using UnityEngine;

[System.Serializable]
public class MovementStats
{
    public float moveSpeed;
    public float jumpHeight;
    public float dashSpeed;
    public float dashDuration;
    public bool canJump;
    public bool canDash;

    public static MovementStats Default => new MovementStats
    {
        moveSpeed = 3f,
        jumpHeight = 1.5f,
        dashSpeed = 20f,
        dashDuration = 0.2f,
        canJump = true,
        canDash = true
    };
}
