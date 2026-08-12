using UnityEngine;

/// <summary>
/// 걷는 동안 발소리를 이어서 냅니다.
/// sfx_walking은 한 걸음이 아니라 여러 걸음이 담긴 2초가 넘는 클립이라,
/// 한 걸음마다 새로 재생하면 소리가 겹치고 멈춘 뒤에도 남은 길이만큼 계속 들립니다.
/// 그래서 루프로 한 번만 틀고, 걷기가 끝나는 순간 바로 멈춥니다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerFootstep : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] float volume = 0.7f;

    /// <summary>기어가기처럼 발소리가 어울리지 않는 상태에서 잠시 끄는 데 씁니다.</summary>
    public bool Muted
    {
        get => muted;
        set
        {
            muted = value;
            // 기어가기로 바뀌는 순간 소리가 남지 않도록 바로 반영합니다.
            if (muted) StopWalkSound();
        }
    }

    PlayerController pc;
    AudioSource loop;
    bool muted;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
    }

    void Update()
    {
        bool walking = !muted && pc.cc.isGrounded && 0.01f < pc.moveInput.sqrMagnitude;

        if (walking) StartWalkSound();
        else StopWalkSound();
    }

    void OnDisable()
    {
        StopWalkSound();
    }

    void StartWalkSound()
    {
        if (loop != null) return;
        if (AudioManager.instance == null) return;

        loop = AudioManager.instance.PlayLoop(SFXKey.Walking, volume);
    }

    void StopWalkSound()
    {
        if (loop == null) return;

        AudioManager.instance?.StopLoop(loop);
        loop = null;
    }
}
