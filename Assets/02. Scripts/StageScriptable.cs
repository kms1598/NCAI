using UnityEngine;

/// <summary>
/// 스테이지 정보를 담는 ScriptableObject입니다.
/// Project 창에서 우클릭 → Create → NCAI → Stage Data로 만드세요.
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "NCAI/Stage Data")]
public class StageScriptable : ScriptableObject
{
    [Tooltip("스테이지 번호입니다. UI 표시나 정렬에 씁니다.")]
    [SerializeField] int stageNumber;
    [Tooltip("스테이지 이름입니다.")]
    [SerializeField] string stageName;
    [Tooltip("이 스테이지로 들어갈 때 페이드로 바꿀 BGM입니다. 비우면 BGM을 바꾸지 않습니다.")]
    [SerializeField] AudioClip bgm;

    public int StageNumber => stageNumber;
    public string StageName => stageName;
    public AudioClip Bgm => bgm;
}
