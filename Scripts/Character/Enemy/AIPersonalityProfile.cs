using UnityEngine;

[CreateAssetMenu(fileName = "NewAIProfile", menuName = "AI/AI Personality Profile")]
public class AIPersonalityProfile : ScriptableObject
{
    [Header("AI Basic Tendency")]
    [TextArea]
    public string description = "이 AI의 성격과 행동 컨셉을 적어주세요.";

    [Header(" Offensive Targeting (공격 대상 선정)")]
    [Tooltip("어그로(도발) 수치에 얼마나 민감하게 반응하는가? (정직하게 탱커를 치는 성향)")]
    public float weightAggro = 1.0f;

    [Tooltip("현재 체력 비율(%)이 낮은 적을 얼마나 선호하는가? (약한 놈부터 확실하게 죽이는 킬캐치 성향)")]
    public float weightLowHPRatio = 1.0f;

    [Tooltip("최대 체력(MaxHP) 자체가 낮은 적을 얼마나 선호하는가? (전열을 무시하고 후열 마법사를 노리는 암살자 성향)")]
    public float weightLowMaxHP = 0.0f;

    [Tooltip("방어력(Def)이 낮은 적을 얼마나 선호하는가? (효율적으로 데미지가 잘 박히는 놈을 찾는 성향)")]
    public float weightLowDef = 0.0f;

    [Tooltip("공격력(Atk)이 높은 적을 얼마나 선호하는가? (위협적인 적 딜러를 우선 제거하려는 똑똑한 성향)")]
    public float weightHighThreat = 0.0f;

    [Tooltip("방어(Guard) 중인 적을 얼마나 회피하는가? (값이 클수록 방어 중인 적을 안 때림)")]
    public float weightDefensePenalty = 2.0f;

    [Header("Self Preservation (자기 생존 본능)")]
    [Tooltip("자신의 체력이 낮을 때, 공격보다 스스로를 치유하거나 방어 버프를 거는 것을 얼마나 우선하는가? (쫄보 성향)")]
    public float weightSelfPreservation = 1.0f;

    [Header("Support & Utility (보조/유틸리티)")]
    [Tooltip("체력이 깎인 아군을 치료(Heal)하려는 성향 (힐러형 몬스터)")]
    public float weightHeal = 1.0f;

    [Tooltip("아군에게 버프(Buff)를 걸어주려는 성향 (지휘관, 바드형 몬스터)")]
    public float weightBuff = 1.0f;

    [Tooltip("적에게 디버프(Debuff)를 걸어 괴롭히려는 성향 (저주술사, 마녀형 몬스터)")]
    public float weightDebuff = 1.0f;

    [Tooltip("사망한 아군을 부활(Revive)시키려는 성향 (네크로맨서형 몬스터)")]
    public float weightRevive = 1.0f;

    [Tooltip("아군의 마나(MP)를 채워주는 스킬에 대한 선호도")]
    public float weightRestoreMP = 1.0f;

    [Header("Tactical Preferences (전술 선호도)")]
    [Tooltip("다수의 적을 맞추는 광역기(AoE)를 얼마나 선호하는가? (값이 높으면 단일 킬캐치보다 전체 양념을 좋아함)")]
    public float weightAoEPreference = 1.0f;

    [Tooltip("상태이상 연계(콤보) 기믹을 얼마나 적극적으로 노릴 것인가?")]
    public float weightSynergy = 1.5f;

    [Tooltip("MP가 부족할 때 턴을 넘길(방어할) 확률 (0~100)")]
    public int zeroMpDefensePercent = 30;
}