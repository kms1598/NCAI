using UnityEngine;

public class NoneAbility : IAbility
{
    public ActivationType Activation => ActivationType.Instant;

    public float GaugeCost => 0f;

    public float DrainPerSecond => 0f;

    public bool ProvidesStats => false;

    public bool IsActive => false;


    public MovementStats GetStats()
    {
        return default;
    }

    public void OnActiveUpdate(PlayerController pc) { }

    public void OnEquip(PlayerController pc) { }
    public void Fire(PlayerController pc) { }

    public void OnUnequip(PlayerController pc) { }
}
