using UnityEngine;

public class Switch : MonoBehaviour, IArrowHittable, IInteractable
{
    public MonoBehaviour[] targets;
    public bool isSpecialSwitch = false;
    public static bool specialTriggered = false;

    public void OnArrowHit() => Activate();
    public void Interact() => Activate();

    void Activate()
    {
        if (isSpecialSwitch) specialTriggered = true;

        foreach(var t in targets)
        {
            if (t is ISwitchable s) s.SetState();
        }

        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (targets == null) return;

        Gizmos.color = Color.yellow;
        foreach (var t in targets)
        {
            if (t == null) continue;

            Gizmos.DrawLine(transform.position, t.transform.position);
            Gizmos.DrawWireSphere(t.transform.position, 0.3f);
        }
    }
}
