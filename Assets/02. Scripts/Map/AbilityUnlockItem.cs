using UnityEngine;

public class AbilityUnlockItem : MonoBehaviour, IInteractable
{
    public int abilityIndex = 0;

    public void Interact()
    {
        AbilityManager.instance.GetItem(abilityIndex);
        Destroy(gameObject);
    }
}