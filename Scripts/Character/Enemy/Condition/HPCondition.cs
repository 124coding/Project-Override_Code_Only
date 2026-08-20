using System.Collections.Generic;
using UnityEngine;

// 타겟을 누구로 잡을 것인지 선택
public enum ConditionTargetType
{
    Self,       // 나 자신의 체력 확인
    AnyAlly,    // 아군 중 한 명의 체력 확인
    AnyEnemy    // 적군 중 한 명의 체력 확인
}

// 어떻게 비교할 것인지 선택
public enum CompareOperator
{
    LessThanOrEqual,    // 이하 (<=)
    GreaterThanOrEqual  // 이상 (>=)
}

[CreateAssetMenu(fileName = "New_HPCondition", menuName = "Battle/AI/Conditions/HP Condition")]
public class HPCondition : AICondition
{
    [Header("Condition Settings (조건 설정)")]
    [Tooltip("누구의 체력을 검사할까요?")]
    public ConditionTargetType whoToCheck = ConditionTargetType.Self;

    [Tooltip("비교 방식 (이하 / 이상)")]
    public CompareOperator operatorType = CompareOperator.LessThanOrEqual;

    [Range(0f, 1f)]
    [Tooltip("체력 퍼센트 기준점 (0.5 = 50%)")]
    public float hpPercentageThreshold = 0.5f;

    public override bool CheckCondition(EnemyAI self, List<ITurnEntity> allCombatants, out CharacterStatus target)
    {
        target = null;
        CharacterStatus myStatus = self.GetComponent<CharacterStatus>();
        if (myStatus == null) return false;

        // 나 자신을 검사하는 경우
        if (whoToCheck == ConditionTargetType.Self)
        {
            float hpRatio = (float)myStatus.CurrentHP / myStatus.MaxHp;
            if (CompareLogic(hpRatio, hpPercentageThreshold))
            {
                target = myStatus; // 타겟을 나 자신으로 설정
                return true;
            }
        }
        // 아군이나 적군을 검사하는 경우
        else
        {
            foreach (var combatant in allCombatants)
            {
                CharacterStatus status = combatant.EntityTransform.GetComponent<CharacterStatus>();
                if (status != null && status.CurrentHP > 0)
                {
                    // 아군을 찾는 거라면 나와 팀이 같아야 하고, 적군을 찾는 거라면 달라야 함
                    bool isAlly = (status.IsPlayer == myStatus.IsPlayer);
                    if ((whoToCheck == ConditionTargetType.AnyAlly && isAlly) ||
                        (whoToCheck == ConditionTargetType.AnyEnemy && !isAlly))
                    {
                        float hpRatio = (float)status.CurrentHP / status.MaxHp;
                        if (CompareLogic(hpRatio, hpPercentageThreshold))
                        {
                            target = status; // 조건에 맞는 첫 번째 대상을 타겟으로 지정
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    // 비교 연산을 처리해주는 헬퍼 함수
    private bool CompareLogic(float currentRatio, float threshold)
    {
        if (operatorType == CompareOperator.LessThanOrEqual) return currentRatio <= threshold;
        if (operatorType == CompareOperator.GreaterThanOrEqual) return currentRatio >= threshold;
        return false;
    }
}