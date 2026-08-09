using System;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;

    IAbility[] slots;
    int[] items;
    int[] maxItems = { 0, 4, 4, 3, 0 };
    bool[] unlocked;
    int selectedIndex;
    int currentIndex;
    const int SUBLIMATION_INDEX = 4;

    public Action<int> OnSelectionChanged;
    public Action<int> OnTransformed;
    public Action<int, int> OnGetItem;
    public Action<int> OnUnlocked;

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
        slots[currentIndex].OnEquip(PlayerController.instance);
        // Anim: AbilityIndex — 초기 형태(None)
        PlayerController.instance.Anim?.SetAbilityIndex(currentIndex);
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
        items[index]++;
        OnGetItem?.Invoke(index, items[index]);

        if(TryGetSublimation())
        {
            unlocked[SUBLIMATION_INDEX] = true;
            items[SUBLIMATION_INDEX]++;
            OnGetItem?.Invoke(SUBLIMATION_INDEX, items[SUBLIMATION_INDEX]);
            OnUnlocked?.Invoke(SUBLIMATION_INDEX);
        }

        if (unlocked[index]) return;
        unlocked[index] = true;
        OnUnlocked?.Invoke(index);
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
        OnSelectionChanged?.Invoke(selectedIndex);
    }

    public void Transform()
    {
        if (selectedIndex == currentIndex) return;
        if (!unlocked[selectedIndex]) return;

        slots[currentIndex].OnUnequip(PlayerController.instance);
        currentIndex = selectedIndex;
        slots[currentIndex].OnEquip(PlayerController.instance);
        RecalStats();
        // Anim: Transform + AbilityIndex — 능력 형태 전환
        PlayerController.instance.Anim?.PlayTransform(currentIndex);
        OnTransformed?.Invoke(currentIndex);
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

        if (cur.ProvidesStats && cur.IsActive) PlayerController.instance.SetStats(cur.GetStats());
        else PlayerController.instance.ResetStats();
    }
}
