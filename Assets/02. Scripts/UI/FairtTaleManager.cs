using UnityEngine;
using UnityEngine.UI;

public class FairyTaleManager : MonoBehaviour
{
    [SerializeField] GameObject book;
    [SerializeField] Button chapter1Button;
    [SerializeField] Button chapter2Button;
    [SerializeField] Button chapter3Button;
    [SerializeField] Button nextButton;
    [SerializeField] Button prevButton;

    int[] maxPage = { 4, 4, 3 };
    public int[] unlockPage = { 0, 0, 0 };
    int nowChpater = 0;
    int nowPage = 0;

    [SerializeField] Sprite[] fairyTales1;
    [SerializeField] Sprite[] fairyTales2;
    [SerializeField] Sprite[] fairyTales3;
    Sprite[][] fairyTales = new Sprite[3][];

    [SerializeField] GameObject fairyTale;
    [SerializeField] Image page;

    private void Start()
    {
        fairyTales[0] = fairyTales1;
        fairyTales[1] = fairyTales2;
        fairyTales[2] = fairyTales3;
    }

    private void OnEnable()
    {
        unlockPage[0] = AbilityManager.instance.Items[1];
        unlockPage[1] = AbilityManager.instance.Items[2];
        unlockPage[2] = AbilityManager.instance.Items[3];
        OpenBook();
    }

    void OpenBook()
    {
        book.SetActive(true);
        chapter1Button.interactable = 0 < unlockPage[0];
        chapter1Button.GetComponent<Image>().color = 0 < unlockPage[0] ? Color.white : Color.black;
        chapter2Button.interactable = 0 < unlockPage[1];
        chapter2Button.GetComponent<Image>().color = 0 < unlockPage[1] ? Color.white : Color.black;
        chapter3Button.interactable = 0 < unlockPage[2];
        chapter3Button.GetComponent<Image>().color = 0 < unlockPage[2] ? Color.white : Color.black;
    }

    public void Open(int chapter)
    {
        nowChpater = chapter;
        nowPage = 0;
        OpenPage();
    }

    void OpenPage()
    {
        fairyTale.gameObject.SetActive(true);
        if (maxPage[nowChpater] - 1 <= nowPage) nextButton.gameObject.SetActive(false);
        else nextButton.gameObject.SetActive(true);

        if (nowPage <= 0) prevButton.gameObject.SetActive(false);
        else prevButton.gameObject.SetActive(true);

        page.sprite = fairyTales[nowChpater][nowPage];
        if(nowPage < unlockPage[nowChpater]) page.color = Color.white;
        else page.color = Color.black;
    }

    public void NextPage()
    {
        nowPage++;
        OpenPage();
    }

    public void PrevPage()
    {
        nowPage--;
        OpenPage();
    }
}
