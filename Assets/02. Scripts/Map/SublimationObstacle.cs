using UnityEngine;
public class SublimationObstacle : SublimationGate
{
    protected override void OnSublimationTouch(PlayerController pc)
    {
        Debug.Log("승화 닿음");
    }
}