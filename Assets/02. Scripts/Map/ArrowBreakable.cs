using UnityEngine;

public class ArrowBreakable : MonoBehaviour, IArrowHittable
{
    public void OnArrowHit()
    {
        AudioManager.PlayAt(SFXKey.Box, transform.position);
        Destroy(gameObject);
    }
}
