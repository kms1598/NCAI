using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 목록입니다. 값은 Build Settings의 순서와 같습니다.
/// 코드 여기저기에 씬 이름 문자열을 흘리지 않으려고 만들었습니다.
/// </summary>
public enum GameScene
{
    Logo = 0,
    Title = 1,
    Prototype = 2,
    Ending = 3
}

/// <summary>
/// 씬 전환을 담당합니다. SceneLoader.Load(GameScene.Title)처럼 어디서든 바로 부르면 됩니다.
///
/// 이름을 SceneManager로 두지 않은 이유가 있습니다. 전역 이름공간에 SceneManager 클래스를 만들면
/// UnityEngine.SceneManagement.SceneManager를 가려 버려서, 이미 그것을 쓰고 있는 CutsceneLoader의
/// SceneManager.LoadScene 호출이 깨집니다.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader instance;

    /// <summary>전환이 시작될 때 씬 이름과 함께 알립니다. 화면을 덮는 연출을 걸 지점입니다.</summary>
    public static event Action<string> OnLoadStarted;
    /// <summary>새 씬이 올라온 뒤에 알립니다. 화면을 걷어내는 연출을 걸 지점입니다.</summary>
    public static event Action<string> OnLoadCompleted;

    [Tooltip("이 씬으로 갈 때는 플레이어와 진행 상황도 함께 정리합니다. 로고나 타이틀로 돌아갈 때 필요합니다.")]
    [SerializeField] GameScene[] resetScenes = { GameScene.Logo, GameScene.Title, GameScene.Ending };
    [Tooltip("UILoadingPanel의 Close/Open 연출로 화면을 덮고 걷어냅니다. 패널이 없으면 그냥 바로 전환합니다.")]
    [SerializeField] bool useLoadingPanel = true;

    string previousSceneName;
    Coroutine loading;

    public static bool IsLoading => instance != null && instance.loading != null;
    /// <summary>0에서 1 사이의 로딩 진행도입니다.</summary>
    public static float Progress { get; private set; }
    /// <summary>직전에 있던 씬 이름입니다. 아직 전환한 적이 없으면 null입니다.</summary>
    public static string PreviousScene => instance != null ? instance.previousSceneName : null;

    void Awake()
    {
        if (instance == null) instance = this;
        if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    #region 전환 요청

    public static void Load(GameScene scene)
    {
        string sceneName = GetSceneName(scene);

        if (sceneName == null)
        {
            Debug.LogWarning($"SceneLoader: {scene}에 연결된 씬 이름이 없습니다. GetSceneName을 확인하세요.");
            return;
        }

        SceneLoader loader = Ensure();

        // 타이틀·엔딩처럼 게임 진행을 처음부터 시작해야 하는 씬이면 남아 있는 싱글톤도 정리합니다.
        bool reset = 0 <= Array.IndexOf(loader.resetScenes, scene)
            || scene == GameScene.Ending
            || scene == GameScene.Logo
            || scene == GameScene.Title;

        loader.Begin(sceneName, reset);
    }

    /// <summary>Build Settings 순서에 맞춘 인덱스로 전환합니다.</summary>
    public static void Load(int index)
    {
        if (!Enum.IsDefined(typeof(GameScene), index))
        {
            Debug.LogWarning($"SceneLoader: {index}번에 해당하는 씬이 GameScene에 없습니다.");
            return;
        }

        Load((GameScene)index);
    }

    /// <summary>GameScene에 없는 씬을 이름으로 직접 여는 경우에 씁니다.</summary>
    public static void Load(string sceneName)
    {
        Ensure().Begin(sceneName, false);
    }

    /// <summary>지금 씬을 다시 불러옵니다.</summary>
    public static void Reload()
    {
        Ensure().Begin(SceneManager.GetActiveScene().name, false);
    }

    /// <summary>직전 씬으로 돌아갑니다.</summary>
    public static void LoadPrevious()
    {
        SceneLoader loader = Ensure();

        if (string.IsNullOrEmpty(loader.previousSceneName))
        {
            Debug.LogWarning("SceneLoader: 돌아갈 직전 씬이 없습니다.");
            return;
        }

        loader.Begin(loader.previousSceneName, false);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    /// <summary>인덱스와 실제 씬 파일 이름을 잇는 곳입니다. 씬을 추가하면 여기와 enum을 같이 고치세요.</summary>
    public static string GetSceneName(GameScene scene)
    {
        switch (scene)
        {
            case GameScene.Logo: return "LogoScene";
            case GameScene.Title: return "TitleScene";
            case GameScene.Prototype: return "PrototypeScene";
            case GameScene.Ending: return "EndingScene";
            default: return null;
        }
    }

    /// <summary>씬에 SceneLoader를 두지 않았어도 처음 부를 때 스스로 만들어집니다.</summary>
    static SceneLoader Ensure()
    {
        if (instance != null) return instance;

        GameObject holder = new GameObject(nameof(SceneLoader));
        instance = holder.AddComponent<SceneLoader>();

        return instance;
    }

    void Begin(string sceneName, bool reset)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        if (loading != null)
        {
            // 전환 중에 버튼을 두 번 누르는 경우입니다. 먼저 시작한 전환을 그대로 둡니다.
            Debug.LogWarning($"{name}: 이미 씬을 불러오는 중이라 '{sceneName}' 요청을 건너뜁니다.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"{name}: '{sceneName}' 씬이 Build Settings에 없습니다. File > Build Profiles에서 추가하세요.", this);
            return;
        }

        loading = StartCoroutine(LoadRoutine(sceneName, reset));
    }

    IEnumerator LoadRoutine(string sceneName, bool reset)
    {
        Progress = 0f;
        previousSceneName = SceneManager.GetActiveScene().name;
        OnLoadStarted?.Invoke(sceneName);

        // Close 연출이 끝나 화면이 완전히 덮인 뒤에 씬을 바꿉니다.
        // 먼저 바꾸면 로딩 중에 새 씬이 그대로 보여 버립니다.
        UILoadingPanel panel = useLoadingPanel ? UILoadingPanel.instance : null;
        if (panel != null) yield return panel.CloseAndHold();

        // 일시정지 메뉴에서 씬을 옮기면 timeScale이 0으로 남아 새 씬이 멈춘 채 시작합니다.
        Time.timeScale = 1f;

        // 화면이 덮인 뒤에 정리합니다. 먼저 지우면 플레이어가 사라지는 장면이 보입니다.
        if (reset) ResetPersistentObjects();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        while (op != null && !op.isDone)
        {
            Progress = op.progress;
            yield return null;
        }

        Progress = 1f;
        OnLoadCompleted?.Invoke(sceneName);

        // 새 씬의 Awake와 Start가 한 번 돌게 두고 나서 화면을 걷어냅니다.
        // 바로 열면 아직 배치가 끝나지 않은 장면이 잠깐 보입니다.
        yield return null;

        // 패널을 다시 찾습니다. 씬 전환 중 패널이 교체됐을 수 있습니다.
        if (useLoadingPanel) panel = UILoadingPanel.instance;
        if (panel != null) yield return panel.Open();

        loading = null;
    }

    /// <summary>
    /// DontDestroyOnLoad로 살아남는 게임 진행용 오브젝트를 정리합니다.
    /// 이걸 하지 않으면 타이틀 화면에 플레이어가 그대로 남고, 모았던 조각도 유지됩니다.
    /// AudioManager는 어느 씬에서나 필요하므로 건드리지 않습니다.
    /// </summary>
    void ResetPersistentObjects()
    {
        DestroyIfAlive(PlayerController.instance);
        DestroyIfAlive(AbilityManager.instance);
        DestroyIfAlive(GaugeSystem.instance);
    }

    static void DestroyIfAlive(MonoBehaviour target)
    {
        if (target == null) return;

        Destroy(target.gameObject);
    }
}
