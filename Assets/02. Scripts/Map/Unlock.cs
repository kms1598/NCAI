using UnityEngine;

public class Unlock : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            other.GetComponent<PlayerController>().Unlock(1);
            gameObject.SetActive(false);
        }
    }
}
