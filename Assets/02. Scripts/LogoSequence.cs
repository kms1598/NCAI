using System.Collections;
using UnityEngine;

/// <summary>
/// 로고를 정해진 시간만큼 보여 준 뒤 다음 씬으로 넘깁니다.
/// LogoScene 안의 아무 오브젝트에나 붙여 두면 됩니다.
/// 넘어가는 연출은 SceneLoader가 UILoadingPanel로 처리하므로 여기서는 시간만 셉니다.
/// </summary>
public class LogoSequence : MonoBehaviour
{
    [Tooltip("로고를 보여 줄 시간(초)입니다. 이 시간이 지나면 다음 씬으로 넘어갑니다.")]
    [SerializeField] float displayDuration = 3f;
    [Tooltip("로고가 끝나고 갈 씬입니다.")]
    [SerializeField] GameScene nextScene = GameScene.Title;

    void Start()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // 실제 시간으로 셉니다. timeScale이 0으로 남은 채 로고가 시작되면 영원히 넘어가지 못합니다.
        yield return new WaitForSecondsRealtime(Mathf.Max(displayDuration, 0f));

        SceneLoader.Load(nextScene);
    }
}
