using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ==============================================================
// AI 성향 결정용 데이터 클래스
// ==============================================================

[System.Serializable]
public class EnemyPhase
{
    [Tooltip("이 페이즈로 넘어가는 체력 비율 (예: 0.7 = 70% 이하일 때 발동)")]
    [Range(0f, 1f)] public float hpThreshold;

    [HideInInspector] public bool isTriggered = false;
}

[System.Serializable]
public class SpecialPattern
{
    [Tooltip("여기에 기획자가 만든 조건(ScriptableObject) 파일을 드래그해서 넣으세요!")]
    public AICondition conditionModule;
    public SkillData overrideSkill;

    [HideInInspector]
    public bool isUse = false;
}

public struct ActionCandidate
{
    public SkillData skill;
    public CharacterStatus mainTarget;
    public float score;
}

// ==============================================================
// 메인 EnemyAI 클래스
// ==============================================================
public class EnemyAI : MonoBehaviour
{
    [Header("기본(1페이즈) AI 성향")]
    public List<EnemyPhase> phaseList;

    private CharacterStatus myStatus;
    private CharacterAction myAction;

    private List<SpecialPattern> specialPatterns;

    private AIPersonalityProfile aiProfile;

    // 이전에 사용한 스킬을 기억하기 위한 변수
    private Queue<SkillData> recentSkills = new Queue<SkillData>();
    private int memorySize = 2; // 최근 2턴 기억

    private void Awake()
    {
        myStatus = GetComponent<CharacterStatus>();
        myAction = GetComponent<CharacterAction>();
    }

    public void InitializeAI(CharacterData data)
    {

        phaseList = new List<EnemyPhase>();
        foreach (var phase in data.phaseList)
        {
            phaseList.Add(new EnemyPhase
            {
                hpThreshold = phase.hpThreshold,
                isTriggered = false
            });
        }

        specialPatterns = new List<SpecialPattern>(data.specialPatterns);
        aiProfile = data.aiProfile;
    }

    public void TakeTurn(List<ITurnEntity> allCombatants)
    {
        StartCoroutine(AILogicCoroutine(allCombatants));
    }

    private IEnumerator AILogicCoroutine(List<ITurnEntity> allCombatants)
    {
        Debug.Log($"[{gameObject.name}] 적 턴 시작! 행동 계산 중...");
        yield return new WaitForSeconds(1.0f);

        CheckAndTriggerPhase();

        CharacterStatus emergencyTarget = null;
        SkillData emergencySkill = null;

        bool isEmergencyTriggered = CheckEmergencyPattern(allCombatants, out emergencyTarget, out emergencySkill);

        if (isEmergencyTriggered)
        {
            Debug.LogWarning($"[{gameObject.name}] 특수 조건 발동!! {emergencyTarget.name}에게 강제 패턴 실행!!");
            recentSkills.Enqueue(emergencySkill);
            if (recentSkills.Count > memorySize) recentSkills.Dequeue();
            myAction.ExecuteSkill(emergencyTarget, emergencySkill);
            yield break;
        }

        ActionCandidate bestAction = GetBestAction(allCombatants);

        Debug.Log($"[{gameObject.name}] {(bestAction.mainTarget != null ? bestAction.mainTarget.name : "본인")}에게 [{(bestAction.skill != null ? bestAction.skill.skillName : "방어")}] 발동!! (점수: {bestAction.score})");

        recentSkills.Enqueue(bestAction.skill);
        if (recentSkills.Count > memorySize) recentSkills.Dequeue();
        myAction.ExecuteSkill(bestAction.mainTarget, bestAction.skill);
    }

    private void CheckAndTriggerPhase()
    {
        float myHpRatio = (float)myStatus.CurrentHP / myStatus.characterData.MaxHp;

        foreach (var phase in phaseList)
        {
            if (!phase.isTriggered && myHpRatio <= phase.hpThreshold)
            {
                phase.isTriggered = true;

                Debug.LogWarning($"[{gameObject.name}] 체력 {phase.hpThreshold * 100}% 이하! 새로운 페이즈 돌입!!");

                BossStatus bossStatus = GetComponent<BossStatus>();
                if (bossStatus != null) bossStatus.UnlockHp();
            }
        }
    }

