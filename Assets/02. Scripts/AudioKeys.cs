/// <summary>
/// 06. Audio/SFX 폴더에 있는 효과음 이름입니다.
/// 클립 이름 끝의 _01, _02는 AudioManager가 변형으로 묶어 주므로 보통 번호를 뺀 이름을 씁니다.
/// 특정 변형만 쓰고 싶을 때는 번호까지 적으면 그 클립만 재생됩니다.
/// 문자열을 직접 적지 말고 이 상수를 쓰면 오타로 소리가 안 나는 일을 막을 수 있습니다.
/// </summary>
public static class SFXKey
{
    // Character — 능력 슬롯과 형태 전환
    public const string CharacterSelect = "sfx_character_sel";
    public const string CharacterChange = "sfx_character_change";
    public const string CharacterOn = "sfx_character_on";

    // Player
    // 변형 3개를 번갈아 쓰면 점프할 때마다 소리가 달라져 산만하게 들려서 두 번째 것으로 고정했습니다.
    // 다시 무작위로 돌리려면 번호를 떼고 "sfx_jump"로 바꾸면 됩니다.
    public const string Jump = "sfx_jump_02";
    public const string Walking = "sfx_walking";
    public const string PlayerDie = "sfx_playerdie";
    public const string Piece = "sfx_piece";

    // Projection — 투사 능력
    public const string Bow = "sfx_bow";
    public const string Switch = "sfx_switch";

    // Sublimation — 승화 능력
    public const string SublimationAttack = "sfx_sublimation_attack";
    public const string SublimationBallet = "sfx_sublimation_ballet";

    // Prop
    public const string Box = "sfx_box";

    // Enemy
    public const string Enemy1 = "sfx_enemy_1";
    public const string Enemy2 = "sfx_enemy_2";
    public const string Enemy3 = "sfx_enemy_3";

    // UI — UI 스크립트에서 쓰라고 이름만 정리해 둔 것입니다.
    public const string Click = "sfx_click";
    public const string PageButton = "sfx_pagebutton";
    public const string Success = "sfx_success";
    public const string Loading = "sfx_loading";
    public const string OpenDoor = "sfx_open_door";
    public const string EnterDoor = "sfx_enter_door";
}

/// <summary>06. Audio/BGM 폴더에 있는 배경음 이름입니다.</summary>
public static class BGMKey
{
    public const string Main = "bgm_main";
    public const string Cutscene = "bgm_cut";
    public const string Projection = "bgm_projection";
    public const string Rationalization = "bgm_rationalization";
    public const string Regression = "bgm_regression";
    public const string Sublimation = "bgm_sublimation";
}
