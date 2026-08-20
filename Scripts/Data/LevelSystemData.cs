using UnityEngine;

[CreateAssetMenu(fileName = "LevelSystemData", menuName = "Game/Level System Data")]
public class LevelSystemData : ScriptableObject
{
    [Header("Global Level Settings")]
    public int maxLevel = 30;

    public int firstIndexLevel = 15;

    [Tooltip("각 레벨로 넘어가기 위해 필요한 경험치 (Index 1 = 1->2렙 필요 경험치)")]
    public int[] requiredExpTable;

    [Tooltip("스파이크 레벨 단위")]
    public int spikeLevel = 5;

    [Header("Skill Point Settings")]
    public int skillPointsPerLevel = 3;

    [Header("EXP Growth Settings")]
    [Tooltip("일반 레벨업 시 필요 경험치 증가율 (1.2 = 20% 증가)")]
    public float normalGrowthFactor = 1.2f;

    [Tooltip("마일스톤 구간 대폭 증가율 (1.8 = 80% 폭증)")]
    public float milestoneGrowthFactor = 1.8f;

    public int GetRequiredExp(int level)
    {
        if (level < 1 || requiredExpTable == null || requiredExpTable.Length == 0) return 999999;
        if (level >= maxLevel || level >= requiredExpTable.Length) return 999999;

        return requiredExpTable[level];
    }

    [ContextMenu("AutoFill")]
    private void AutoFillExperienceTable()
    {
        requiredExpTable = new int[maxLevel + 1];

        requiredExpTable[0] = 0;
        requiredExpTable[1] = firstIndexLevel; // 1->2렙 필요 경험치

        for (int i = 2; i <= maxLevel; i++)
        {
            bool isMilestoneLevel = ((i - 1) % spikeLevel == 0);

            float factor = isMilestoneLevel ? milestoneGrowthFactor : normalGrowthFactor;

            float nextExp = requiredExpTable[i - 1] * factor;

            requiredExpTable[i] = Mathf.RoundToInt(nextExp);
        }

        Debug.Log($"[LevelSystemData] 5레벨 스파이크가 적용된 경험치 테이블이 생성되었습니다!");
    }
}