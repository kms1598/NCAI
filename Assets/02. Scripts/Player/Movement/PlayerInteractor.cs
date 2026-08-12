using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public GameObject interactUI;
    IInteractable current;

    public void OnInteract(InputValue v)
    {
        if (!v.isPressed) return;

        // 대상이 이미 사라졌다면 상호작용하지 않고 상태만 정리합니다.
        if (!IsUsable(current))
        {
            Clear();
            return;
        }

        current.Interact();

        // 아이템처럼 스스로 사라지는 대상은 OnTriggerExit이 오지 않습니다.
        // 여기서 비워 주지 않으면 다음 입력에서 같은 대상을 또 먹게 됩니다.
        Clear();
    }

    void Update()
    {
        // 화살에 맞아 꺼지는 스위치처럼 다른 경로로 사라지는 경우도 있어
        // 잡고 있는 대상이 아직 쓸 수 있는지 매 프레임 확인합니다.
        if (current != null && !IsUsable(current)) Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        var it = other.GetComponentInParent<IInteractable>();
        if (it == null) return;

        current = it;
        if (interactUI != null) interactUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        var it = other.GetComponentInParent<IInteractable>();
        if (it != null && it == current) Clear();
    }

    void Clear()
    {
        current = null;
        if (interactUI != null) interactUI.SetActive(false);
    }

    /// <summary>
    /// 아직 상호작용할 수 있는 대상인지 확인합니다.
    /// current는 인터페이스 타입이라 == null 비교만으로는 부족합니다.
    /// UnityEngine.Object의 파괴 여부를 봐 주는 == 연산자는 인터페이스 참조에는 적용되지 않아서,
    /// 파괴된 오브젝트도 null이 아닌 것으로 판정되기 때문입니다.
    /// </summary>
    static bool IsUsable(IInteractable target)
    {
        if (target == null) return false;

        // Component로 받아 비교하면 Unity가 파괴 여부까지 확인해 줍니다.
        if (target is Component component) return component != null && component.gameObject.activeInHierarchy;

        return true;
    }
}
