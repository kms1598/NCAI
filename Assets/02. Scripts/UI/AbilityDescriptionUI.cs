using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class AbilityDescriptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject panel;
    public TMP_Text text;
    bool hovering;

    [TextArea] public string[] descriptions;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }
    void Start()
    {
        if (AbilityManager.instance != null)
            AbilityManager.instance.OnTransformed += OnTransformed;
    }
    void OnDestroy()
    {
        if (AbilityManager.instance != null)
            AbilityManager.instance.OnTransformed -= OnTransformed;
    }

    void OnTransformed(int index)
    {
        if (hovering) ShowCurrent();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hovering = true;
        ShowCurrent();
    }
    public void OnPointerExit(PointerEventData e)
    {
        hovering = false;
        Hide();
    }

    private void ShowCurrent()
    {
        if (AbilityManager.instance == null) return;

        int index = AbilityManager.instance.CurrentIndex;
        if (index < 0 || index >= descriptions.Length) return;

        if (panel != null) panel.SetActive(true);
        if (text != null) text.text = descriptions[index];
    }

    private void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}