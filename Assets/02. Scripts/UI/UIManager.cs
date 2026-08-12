using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject[] arrowPiece;
    [SerializeField] GameObject[] crawlPiece;
    [SerializeField] GameObject[] platformPiece;
    [SerializeField] GameObject[] sublimationPiece;
    GameObject[][] piece = new GameObject[4][];

    [SerializeField] Image abilityIcon;
    [SerializeField] Sprite[] abilitySprite;

    [SerializeField] GameObject[] abilityLock;
    [SerializeField] GameObject[] abilityFocus;

    void Start()
    {
        AbilityManager.instance.OnSelectionChanged += FocusAbility;
        AbilityManager.instance.OnTransformed += Transform;
        AbilityManager.instance.OnGetItem += GetPiece;
        AbilityManager.instance.OnUnlocked += Unlock;

        piece[0] = arrowPiece;
        piece[1] = platformPiece;
        piece[2] = crawlPiece;
        piece[3] = sublimationPiece;
    }

    private void OnDisable()
    {
        AbilityManager.instance.OnSelectionChanged -= FocusAbility;
        AbilityManager.instance.OnTransformed -= Transform;
        AbilityManager.instance.OnGetItem -= GetPiece;
        AbilityManager.instance.OnUnlocked -= Unlock;
    }

    void GetPiece(int index, int amount)
    {
        piece[index - 1][amount - 1].SetActive(true);
    }

    void Unlock(int index)
    {
        abilityLock[index - 1].SetActive(false);
    }

    void FocusAbility(int index)
    {
        foreach(var f in abilityFocus) f.SetActive(false);

        if(index != 0) abilityFocus[index - 1].SetActive(true);
    }

    void Transform(int index)
    {
        abilityIcon.sprite = abilitySprite[index];
        foreach (var f in abilityFocus) f.SetActive(false);
    }
}
