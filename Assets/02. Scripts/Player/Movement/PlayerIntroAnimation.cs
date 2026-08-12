using System.Collections;
using UnityEngine;

/// <summary>
/// 시작 연출(Stand Up)이 끝날 때까지 이동·조준을 잠급니다.
/// Animator 기본 상태가 인트로 상태여야 하며, 그 상태에서 나가는 전환이 있어야 합니다.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerController))]
public class PlayerIntroAnimation : MonoBehaviour
{
    [Tooltip("시작 연출로 사용할 Animator 상태 이름")]
    [SerializeField] string introStateName = "Stand Up";
    [SerializeField] int animatorLayer = 0;
    [Tooltip("전환이 없거나 상태 이름이 틀렸을 때 조작이 영원히 잠기지 않도록 하는 최대 대기 시간")]
    [SerializeField] float maxWaitSeconds = 15f;

    Animator anim;
    PlayerController pc;
    bool finished;

    void Awake()
    {
        anim = GetComponent<Animator>();
        pc = GetComponent<PlayerController>();
        pc.SetControlEnabled(false);
    }

    void Start()
    {
        StartCoroutine(WaitForIntro());
    }

    IEnumerator WaitForIntro()
    {
        // Animator가 기본 상태를 갱신할 때까지 한 프레임 대기
        yield return null;

        float deadline = Time.time + maxWaitSeconds;
        bool entered = false;

        while (Time.time < deadline)
        {
            var info = anim.GetCurrentAnimatorStateInfo(animatorLayer);

            if (info.IsName(introStateName))
            {
                entered = true;
                if (1f <= info.normalizedTime && !anim.IsInTransition(animatorLayer)) break;
            }
            else if (entered)
            {
                // 이미 다음 상태로 전환됨
                break;
            }

            yield return null;
        }

        if (!entered)
            Debug.LogWarning($"{name}: Animator에서 '{introStateName}' 상태를 찾지 못했습니다. 상태 이름을 확인하세요.", this);

        FinishIntro();
    }

    /// <summary>인트로 클립 마지막 프레임 Animation Event로도 호출할 수 있습니다.</summary>
    public void FinishIntro()
    {
        if (finished) return;

        finished = true;
        pc.SetControlEnabled(true);

        // Stand Up이 끝난 직후 스테이지 0 시작 연출을 띄웁니다.
        if (StagePanel.instance != null)
            StagePanel.instance.StartStageStartAnimation(0);
    }
}