    private bool CheckEmergencyPattern(List<ITurnEntity> allCombatants, out CharacterStatus outTarget, out SkillData outSkill)
    {
        outTarget = null;
        outSkill = null;

        foreach (var pattern in specialPatterns)
        {
            if (pattern.conditionModule == null) continue;
            if (pattern.isUse) continue;

            if (pattern.conditionModule.CheckCondition(this, allCombatants, out outTarget))
            {
                outSkill = pattern.overrideSkill;
                return true;
            }
        }
        return false;
    }

    private ActionCandidate GetBestAction(List<ITurnEntity> allCombatants)
    {
        List<ActionCandidate> candidates = new List<ActionCandidate>();
        List<SkillData> affordableSkills = GetAffordableSkills(); // MP 조건 맞는 스킬 리스트

        // 평타는 MP가 꽉 차있어도 '항상' 행동 후보에 포함시킵니다!
        if (myStatus.characterData.basicAttackData != null)
        {
            affordableSkills.Add(myStatus.characterData.basicAttackData);
        }

        float mpRatio = (float)myStatus.CurrentMP / myStatus.MaxMp;

        float saveUrge = (1f - mpRatio) * aiProfile.weightSelfPreservation;

        // 모든 가용 스킬(평타 포함) 점수 매기기
        foreach (var skill in affordableSkills)
        {
            List<CharacterStatus> validMainTargets = GetValidCandidates(allCombatants, skill);

            if (skill.isAoE && validMainTargets.Count > 0)
            {
                validMainTargets = new List<CharacterStatus> { validMainTargets[0] };
            }

            foreach (var mainTarget in validMainTargets)
            {
                float actionScore = CalculateActionScore(skill, mainTarget, allCombatants);

                // 평타에 대한 점수 뻥튀기 로직
                if (skill == myStatus.characterData.basicAttackData)
                {
                    // 평타에 가산점을 '비례해서' 줍니다.
                    // 마나가 0%면 +50점, 마나가 50%면 +25점, 마나가 100%면 +0점
                    actionScore += (saveUrge * 50f);
                }

                candidates.Add(new ActionCandidate { skill = skill, mainTarget = mainTarget, score = actionScore });
            }
        }

        // 방어 후보 별도 추가
        float defendScore = 10f + (saveUrge * 70f);
        candidates.Add(new ActionCandidate { skill = null, mainTarget = null, score = defendScore });

        // 가장 점수가 높은 행동을 반환
        return candidates.OrderByDescending(c => c.score).FirstOrDefault();
    }

    private float CalculateActionScore(SkillData skill, CharacterStatus mainTarget, List<ITurnEntity> allCombatants)
    {
        float totalScore = 0f;

        // 스킬 안의 모든 효과를 순회하며 "실제 맞을 대상"을 기준으로 점수 합산
        foreach (var groupPayload in skill.GetEffects())
        {
            foreach (var payload in groupPayload.payloads) {
                // 이 효과가 진짜로 누구한테 들어가는지 대상들을 찾음
                List<CharacterStatus> actualTargets = ResolveEffectTargets(payload, mainTarget, allCombatants);

                foreach (var actualTarget in actualTargets)
                {
                    totalScore += EvaluateEffectScore(skill, payload, actualTarget, allCombatants);
                }
            }
        }

        // 연속 사용 페널티 적용 (같은 스킬 연속 사용 시 점수 대폭 삭감)
        if (recentSkills.Contains(skill))
        {
            // 큐에 들어있다면 최근에 쓴 스킬이므로 점수를 대폭 깎음
            totalScore -= skill.lastUsedValue;
        }

        // 약간의 랜덤성(조미료)
        totalScore += Random.Range(0f, 15f);

        return totalScore;
    }

    private List<SkillData> GetAffordableSkills()
    {
        List<SkillData> affordableSkills = new List<SkillData>();

        foreach (var s in myStatus.equippedSkills)
        {
            if(myStatus.CanUseSkill(s)) affordableSkills.Add(s);
        }

        return affordableSkills;
    }

