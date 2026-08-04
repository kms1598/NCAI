using UnityEngine;

public class ArrowBreakable : MonoBehaviour, IArrowHittable
{
    public void OnArrowHit()
    {
        Destroy(gameObject);
    }
}
