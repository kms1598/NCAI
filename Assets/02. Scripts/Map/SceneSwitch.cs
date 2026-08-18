using UnityEngine;

/// <summary>
/// Switch처럼 F키/상호작용 UI로 눌러 컷씬에서 복귀시킵니다.
/// Switch의 targets에 넣을 수도 있습니다(ISwitchable).
/// </summary>
public class SceneSwitch : MonoBehaviour, IInteractable, ISwitchable
{
    bool used;

    public void Interact() => Activate();

    public void SetState() => Activate();

    public void Activate()
    {
        if (used) return;
        if (AbilityManager.instance == null) return;

        used = true;
        AudioManager.Play(SFXKey.EnterDoor);
        AbilityManager.instance.ReturnFromCutscene();
    }
}