    private List<CharacterStatus> GetValidCandidates(List<ITurnEntity> allCombatants, SkillData skillToUse)
    {
        List<CharacterStatus> validCandidates = new List<CharacterStatus>();
        List<CharacterStatus> tauntingTargets = new List<CharacterStatus>();

        if (skillToUse.validTargetGroup == TargetType.Self)
        {
            validCandidates.Add(myStatus);
            return validCandidates;
        }

        bool hasReviveEffect = false;

        foreach (var groupPayload in skillToUse.GetEffects())
        {
            foreach (var effectPayload in groupPayload.payloads)
            {
                if (effectPayload.effectType == EffectType.Revive) hasReviveEffect = true;
            }
        }

        foreach (var combatant in allCombatants)
        {
            if (combatant == null || combatant.EntityTransform == null) continue;

            CharacterStatus status = combatant.EntityTransform.GetComponent<CharacterStatus>();
            if (status == null) continue;
            if (status.CurrentHP <= 0 && !hasReviveEffect) continue;
            if (status.CurrentHP > 0 && hasReviveEffect) continue;

            bool isValid = false;
            switch (skillToUse.validTargetGroup)
            {
                case TargetType.Enemy:
                    isValid = status.IsPlayer;
                    break;
                case TargetType.Ally:
                    isValid = !status.IsPlayer;
                    break;
                case TargetType.AllyExceptSelf:
                    isValid = !status.IsPlayer && status != myStatus;
                    break;
                case TargetType.All:
                    isValid = true;
                    break;
            }

            if (isValid)
            {
                validCandidates.Add(status);

                if (skillToUse.validTargetGroup == TargetType.Enemy && status.HasEffect(StatusEffectType.Taunt))
                {
                    tauntingTargets.Add(status);
                }
            }
        }

        if (tauntingTargets.Count > 0 && skillToUse.validTargetGroup == TargetType.Enemy && !skillToUse.isAoE)
        {
            return tauntingTargets;
        }

        return validCandidates;
    }

    private List<CharacterStatus> ResolveEffectTargets(EffectPayload payload, CharacterStatus mainTarget, List<ITurnEntity> allCombatants)
    {
        List<CharacterStatus> targets = new List<CharacterStatus>();

        switch (payload.effectTarget)
        {
            case EffectTargetCategory.MainTarget:
                targets.Add(mainTarget);
                break;

            case EffectTargetCategory.Self:
                targets.Add(myStatus);
                break;

            case EffectTargetCategory.LowestHpAlly:
                CharacterStatus lowestHpAlly = GetLowestHpAlly(allCombatants);
                if (lowestHpAlly != null) targets.Add(lowestHpAlly);
                break;

            case EffectTargetCategory.AllAllies:
                foreach (var c in allCombatants)
                {
                    CharacterStatus status = c.EntityTransform.GetComponent<CharacterStatus>();
                    if (status != null && !status.IsPlayer && status.CurrentHP > 0) targets.Add(status);
                }
                break;

            case EffectTargetCategory.AllEnemies:
                foreach (var c in allCombatants)
                {
                    CharacterStatus status = c.EntityTransform.GetComponent<CharacterStatus>();
                    if (status != null && status.IsPlayer && status.CurrentHP > 0) targets.Add(status);
                }
                break;

            case EffectTargetCategory.AllAlliesExceptSelf:
                foreach (var c in allCombatants)
                {
                    CharacterStatus status = c.EntityTransform.GetComponent<CharacterStatus>();
                    if (status != null && status.IsPlayer && status.CurrentHP > 0 && status != myStatus) targets.Add(status);
                }
                break;
        }

        return targets;
    }

    // 서포터 로직용 헬퍼 함수
    private CharacterStatus GetLowestHpAlly(List<ITurnEntity> allCombatants)
    {
        CharacterStatus bestAlly = null;
        float lowestHpRatio = float.MaxValue;

        foreach (var c in allCombatants)
        {
            CharacterStatus status = c.EntityTransform.GetComponent<CharacterStatus>();
            if (status != null && !status.IsPlayer && status.CurrentHP > 0)
            {
                float ratio = (float)status.CurrentHP / status.MaxHp;
                if (ratio < lowestHpRatio)
                {
                    lowestHpRatio = ratio;
                    bestAlly = status;
                }
            }
        }
        return bestAlly;
    }

