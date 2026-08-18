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

    // 컷씬이 끝나고 Prototype 계열 씬으로 돌아온 뒤 재생할 스테이지 연출 번호입니다.
    // -1이면 대기 중인 연출이 없습니다.
    int pendingStageIntro = -1;

    // Prototype 재로드 때 RoomManager가 시작 스폰으로 끌어가지 않도록,
    // 조각을 먹었던 위치와 방을 기억해 둡니다.
    bool hasReturnPose;
    Vector3 returnPosition;
    string returnRoomId;

    /// <summary>컷씬 복귀용 위치가 저장돼 있는지입니다. RoomManager.Start에서 확인합니다.</summary>
    public bool HasReturnPose => hasReturnPose;
    public Vector3 ReturnPosition => returnPosition;
    public string ReturnRoomId => returnRoomId;
        
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

        SceneLoader.OnLoadCompleted += OnSceneLoadCompleted;
    }

    void OnDestroy()
    {
        SceneLoader.OnLoadCompleted -= OnSceneLoadCompleted;
        if (instance == this) instance = null;
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

        // 능력별 첫 조각: 화면을 덮은 뒤 해당 스테이지 컷씬으로 넘어갑니다.
        // 컷씬이 끝나고 Prototype 계열 씬으로 돌아오면 StartStageStartAnimation을 재생합니다.
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

    /// <summary>stage1CutScene처럼 스테이지 번호로 컷씬 씬 이름을 만듭니다.</summary>
    public static string GetStageCutsceneName(int stageIndex)
    {
        return "stage" + stageIndex + "CutScene";
    }

    /// <summary>
    /// 로딩 패널로 화면을 덮은 뒤 stage{n}CutScene으로 전환합니다.
    /// 스테이지 연출은 컷씬이 끝나고 Prototype 씬으로 돌아왔을 때 이어서 재생합니다.
    /// </summary>
    IEnumerator PlayFirstItemReveal(int stageIndex)
    {
        pendingStageIntro = stageIndex;
        RememberReturnPose();

        string cutsceneName = GetStageCutsceneName(stageIndex);

        // CloseAndHold로 화면을 덮은 채 유지합니다.
        // SceneLoader가 컷씬을 불러온 뒤 Open으로 걷어냅니다.
        if (UILoadingPanel.instance != null)
            yield return UILoadingPanel.instance.CloseAndHold();

        SceneLoader.Load(cutsceneName);
    }

    /// <summary>지금 플레이어 위치와 방을 저장해, Prototype 재진입 때 그대로 되돌립니다.</summary>
    void RememberReturnPose()
    {
        if (PlayerController.instance == null) return;

        hasReturnPose = true;
        returnPosition = PlayerController.instance.transform.position;
        returnRoomId = RoomManager.instance != null ? RoomManager.instance.CurrentRoomId : null;

        // 리스폰도 같은 지점으로 맞춰 두면, 복귀 직후 떨어져도 조각 근처로 돌아옵니다.
        PlayerController.instance.GetComponent<PlayerRespawn>()?.SetSpawn(returnPosition);
    }

    public void ClearReturnPose()
    {
        hasReturnPose = false;
        returnRoomId = null;
    }

    void OnSceneLoadCompleted(string sceneName)
    {
        if (pendingStageIntro < 0) return;

        // 컷씬으로 들어갈 때는 연출을 미룹니다. Prototype 계열로 돌아올 때만 재생합니다.
        if (sceneName.IndexOf("CutScene", StringComparison.OrdinalIgnoreCase) >= 0) return;

        StartCoroutine(PlayPendingStageIntroAfterLoad());
    }

    /// <summary>
    /// SceneLoader.OnLoadCompleted는 Open 연출보다 먼저 불립니다.
    /// 로딩 커튼이 다 걷힌 뒤에 StagePanel을 띄워야 겹치지 않습니다.
    /// </summary>
    IEnumerator PlayPendingStageIntroAfterLoad()
    {
        // RoomManager.Start가 이 사이에 조각 위치로 되돌립니다.
        while (SceneLoader.IsLoading)
            yield return null;

        yield return null;

        // RoomManager가 없는 씬이거나 Start 순서가 어긋난 경우 대비입니다.
        if (hasReturnPose)
        {
            if (RoomManager.instance != null)
                RoomManager.instance.ApplyReturnPose(this);
            else if (PlayerController.instance != null)
            {
                var pc = PlayerController.instance;
                pc.cc.enabled = false;
                pc.transform.position = returnPosition;
                pc.cc.enabled = true;
                pc.ResetVelocity();
                ClearReturnPose();
            }
        }

        int stageIndex = pendingStageIntro;
        pendingStageIntro = -1;

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
        foreach(var model in models)
        {
            model.gameObject.SetActive(false);
        }

        if (models[currentIndex] != null) models[currentIndex].gameObject.SetActive(true);
        if (avatars[currentIndex] != null)  animator.avatar = avatars[currentIndex];
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
