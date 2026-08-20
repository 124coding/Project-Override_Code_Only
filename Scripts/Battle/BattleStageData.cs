using UnityEngine;

[CreateAssetMenu(fileName = "NewStageData", menuName = "Battle/Stage Data")]
public class BattleStageData : ScriptableObject
{
    public string stageName;               // 스테이지 이름

    [Header("Environment")]
    public GameObject backgroundPrefab;    // 이 스테이지에 쓰일 2.5D 배경 전체가 담긴 프리팹
    public AudioClip battleBGM;            // 스테이지 전용 전투 BGM

    [Header("Lighting (Optional)")]
    public Color ambientLightColor = Color.white; // 조명 색상 (동굴이면 어둡게 등)
}