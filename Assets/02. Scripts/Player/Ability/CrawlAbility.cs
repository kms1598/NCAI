using UnityEngine;

public class CrawlAbility : IAbility
{
    public ActivationType Activation => ActivationType.Toggle;

    public float GaugeCost => 0f;

    public float DrainPerSecond => 3f;

    bool active;
    public bool IsActive => active;


    public MovementStats GetStats()
    {
        return new MovementStats
        {
            moveSpeed = 3 * 0.8f,
            jumpHeight = 0f,
            dashSpeed = 0f,
            dashDuration = 0f,
            canJump = false,
            canDash = false
        };
    }

    public void OnActiveUpdate(PlayerController pc) { }

    public void OnEquip(PlayerController pc) { }
    public void Fire(PlayerController pc)
    {
        SetActive(pc, !active);
        // 소리는 SetActive가 아니라 여기서 냅니다. 형태 전환으로 해제될 때는
        // AbilityManager가 이미 전환음을 내므로 소리가 겹치면 안 됩니다.
        AudioManager.Play(SFXKey.CharacterChange);
    }

    public void OnUnequip(PlayerController pc)
    {
        if (active) SetActive(pc, false);
    }

    void SetActive(PlayerController pc, bool on)
    {
        if (pc == null) return;

        pc.transform.localScale = on ? Vector3.one * 0.5f : Vector3.one;
        pc.gameObject.layer = LayerMask.NameToLayer(on ? "Crawling" : "Player");
        pc.Anim?.SetCrawling(on);
        if (pc.Footstep != null) pc.Footstep.Muted = on;

        active = on;

        // Anim: Crawl_Idle / Crawl_Move — 기어가기 on·off
        pc.Anim?.SetCrawling(on);

        // 기어갈 때는 두 발로 걷는 소리가 어울리지 않아 발소리를 끕니다.
        if (pc.Footstep != null) pc.Footstep.Muted = on;
    }
}
