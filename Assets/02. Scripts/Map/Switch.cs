using System;
using UnityEngine;

public class Switch : MonoBehaviour, IArrowHittable, IInteractable
{
    public MonoBehaviour[] targets;
    public bool isSpecialSwitch = false;
    public static bool specialTriggered = false;
    public static event Action OnTriggered;

    public void OnArrowHit() => Activate();
    public void Interact() => Activate();

    void Activate()
    {
        // AudioManager가 소리를 대신 내 주므로, 바로 아래에서 이 오브젝트를 꺼도 소리는 끝까지 재생됩니다.
        AudioManager.PlayAt(SFXKey.Switch, transform.position);

        if (isSpecialSwitch)
        {
            OnTriggered?.Invoke();
            specialTriggered = true;
        }

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
