using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneLoader : MonoBehaviour
{
    public static CutsceneLoader instance;

    public MonoBehaviour[] toPause;
    public Camera gameCamera;

    public bool IsPlaying { get; private set; }

    void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    public void EnterCutscene(string sceneName) //게임 씬에서 이름으로 컷씬 호출
    {
        if (IsPlaying) return;
        IsPlaying = true;
        SetPaused(true);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    public void ExitCutscene(string sceneName) //컷씬 종료 후 게임 씬으로 복귀
    {
        if (!IsPlaying) return;
        StartCoroutine(ExitRoutine(sceneName));
    }

    private IEnumerator ExitRoutine(string sceneName)
    {
        yield return SceneManager.UnloadSceneAsync(sceneName);
        SetPaused(false);
        IsPlaying = false;
    }

    private void SetPaused(bool pause)
    {
        foreach (var m in toPause)
            if (m != null) m.enabled = !pause;

        if (gameCamera != null) gameCamera.enabled = !pause;
    }
}