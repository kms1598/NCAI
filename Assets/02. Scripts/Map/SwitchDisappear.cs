using UnityEngine;

public class SwitchDisappear : MonoBehaviour, ISwitchable
{
    public bool isSpecial;

    void OnEnable()
    {
        if(isSpecial && Switch.specialTriggered) gameObject.SetActive(false);
        Switch.OnTriggered += SetState;
    }

    void OnDisable()
    {
        Switch.OnTriggered -= SetState;
    }

    public void SetState()
    {
        gameObject.SetActive(false);
    }
}