    private float EvaluateEffectScore(SkillData skill, EffectPayload payload, CharacterStatus target, List<ITurnEntity> allCombatants)
    {
        // 살아있는 플레이어 수 미리 계산 (기절 등 특정 상태이상 판단용)
        int alivePlayerCount = 0;
        foreach (var c in allCombatants)
        {
            CharacterStatus s = c.EntityTransform.GetComponent<CharacterStatus>();
            if (s != null && s.IsPlayer && s.CurrentHP > 0) alivePlayerCount++;
        }

        switch (payload.effectType)
        {
            case EffectType.Damage:
                return EvaluateDamageScore(skill, payload, target, allCombatants, alivePlayerCount);
            case EffectType.Heal:
                return EvaluateHealScore(skill, payload, target);
            case EffectType.RestoreMP:
                return EvaluateRestoreMPScore(skill, payload, target);
            case EffectType.Buff:
                return EvaluateBuffScore(skill, payload, target);
            case EffectType.Debuff:
                return EvaluateDebuffScore(skill, payload, target, alivePlayerCount);
            case EffectType.Revive:
                return EvaluateReviveScore(payload, target);
            case EffectType.ConsumeStatusAndRestoreMP:
                return EvaluateConsumeStatusAndRestoreMP(skill, payload, target);
            case EffectType.ConsumeStatusAndDamage:
                return EvaluateConsumeStatusAndDamage(skill, payload, target, allCombatants, alivePlayerCount);
        }
        return payload.aiWeight;
    }

    // ==============================================================
    // 개별 가치 평가 함수들 (Utility Evaluators) - TODO: 추가된 AIProfile에 맞게 수정 및 추가
    // ==============================================================
    private float EvaluateDamageScore(SkillData skill, EffectPayload payload, CharacterStatus target, List<ITurnEntity> allCombatants, int alivePlayerCount)
    {
        float score = payload.aiWeight;

        // 파티 내 최고 체력, 최고 공격력, 최고 방어력 탐색용 변수
        int highestMaxHpInParty = 1;
        float highestAtkInParty = 1f;
        float highestDefInParty = 1f;

        foreach (var c in allCombatants)
        {
            CharacterStatus cStatus = c.EntityTransform.GetComponent<CharacterStatus>();
            if (cStatus != null && cStatus.IsPlayer == target.IsPlayer)
            {
                if (cStatus.MaxHp > highestMaxHpInParty) highestMaxHpInParty = cStatus.MaxHp;
                if (cStatus.characterData.Attack > highestAtkInParty) highestAtkInParty = cStatus.characterData.Attack;
                if (cStatus.characterData.Defense > highestDefInParty) highestDefInParty = cStatus.characterData.Defense;
            }
        }

        float hpRatio = (float)target.CurrentHP / target.MaxHp;
        float maxHpRatio = (float)target.MaxHp / highestMaxHpInParty;
        float atkRatio = (float)target.characterData.Attack / highestAtkInParty;
        float defRatio = 1f - ((float)target.characterData.Defense / highestDefInParty); // 방어력이 낮을수록 1에 가까움

        // [1. 단일 공격 디테일 판정]
        if (!skill.isAoE)
        {
            // 킬캐치 (피가 25% 이하일 때 확실한 마무리)
            if (hpRatio <= 0.25f) score += 60f;

            // 프로필 가중치 반영: 막타충, 물몸저격, 위협(딜러)제거, 방어력약점 찌르기
            score += (1f - hpRatio) * aiProfile.weightLowHPRatio * 40f;
            score += (1f - maxHpRatio) * aiProfile.weightLowMaxHP * 40f;
            score += atkRatio * aiProfile.weightHighThreat * 40f;
            score += defRatio * aiProfile.weightLowDef * 40f;
        }
        // [2. 광역 공격(AoE) 판정]
        else
        {
            // 광역기 선호도(weightAoEPreference)에 따라 페널티가 유동적으로 변함
            if (alivePlayerCount >= 3)
            {
                // 3명 이상: 선호도에 비례해 점수 뻥튀기 (기본 1.0배 -> 선호도 높으면 1.3배, 1.6배...)
                score *= (1.0f + (aiProfile.weightAoEPreference * 0.3f));
            }
            else if (alivePlayerCount == 2)
            {
                // 2명: 기본 페널티(0.8배)지만, 선호도가 높으면 페널티가 줄어듦!
                score *= (0.8f + (aiProfile.weightAoEPreference * 0.1f));
            }
            else
            {
                // 1명: 심한 페널티(0.4배)지만, 선호도가 엄청 높으면 약간 상쇄됨
                score *= (0.4f + (aiProfile.weightAoEPreference * 0.1f));
            }
        }

        // [3. 공통: 어그로 및 방어 판정]
        score += target.AggroLevel * aiProfile.weightAggro * 30f; // 탱커 때리기
        if (target.isDefending) score -= aiProfile.weightDefensePenalty * 50f; // 방어 중인 놈 피하기

        // ==============================================================
        // [4. 속성 연계 (Synergy & Gimmick) 판정 추가!]
        // ==============================================================
        if (payload.conditionTargetStatus != StatusEffectType.None)
        {
            // 타겟이 마침 내가 원하는 상태이상(예: 젖음)에 걸려있다면?
            if (target.HasEffect(payload.conditionTargetStatus))
            {
                // 1. 데미지 뻥튀기 기믹이 있는 경우 점수 대폭 상승
                if (payload.conditionMultiplier > 1.0f)
                {
                    score *= payload.conditionMultiplier; // 데미지 배율만큼 점수도 배로 뜀!
                    score += 40f; // 보너스 가산점
                }

                // 2. 추가 상태이상(예: 감전, 빙결)을 거는 기믹이 있는 경우
                if (payload.extraStatusOnCondition != StatusEffectType.None)
                {
                    score += 50f; // 엄청난 전술적 이득이므로 높은 가산점
                }

                // AI의 성향(콤보 선호도) 곱해주기 (만약 1번에서 Profile에 변수를 추가했다면)
                score *= aiProfile.weightSynergy; 

                Debug.Log($"[AI 기믹 탐지] {target.name}에게 {payload.conditionTargetStatus} 발견! 콤보 발동을 위해 점수 급상승: {score}");
            }
        }

        return score;
    }

