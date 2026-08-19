using UnityEngine;

/// <summary>
/// 컷씬 맵 종료용 문입니다. 플레이어가 들어오면 조각 먹던 위치로 되돌리고,
/// AbilityManager가 스테이지 시작 연출을 이어서 처리합니다.
/// </summary>
public class ScenenDoor : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (AbilityManager.instance == null) return;

        AudioManager.Play(SFXKey.EnterDoor);
        AbilityManager.instance.ReturnFromCutscene();
    }
}
