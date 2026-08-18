using UnityEngine;

public enum ActivationType { Instant, Toggle }
public interface IAbility
{
    ActivationType Activation { get; }
    float GaugeCost { get; }

    float DrainPerSecond { get; }
    bool IsActive { get; }
    MovementStats GetStats();

    void OnEquip(PlayerController pc);
    void OnUnequip(PlayerController pc);
    void Fire(PlayerController pc);
    void OnActiveUpdate(PlayerController pc);
}
