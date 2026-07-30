using UnityEngine;

public class Trigger : MonoBehaviour
{
    
    [SerializeField] GameObject[] targets;

    bool used;

    void OnTriggerEnter(Collider other)
    {
        if (used) return;

        foreach (var t in targets)
            t.GetComponent<Activatable>().Activate();
        
        used = true;
    }
}
