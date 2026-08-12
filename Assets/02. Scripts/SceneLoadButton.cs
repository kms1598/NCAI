using UnityEngine;

/// <summary>
/// UI Button의 On Click()에 연결해 씬을 바꾸는 헬퍼입니다.
/// 버튼 오브젝트(또는 부모)에 붙인 뒤, On Click에서 SceneLoadButton.Load를 지정하세요.
/// </summary>
public class SceneLoadButton : MonoBehaviour
{
    [Tooltip("이 버튼을 눌렀을 때 갈 씬입니다.")]
    [SerializeField] GameScene targetScene = GameScene.Prototype;

    /// <summary>Button On Click()에 연결하는 진입점입니다.</summary>
    public void Load()
    {
        // 이미 전환 중이면 SceneLoader가 경고만 남기고 무시합니다.
        SceneLoader.Load(targetScene);
    }

    /// <summary>게임 종료 버튼용입니다. On Click에서 SceneLoadButton.Quit을 지정하세요.</summary>
    public void Quit()
    {
        SceneLoader.Quit();
    }
}