    private float EvaluateHealScore(SkillData skill, EffectPayload payload, CharacterStatus target)
    {
        float hpRatio = (float)target.CurrentHP / target.MaxHp;

        // 피가 70% 이상이면 힐을 아예 안 함 (마나 낭비 방지)
        if (hpRatio > 0.7f) return -100f;

        float score = payload.aiWeight + 30f;

        // 피가 30% 미만인 초위급 상황일 때 기본 가치 3배 상승
        if (hpRatio < 0.3f) score *= 3f;

        // 프로필 가중치 반영
        if (skill.validTargetGroup != TargetType.Self)
        {
            // 다른 아군을 치유할 때 (이타심/힐러 성향)
            score *= aiProfile.weightHeal;
        }
        else
        {
            // 나 자신을 치유할 때 (생존 본능/쫄보 성향)
            score *= aiProfile.weightSelfPreservation;
        }

        return score;
    }

    private float EvaluateRestoreMPScore(SkillData skill, EffectPayload payload, CharacterStatus target)
    {
        float mpRatio = (float)target.CurrentMP / target.MaxMp;

        if (mpRatio > 0.6f) return -100f; // 마나 넉넉하면 안 씀

        float score = payload.aiWeight + 30f;
        if (mpRatio < 0.2f) score *= 2f;

        // 프로필 가중치 반영
        if (skill.validTargetGroup != TargetType.Self)
            score *= aiProfile.weightRestoreMP; // 동료 마나 채워주기 성향
        else
            score *= aiProfile.weightSelfPreservation; // 내 마나 챙기기

        return score;
    }

