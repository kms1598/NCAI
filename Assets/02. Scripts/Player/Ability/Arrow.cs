using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float lifeTime = 5f;

    void Start() => Destroy(gameObject, lifeTime);

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;

        var hittable = other.GetComponentInParent<IArrowHittable>();
        if (hittable != null) hittable.OnArrowHit();
        Destroy(gameObject);
    }
}
