using UnityEngine;

public class Activatable : MonoBehaviour
{
    public void Activate()
    {
        GetComponent<Rigidbody>().useGravity = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player") Debug.Log("Clear");
        gameObject.SetActive(false);
    }
}
