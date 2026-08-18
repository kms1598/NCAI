using System;
using System.Collections;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;

    IAbility[] slots;
    int[] items;
    public int[] Items => items; 
    int[] maxItems = { 0, 4, 4, 3, 0 };
    bool[] unlocked;
    int selectedIndex;
    int currentIndex;
    public int CurrentIndex => currentIndex;
    const int SUBLIMATION_INDEX = 4;
    bool hasFocus = false;
        
    /// <summary>승화 형태로 변신해 있는지입니다.</summary>
    public bool IsSublimation => currentIndex == SUBLIMATION_INDEX;

    public Action<int> OnSelectionChanged;
    public Action<int> OnTransformed;
    public Action<int, int> OnGetItem;
    public Action<int> OnUnlocked;

    /// <summary>능력 사용시 각 모델링을 끄고 켜는 식으로 변신을 구현</summary>
    [SerializeField] GameObject[] models;
    [SerializeField] Avatar[] avatars;
    [SerializeField] Animator animator;

    void Awake()
    {
        if (instance == null) instance = this;
        if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        slots = new IAbility[]
        {
            new NoneAbility(),
            new ArrowAbility(),
            new PlatformAbility(),
            new CrawlAbility(),
            new SublimationAbility()
        };

        items = new int[] {0, 0, 0, 0, 0};

        unlocked = new bool[] { true, false, false, false, false };

        selectedIndex = 0;
        currentIndex = 0;
    }

    void Start()
    {
        // 중복 인스턴스는 Awake에서 파괴 예약만 되고 Start가 불릴 수 있으므로 한 번 더 걸러 냅니다.
        if (instance != this) return;

        // PlayerController.instance는 PlayerController.Awake에서 채워지고
        // Awake끼리는 실행 순서가 보장되지 않으므로 초기 장착은 Start에서 처리합니다.
        PlayerController pc = PlayerController.instance;

        if (pc == null)
        {
            Debug.LogWarning($"{name}: 씬에 PlayerController가 없어 초기 능력 장착을 건너뜁니다.", this);
            return;
        }

        slots[currentIndex].OnEquip(pc);
        // Anim: AbilityIndex — 초기 형태(None)
        pc.Anim?.SetAbilityIndex(currentIndex);

        ApplyModel();
    }

    void Update()
    {
        var cur = slots[currentIndex];
        if(cur.IsActive)
        {
            cur.OnActiveUpdate(PlayerController.instance);
            float drain = cur.DrainPerSecond;

            if(0f < drain && !GaugeSystem.instance.Drain(drain))
            {
                cur.Fire(PlayerController.instance);
                RecalStats();
            }
        }
    }

    public void GetItem(int index)
    {
        if (index < 0 || slots.Length <= index) return;

        // 정해진 개수를 넘겨 모으면 UI에 표시할 조각 칸이 없어 받는 쪽에서 범위를 벗어납니다.
        if (maxItems[index] <= items[index]) return;

        // 능력별 첫 조각: 로딩 패널로 화면을 덮었다가 연 뒤, 스테이지 연출을 이어 붙입니다.
        // ?. 대신 != null로 비교해야 합니다. ?.는 C# 참조만 보기 때문에
        // 파괴된 패널을 가리키고 있어도 통과시켜 버립니다.
        if (items[index] == 0)
            StartCoroutine(PlayFirstItemReveal(index));

        
        items[index]++;
        AudioManager.Play(SFXKey.Piece);
        OnGetItem?.Invoke(index, items[index]);

        if(TryGetSublimation())
        {
            unlocked[SUBLIMATION_INDEX] = true;
            items[SUBLIMATION_INDEX]++;
            AudioManager.Play(SFXKey.CharacterOn);
            OnGetItem?.Invoke(SUBLIMATION_INDEX, items[SUBLIMATION_INDEX]);
            OnUnlocked?.Invoke(SUBLIMATION_INDEX);
        }

        if (unlocked[index]) return;
        unlocked[index] = true;
        AudioManager.Play(SFXKey.CharacterOn);
        OnUnlocked?.Invoke(index);
    }

    /// <summary>
    /// UILoadingPanel Close → Open이 끝난 다음에 StagePanel 연출을 켭니다.
    /// Close() 안에 Open까지 들어 있어서, 그 코루틴이 끝날 때까지 기다리면 됩니다.
    /// </summary>
    IEnumerator PlayFirstItemReveal(int stageIndex)
    {
        if (UILoadingPanel.instance != null)
            yield return UILoadingPanel.instance.Close();

        if (StagePanel.instance != null)
            StagePanel.instance.StartStageStartAnimation(stageIndex);
    }

    bool TryGetSublimation()
    {
        for(int i = 1; i < items.Length; i++)
        {
            if (items[i] < maxItems[i]) return false;
        }

        return true;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || slots.Length <= index) return;
        if (!unlocked[index]) return;
        selectedIndex = index;
        hasFocus = true;
        AudioManager.Play(SFXKey.CharacterSelect);
        OnSelectionChanged?.Invoke(selectedIndex);
    }

    public void Transform()
    {
        int target = hasFocus ? selectedIndex : 0;

        if (target == currentIndex)
        {
            hasFocus = false;
            SelectSlot(0);
            return;
        }
        if (0 < target && !unlocked[selectedIndex]) return;

        slots[currentIndex].OnUnequip(PlayerController.instance);
        currentIndex = target;
        hasFocus = false;
        slots[currentIndex].OnEquip(PlayerController.instance);
        RecalStats();
        // Anim: Transform + AbilityIndex — 능력 형태 전환
        PlayerController.instance.Anim?.PlayTransform(currentIndex);
        AudioManager.Play(SFXKey.CharacterChange);
        OnTransformed?.Invoke(currentIndex);

        //능력 사용시 변신 로직, 해당 능력과 일치하는 순서로 모델과 아바타를 지정해야 함
        ApplyModel();
    }

    public void FireCurrent()
    {
        var cur = slots[currentIndex];

        if(cur.Activation == ActivationType.Instant)
        {
            if (GaugeSystem.instance.TryConsumeGauge(cur.GaugeCost)) cur.Fire(PlayerController.instance);
        }
        else
        {
            if(cur.IsActive)
            {
                cur.Fire(PlayerController.instance);
                RecalStats();
            }
            else
            {
                if (!GaugeSystem.instance.HasGauge(cur.GaugeCost)) return;
                GaugeSystem.instance.TryConsumeGauge(cur.GaugeCost);
                cur.Fire(PlayerController.instance);
                RecalStats();
            }
        }
    }

    void RecalStats()
    {
        var cur = slots[currentIndex];

        if (cur.IsActive) PlayerController.instance.SetStats(cur.GetStats());
        else PlayerController.instance.ResetStats();
    }

    void ApplyModel()
    {
        foreach (var model in models)
        {
            model.gameObject.SetActive(false);
        }

        if (models[currentIndex] != null) models[currentIndex].gameObject.SetActive(true);
        if (avatars[currentIndex] != null) animator.avatar = avatars[currentIndex];
    }
}
