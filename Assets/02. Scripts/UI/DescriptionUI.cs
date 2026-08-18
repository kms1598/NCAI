using UnityEngine;
using TMPro;

public class DescriptionUI : MonoBehaviour
{
    public static DescriptionUI instance;

    public GameObject panel;
    public TMP_Text text;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        instance = this;
        if (panel != null) panel.SetActive(false);
        IsOpen = false;
    }

    public void Open(string description)
    {
        if (panel != null) panel.SetActive(true);
        if (text != null) text.text = description;
        IsOpen = true;
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        IsOpen = false;
    }
}