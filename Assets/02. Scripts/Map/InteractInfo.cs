using UnityEngine;

public class InteractInfo : MonoBehaviour, IInteractable
{
    [SerializeField][TextArea] string infoText;

    public void Interact()
    {
        if (!DescriptionUI.instance.IsOpen)
        {
            DescriptionUI.instance.Open(infoText);
            Destroy(gameObject);
        }
    }
}