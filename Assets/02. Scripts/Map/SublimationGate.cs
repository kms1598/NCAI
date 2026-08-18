using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class SublimationGate : MonoBehaviour
{
    const string SublimationLayerName = "Sublimation";

    int sublimationLayer;
    PlayerController inside;

    bool wasSublimation;

    protected virtual void Awake()
    {
        sublimationLayer = LayerMask.NameToLayer(SublimationLayerName);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        inside = pc;
        Evaluate();
    }

    void OnTriggerExit(Collider other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null || pc != inside) return;

        inside = null;
        wasSublimation = false;
    }

    void Update()
    {
        if (inside != null) Evaluate();
    }

    void Evaluate()
    {
        bool now = 0 <= sublimationLayer && inside.gameObject.layer == sublimationLayer;

        if (now && !wasSublimation) OnSublimationTouch(inside);

        wasSublimation = now;
    }

    protected abstract void OnSublimationTouch(PlayerController pc);
}