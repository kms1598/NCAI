using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public GameObject interactUI;
    IInteractable current;

    public void OnInteract(InputValue v)
    {
        if (v.isPressed && current != null)
        {
            if (interactUI != null) interactUI.SetActive(false);
            current.Interact();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var it = other.GetComponentInParent<IInteractable>();
        if(it != null)
        {
            current = it;
            if (interactUI != null) interactUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        var it = other.GetComponentInParent<IInteractable>();
        if (it != null && it == current)
        {
            current = null;
            if (interactUI != null) interactUI.SetActive(false);
        }
    }
}