    private float EvaluateBuffScore(SkillData skill, EffectPayload payload, CharacterStatus target)
    {
        // 이미 같은 버프가 걸려있으면 이 타겟에 대한 점수는 마이너스
        if (target.HasEffect(payload.statusEffectType)) return -100f;

        float score = payload.aiWeight + 30f;
        float targetHpRatio = (float)target.CurrentHP / target.MaxHp;

        switch (payload.statusEffectType)
        {
            // [절대 방어] 쉴드 (1회 뎀감/무효)
            case StatusEffectType.Shield:
                score *= 1.3f;
                // 피가 30% 이하인 위급한 아군을 살리기 위해 사용할 때 가치 대폭 상승! (유저 빡침 유발)
                if (targetHpRatio < 0.3f) score *= 2.0f;
                break;

            // [어그로 및 탱킹] 도발, 피해 대신 받기
            case StatusEffectType.Taunt:
            case StatusEffectType.DamageShare:
                // 피가 40% 미만일 때 이걸 걸면 자살행위이므로 절대 안 씀
                if (targetHpRatio < 0.4f) return -100f;

                // 피가 70% 이상으로 넉넉한 튼튼한 아군에게 걸어서 진형을 붕괴시킴
                if (targetHpRatio >= 0.7f) score *= 1.5f;
                break;

            // [공격형 버프] 공격력 증가, 추가 타격 확률 증가
            case StatusEffectType.AtkUp:
            case StatusEffectType.ExtraHitChance:
                score *= 1.3f;
                if (targetHpRatio < 0.2f) score *= 0.3f; // 곧 죽을 애한테는 안 줌
                break;

            // [방어형 버프] 방어력 증가, 효과/속성 저항력 증가
            case StatusEffectType.DefUp:
            case StatusEffectType.ResUp:
                if (targetHpRatio < 0.7f && targetHpRatio >= 0.3f) score *= 1.4f;
                break;

            // [생존/유틸 버프] 회피 버프, 속도 증가
            case StatusEffectType.Evasion:
                score *= 1.4f;
                if (targetHpRatio < 0.5f) score *= 1.6f;
                break;
            case StatusEffectType.SpeedUp:
                score *= 1.2f;
                break;
        }

        // AI 프로필 가중치 반영
        if (skill.validTargetGroup != TargetType.Self)
            score *= aiProfile.weightBuff; // 아군 서포팅 성향
        else
            score *= aiProfile.weightSelfPreservation; // 이기적 방어 성향

        return score;
    }

    private float EvaluateDebuffScore(SkillData skill, EffectPayload payload, CharacterStatus target, int alivePlayerCount)
    {
        // 이미 같은 디버프가 걸려있으면 안 씀
        if (target.HasEffect(payload.statusEffectType)) return -100f;

        float score = payload.aiWeight + 30f;
        float hpRatio = (float)target.CurrentHP / target.MaxHp;

        if (skill.isAoE && alivePlayerCount == 1)
        {
            score *= 0.5f; // 적 1명 남았을 때 광역 디버프는 낭비
        }

        switch (payload.statusEffectType)
        {
            // [가장 악랄한 디버프] 저주 (힐 반전)
            case StatusEffectType.Curse:
                // 피가 적은 타겟에게 걸면, 유저 힐러가 힐을 못하게 봉쇄하는 미친 성능을 발휘!
                if (hpRatio < 0.4f) score *= 2.5f;
                else score *= 1.2f;
                break;

            // [군중 제어] 감전 (스턴)
            case StatusEffectType.Electrocute:
                if (alivePlayerCount == 1) score *= 2.0f;
                else score *= 1.5f;
                break;

            // [명중 방해] 실명
            case StatusEffectType.Blind:
                score *= 1.3f;
                break;

            // [행동 억제] 가시 (행동 시 데미지 반사/출혈)
            case StatusEffectType.Thorn:
                // 피가 많은 적을 말라 죽이거나 턴을 낭비하게 만듦
                if (hpRatio > 0.5f) score *= 1.3f;
                break;

            // [도트 데미지] 화상 (최대 체력 비례 뎀)
            case StatusEffectType.Burn:
                if (hpRatio < 0.2f) return -50f; // 어차피 죽을 애한테는 안 걸음
                score += hpRatio * 30f; // 피가 많은 놈(탱커)일수록 점수 대폭 상승!
                break;

            // [원소 시너지용] 젖음
            case StatusEffectType.Wet:
                // 자체 데미지는 적지만, 아래의 '시너지 기믹(conditionTargetStatus)' 체크 로직에서
                // 이 AI가 전기 스킬을 가지고 있다면 점수가 폭발적으로 오름!
                score *= 1.1f;
                break;

            // [장갑 파괴] 부식 (방어력 감소) 및 일반 스탯 감소
            case StatusEffectType.Corrosion:
            case StatusEffectType.DefDown:
            case StatusEffectType.ResDown:
                if (hpRatio < 0.2f) return -50f;
                if (hpRatio > 0.6f) score *= 1.5f; // 방어력/체력이 쌩쌩한 놈을 뚫기 위해 사용
                break;

            case StatusEffectType.AtkDown:
            case StatusEffectType.SpeedDown:
                score *= 1.2f;
                break;
        }

        // [핵심] 시너지 기믹 우선순위 처리 (예: 젖음 상태일 때 감전 스킬을 쓴다면?)
        if (payload.conditionTargetStatus != StatusEffectType.None && target.HasEffect(payload.conditionTargetStatus))
        {
            score += 50f; // 기믹 발동 조건이 맞춰졌다면 우선순위 최상위로 끌어올림!
            if (payload.extraStatusOnCondition != StatusEffectType.None)
            {
                score += 40f;
            }
        }

        // AI 프로필 가중치 반영 (상대를 괴롭히려는 성향)
        score *= aiProfile.weightDebuff;

        return score;
    }

