using UnityEngine;

public class SwitchDisappear : MonoBehaviour, ISwitchable
{
    public bool isSpecial;

    public void OnEnable()
    {
        if(isSpecial && Switch.specialTriggered) gameObject.SetActive(false);
    }

    public void SetState()
    {
        gameObject.SetActive(false);
    }
}
