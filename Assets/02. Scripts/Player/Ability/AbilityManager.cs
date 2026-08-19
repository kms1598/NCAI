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

    // 컷씬 영역에서 돌아온 뒤 재생할 스테이지 연출 번호입니다.
    // -1이면 대기 중인 연출이 없습니다.
    int pendingStageIntro = -1;

    // 컷씬으로 떠나기 전 위치·방. ScenenDoor로 돌아올 때 씁니다.
    bool hasReturnPose;
    Vector3 returnPosition;
    string returnRoomId;

    /// <summary>컷씬 복귀용 위치가 저장돼 있는지입니다.</summary>
    public bool HasReturnPose => hasReturnPose;
    public Vector3 ReturnPosition => returnPosition;
    public string ReturnRoomId => returnRoomId;

    bool isCutsceneTransition;
        
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

    void OnDestroy()
    {
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

        // 능력별 첫 조각: 화면을 덮은 뒤 해당 스테이지 컷씬 맵 위치로 텔레포트합니다.
        // 컷씬 문(ScenenDoor)으로 돌아오면 저장 위치 복귀 + StartStageStartAnimation을 재생합니다.
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
    /// 로딩 패널로 화면을 덮은 뒤, 스테이지 컷씬 맵 스폰으로 텔레포트합니다.
    /// 스테이지 연출은 ScenenDoor로 원래 위치에 돌아온 뒤 재생합니다.
    /// </summary>
    IEnumerator PlayFirstItemReveal(int stageIndex)
    {
        if (isCutsceneTransition) yield break;

        isCutsceneTransition = true;
        pendingStageIntro = stageIndex;
        RememberReturnPose();

        if (UILoadingPanel.instance != null)
            yield return UILoadingPanel.instance.CloseAndHold();

        TeleportToCutscene(stageIndex);

        // 텔레포트 직후 데스존 OnTriggerStay가 한 번 돌 수 있어 전환 플래그를 유지한 채 한 프레임 쉽니다.
        yield return null;

        if (UILoadingPanel.instance != null)
            yield return UILoadingPanel.instance.Open();

        // 컷씬 맵에 도착한 뒤 Stage Data의 Timeline을 재생합니다.
        if (StagePanel.instance != null)
            StagePanel.instance.PlayStageTimeline(stageIndex);

        isCutsceneTransition = false;
    }

    /// <summary>컷씬 진입/복귀 연출 중입니다. 이 동안 사망 트리거를 무시합니다.</summary>
    public bool IsCutsceneTransition => isCutsceneTransition;

    /// <summary>지금 플레이어 위치와 방을 저장해, 컷씬에서 돌아올 때 그대로 되돌립니다.</summary>
    void RememberReturnPose()
    {
        if (PlayerController.instance == null) return;

        hasReturnPose = true;
        returnPosition = PlayerController.instance.transform.position;
        returnRoomId = RoomManager.instance != null ? RoomManager.instance.CurrentRoomId : null;
        // 리스폰 포인트는 건드리지 않습니다.
        // 여기서 이전 좌표로 바꿔 두면, 컷씬 도착 직후 낙하/데스존에 닿을 때
        // 조각 위치로 되돌아가고 카메라만 컷씬에 남는 문제가 납니다.
    }

    public void ClearReturnPose()
    {
        hasReturnPose = false;
        returnRoomId = null;
    }

    /// <summary>
    /// StagePanel에 등록된 해당 스테이지의 컷씬 스폰 오브젝트 위치로 보냅니다.
    /// </summary>
    void TeleportToCutscene(int stageIndex)
    {
        if (StagePanel.instance == null)
        {
            Debug.LogWarning($"{name}: StagePanel이 없어 컷씬 스폰으로 이동하지 못했습니다.", this);
            return;
        }

        Transform spawn = StagePanel.instance.GetCutsceneSpawn(stageIndex);
        if (spawn == null)
        {
            Debug.LogWarning($"{name}: Stage {stageIndex}의 Cutscene Spawn이 비어 있습니다. StagePanel stages를 확인하세요.", this);
            return;
        }

        Vector3 destination = spawn.position;
        Transform cameraCenter = StagePanel.instance.GetCutsceneCameraCenter(stageIndex);
        bool tallRoom = StagePanel.instance.GetCutsceneTallRoom(stageIndex);

        TeleportPlayer(destination);
        PlayerRespawn respawn = PlayerController.instance.GetComponent<PlayerRespawn>();
        if (respawn != null)
        {
            respawn.SetSpawn(destination);
            // 텔레포트로 데스존 안에 들어가면 Enter 대신 Stay만 오므로, 도착 직후는 무시합니다.
            respawn.IgnoreDeathFor(1f);
        }

        if (CameraSpin.instance != null)
        {
            CameraSpin.instance.SetRoom(cameraCenter != null ? cameraCenter.position : destination, tallRoom);
            CameraSpin.instance.SetFollowPlayer(true);
        }
    }

    void TeleportPlayer(Vector3 position)
    {
        PlayerController pc = PlayerController.instance;
        if (pc == null) return;

        if (pc.cc != null) pc.cc.enabled = false;
        pc.transform.position = position;
        Physics.SyncTransforms();
        if (pc.cc != null) pc.cc.enabled = true;
        pc.ResetVelocity();
    }

    /// <summary>
    /// 컷씬 종료 문(ScenenDoor)에서 호출합니다.
    /// 저장해 둔 위치로 되돌린 뒤 스테이지 연출을 이어 붙입니다.
    /// </summary>
    public void ReturnFromCutscene()
    {
        if (isCutsceneTransition) return;
        if (!hasReturnPose && pendingStageIntro < 0) return;

        StartCoroutine(ReturnFromCutsceneRoutine());
    }

    IEnumerator ReturnFromCutsceneRoutine()
    {
        isCutsceneTransition = true;

        // 화면을 먼저 덮습니다. 여기서 follow를 끄면 roomCenter(스테이지 방 중심)로
        // 카메라가 순간 이동해 컷씬이 깨집니다.
        if (UILoadingPanel.instance != null)
            yield return UILoadingPanel.instance.CloseAndHold();

        if (StagePanel.instance != null)
            StagePanel.instance.StopActiveTimeline();

        if (hasReturnPose)
        {
            Vector3 spawnBack = returnPosition;

            // 복귀 좌표로 옮긴 뒤에야 방 중심 카메라로 전환합니다.
            if (CameraSpin.instance != null)
                CameraSpin.instance.SetFollowPlayer(false);

            if (RoomManager.instance != null)
                RoomManager.instance.ApplyReturnPose(this);
            else
                RestoreReturnPoseWithoutRoom();

            PlayerRespawn respawn = PlayerController.instance != null
                ? PlayerController.instance.GetComponent<PlayerRespawn>()
                : null;
            if (respawn != null)
            {
                respawn.SetSpawn(spawnBack);
                respawn.IgnoreDeathFor(1f);
            }
        }
        else if (CameraSpin.instance != null)
        {
            CameraSpin.instance.SetFollowPlayer(false);
        }

        // 트리거 잔여 판정이 한 프레임 더 돌 수 있어 한 박자 쉰 뒤 연출을 엽니다.
        yield return null;

        if (UILoadingPanel.instance != null)
            yield return UILoadingPanel.instance.Open();

        int stageIndex = pendingStageIntro;
        pendingStageIntro = -1;
        isCutsceneTransition = false;

        if (stageIndex >= 0 && StagePanel.instance != null)
            StagePanel.instance.StartStageStartAnimation(stageIndex);
    }

    void RestoreReturnPoseWithoutRoom()
    {
        TeleportPlayer(returnPosition);

        if (CameraSpin.instance != null)
        {
            CameraSpin.instance.SetFollowPlayer(false);
            CameraSpin.instance.SetRoom(returnPosition, false);
        }

        ClearReturnPose();
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

        // 능력 사용시 변신 로직, 해당 능력과 일치하는 순서로 모델과 아바타를 지정해야 함
        // 아바타 교체 시 Animator가 기본 상태(Stand Up)로 돌아가므로 건너뜁니다.
        ApplyModel(skipStandUp: true);
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

    void ApplyModel(bool skipStandUp = false)
    {
        foreach (var model in models)
        {
            model.gameObject.SetActive(false);
        }

        if (models[currentIndex] != null) models[currentIndex].gameObject.SetActive(true);
        if (avatars[currentIndex] != null) animator.avatar = avatars[currentIndex];

        // 아바타 교체 시 기본 상태(Stand Up)로 리셋되므로, 변신 시에만 Locomotion으로 넘깁니다.
        if (skipStandUp && animator != null)
        {
            animator.Play("Locomotion", 0, 0f);
            animator.Update(0f);
        }
    }
}