    private float EvaluateReviveScore(EffectPayload payload, CharacterStatus target)
    {
        if (target.CurrentHP > 0) return -1000f; // 살아있으면 부활 불가

        // 프로필 가중치 반영 (네크로맨서처럼 부활에 집착하는 정도)
        // weightRevive가 1.0이면 +150점 (최우선 발동), 0.0이면 부활 스킬이 있어도 잘 안 씀
        return payload.aiWeight + (aiProfile.weightRevive * 150f);
    }

    private float EvaluateConsumeStatusAndRestoreMP(SkillData skill, EffectPayload payload, CharacterStatus target)
    {
        // 대상에게 우리가 빨아먹을 상태이상(ex. 화상)이 없다면 이 효과는 무용지물 (0점)
        if (!target.HasEffect(payload.statusEffectType)) return 0f;

        float score = payload.aiWeight + 30f; // 조건이 만족되었으니 기본 점수 가산

        // 내(시전자) 마나 상태 확인
        // 이전 로직에서 이 이펙트는 'attackerStatus'의 MP를 채워주었으므로, myStatus를 확인합니다.
        float myMpRatio = (float)myStatus.CurrentMP / myStatus.MaxMp;

        if (myMpRatio > 0.8f)
        {
            // 마나가 거의 꽉 찼다면 굳이 타겟의 상태이상을 소모해가며 마나를 채울 필요가 없음
            return 0f;
        }
        else if (myMpRatio < 0.3f)
        {
            // 마나가 바닥이라면 이 스킬의 가치는 미친듯이 폭등함
            score += 60f;
        }

        // AI 성향(프로필) 가중치 반영
        // 기믹(Synergy)을 활용하는 것이므로 시너지 성향을 곱해주고,
        // 자기 자신의 마나를 채우는 행위이므로 생존 본능(SelfPreservation) 성향도 곱해줍니다.
        score *= aiProfile.weightSynergy;
        score *= aiProfile.weightSelfPreservation;

        Debug.Log($"[AI 기믹 평가] {target.name}의 {payload.statusEffectType} 흡수 및 MP 회복 점수: {score}");

        return score;
    }

    private float EvaluateConsumeStatusAndDamage(SkillData skill, EffectPayload payload, CharacterStatus target, List<ITurnEntity> allCombatants, int alivePlayerCount)
    {
        // 대상에게 터뜨릴 상태이상(ex. 감전, 빙결)이 없다면 무용지물 (0점)
        if (!target.HasEffect(payload.statusEffectType)) return 0f;

        // 조건 만족! 기믹 폭발이므로 기본 가치가 매우 높음
        float score = payload.aiWeight + 50f;

        // 기존 데미지 스킬처럼 타겟의 체력 상황을 봅니다.
        float hpRatio = (float)target.CurrentHP / target.MaxHp;

        // 피가 30% 이하인 적에게 이 기믹을 터뜨려 확실한 킬캐치를 할 수 있다면 점수 가산
        if (hpRatio <= 0.3f)
        {
            score += 50f;
        }

        // AI 성향(프로필) 가중치 대폭 반영
        // 단순히 때리는 게 아니라 '상태이상을 소모해서 터뜨리는 고급 콤보'이므로
        // 콤보를 좋아하는 똑똑한 보스(weightSynergy가 높은 AI)일수록 이 스킬을 미친듯이 선호하게 만듭니다.
        score *= (aiProfile.weightSynergy * 1.5f);

        // 막타충 성향 반영
        score += (1f - hpRatio) * aiProfile.weightLowHPRatio * 30f;

        Debug.Log($"[AI 기믹 평가] {target.name}의 {payload.statusEffectType} 폭발 데미지 점수: {score}");

        return score;
    }
}