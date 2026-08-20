using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterStatus : MonoBehaviour, ITurnEntity
{
    [Header("CharacterData")]
    [SerializeField] public CharacterData characterData;

    protected EffectSystem effectSystem;
    protected ElementSystem elementSystem;
    protected ActionVisualizer visualizer;

    [Header("Current State")]
    // 현재 레벨
    [SerializeField] private int currentLevel;

    // 현재 경험치
    [SerializeField] private int currentEXP;

    // 속도, 행동 게이지
    [SerializeField] private int baseSpeed;
    [SerializeField] private float actionGauge;
    // 최대 체력, 마나
    [SerializeField] private int baseHp;
    [SerializeField] private int baseMp;
    // 현재 체력, 마나
    [SerializeField] private int currentHp;
    [SerializeField] private int currentMp;
    // 기본 공격력
    [SerializeField] private int baseAttack;

    // 기본 방어력
    [SerializeField] private int baseDefense;

    // 기본 저항력
    [SerializeField] private int baseEffectResistance;

    // 현재 방어중인지
    public bool isDefending = false;

    [HideInInspector] public int cumulativeAggro = 0;

    [Header("Shock Gauges")]
    public float currentShockGauge = 0f;
    public float maxShockGauge = 100f;

    [Header("Equipped Skills")]
    public List<SkillData> equippedSkills = new List<SkillData>();

    [Header("Skill Points")]
    [SerializeField] private int currentSkillPoints = 0; // 현재 보유 스킬 포인트

    [Header("Battle Only Variables")]
    public int currentBattleTurnCount { get; private set; }
    public bool hasUsedUltimate { get; private set; }

    public IReadOnlyList<EffectData> ActiveEffects => effectSystem.activeEffects;

    public List<ElementData> ElementDatas => elementSystem.elementDatas;
    public bool IsFullyBroken => elementSystem.isFullyBroken;

    public int CurrentLevel => currentLevel;

    public int CurrentEXP => currentEXP;

    public int CurrentSkillPoints => currentSkillPoints;

    public void AddEXP(int exp)
    {
        if (characterData == null) return;

        // 전역 레벨 시스템 가져오기
        LevelSystemData levelSystem = DataManager.Instance.globalLevelSystem;
        if (levelSystem == null) return;

        // 만렙 체크
        if (currentLevel >= levelSystem.maxLevel) return;

        currentEXP += exp;
        Debug.Log($"[{gameObject.name}] 경험치 획득: +{exp} (현재 EXP: {currentEXP})");

        // 전역 시스템의 요구 경험치를 바탕으로 다중 레벨업 처리
        while (currentLevel < levelSystem.maxLevel && currentEXP >= levelSystem.GetRequiredExp(currentLevel))
        {
            LevelUp(levelSystem);
        }
    }

    public int MaxHp
    {
        get
        {
            return baseHp;
        }
    }

    public int MaxMp
    {
        get
        {
            return baseMp;
        }
    }

    public int CurrentHP => currentHp;
    public int CurrentMP => currentMp;

    public bool IsPlayer => characterData != null && characterData.IsPlayer;
    public int Speed => effectSystem.CalculateModifiedStat(baseSpeed, StatusEffectType.SpeedUp, StatusEffectType.SpeedDown, 0);

    public int Attack => effectSystem.CalculateModifiedStat(baseAttack, StatusEffectType.AtkUp, StatusEffectType.AtkDown, 1);

    public int Defense => effectSystem.CalculateModifiedStat(baseDefense, StatusEffectType.DefUp, StatusEffectType.DefDown, 0);

    public int EffectResistance => effectSystem.CalculateModifiedStat(baseEffectResistance, StatusEffectType.ResUp, StatusEffectType.ResDown, 0);

    public int AggroLevel
    {
        get
        {
            int baseAggro = characterData != null ? characterData.AggroLevel : 1;

            return Mathf.Max(0, Mathf.RoundToInt(baseAggro + cumulativeAggro));
        }
    }

    [Header("Status Effect States")]
    public bool IsStunned => effectSystem.IsStunned;

    public float CurrentActionGauge
    {
        get => actionGauge;
        set => actionGauge = value;
    }
    public Transform EntityTransform => this.transform;

    private void Awake()
    {
        effectSystem = GetComponent<EffectSystem>();
        elementSystem = GetComponent<ElementSystem>();
        visualizer = GetComponent<ActionVisualizer>();
    }

    public void ApplySaveData(CharacterSaveData saveData)
    {
        this.currentLevel = saveData.currentLevel;
        this.currentEXP = saveData.currentEXP;
        this.currentSkillPoints = saveData.currentSkillPoints;
        this.equippedSkills = DataManager.Instance.GetSkillSOListFromNames(saveData.equippedSkillNames);
    }

    public virtual void Initialize(CharacterData data, CharacterSaveData saveData = null)
    {
        this.characterData = data;

        // 세이브 데이터가 있으면 레벨/EXP/스킬포인트/스킬 세팅, 없으면 기본값 세팅
        if (saveData != null)
        {
            ApplySaveData(saveData);
        }
        else
        {
            currentLevel = characterData.BaseLevel;
            currentEXP = 0;
            currentSkillPoints = 0; // 필요 시 초기값 조정
            equippedSkills = new List<SkillData>(characterData.defaultSkills);
        }

        // 확정된 currentLevel을 바탕으로 기초 스탯(공격력, 방어력, 최대체력 등) 계산!
        baseHp = characterData.GetHpAtLevel(currentLevel);
        baseAttack = characterData.GetAttackAtLevel(currentLevel);
        baseDefense = characterData.GetDefenseAtLevel(currentLevel);
        baseSpeed = characterData.Speed;
        baseMp = characterData.MaxMp;

        // 현재 체력 설정
        // 세이브 데이터의 currentHp가 새로 계산된 최대 체력(baseHp)을 넘지 않도록 안전장치 적용
        currentHp = (saveData != null) ? Mathf.Min(saveData.currentHp, baseHp) : baseHp;
        currentMp = 5;

        // 전투 전용 상태 초기화
        actionGauge = 0f;
        currentBattleTurnCount = 0;
        hasUsedUltimate = false;

        // 적 전용 랜덤 속성 및 스킬 갱신
        if (!IsPlayer)
        {
            currentLevel = DataManager.Instance.currentEnemyPartyLevel;
            elementSystem = GetComponent<ElementSystem>();
            if (elementSystem != null)
            {
                elementSystem.InitializeSystem(this); // 나 자신(this)을 전달하며 세팅 지시
            }
            UpdateSkills();
        }

        Debug.Log($"[{gameObject.name}] 초기화 성공! Lv.{currentLevel} | HP: {currentHp}/{baseHp} | 공격력: {baseAttack}");
    }

    private void LevelUp(LevelSystemData levelSystem)
    {
        // 경험치 차감 및 레벨 증가
        int requiredExp = levelSystem.GetRequiredExp(currentLevel);
        currentEXP -= requiredExp;
        currentLevel++;

        // 레벨 업 포인트 보상
        int rewardPoints = levelSystem.skillPointsPerLevel;
        currentSkillPoints += rewardPoints;

        // 스탯 성장 (CharacterData에 설정해둔 직업별 성장치만큼 증가)
        baseHp += characterData.hpGrowth;
        baseAttack += characterData.attackGrowth;
        baseDefense += characterData.defenseGrowth;

        // 레벨업 시 체력/마나 풀 회복
        currentHp = baseHp;

        Debug.Log($"[{gameObject.name}] 레벨 업! 현재 레벨: {currentLevel}");
    }

    public bool TryConsumeSkillPoints(int amount)
    {
        if (currentSkillPoints >= amount)
        {
            currentSkillPoints -= amount;
            return true;
        }
        return false;
    }

    public void IncrementTurnCount()
    {
        currentBattleTurnCount++;
        Debug.Log($"[{gameObject.name}] 턴 시작! 현재 누적 턴: {currentBattleTurnCount}");
    }

    public void ConsumeUltimate()
    {
        hasUsedUltimate = true;
        Debug.Log($"[{gameObject.name}] 궁극기 사용 완료! 이번 전투에서는 더 이상 사용할 수 없습니다.");
    }

    public virtual void UpdateSkills()
    {
        // 후보군 풀(Candidate Pool) 생성
        List<SkillData> candidatePool = new List<SkillData>();

        foreach (SkillData skill in characterData.allAvailableSkills)
        {
            // 무속성 스킬이면 후보군에 무조건 포함
            if (skill.skillElement == null)
            {
                candidatePool.Add(skill);
                continue;
            }

            // 현재 적이 껍질로 들고 있는 속성과 일치하면 후보군에 포함
            if ((elementSystem != null && elementSystem.elementDatas.Contains(skill.skillElement)) || !characterData.ElementSkillSet)
            {
                candidatePool.Add(skill);
            }
        }

        int targetEquipCount = Mathf.Min(characterData.maxSkillEquipCount, candidatePool.Count);
        List<SkillData> newEquippedSkills = new List<SkillData>();

        // 무작위 셔플 (Fisher-Yates 알고리즘)
        // 리스트 자체를 랜덤하게 마구 섞어버립니다. (가장 가볍고 중복 없는 정석 방식)
        for (int i = 0; i < candidatePool.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, candidatePool.Count);
            SkillData temp = candidatePool[i];
            candidatePool[i] = candidatePool[randomIndex];
            candidatePool[randomIndex] = temp;
        }

        for (int i = 0; i < targetEquipCount; i++)
        {
            newEquippedSkills.Add(candidatePool[i]);
        }

        // 최종 슬롯 덮어쓰기
        equippedSkills = newEquippedSkills;

        Debug.Log($"[{gameObject.name}] 속성 변경에 따른 스킬 셔플 완료! (후보 {candidatePool.Count}개 중 {equippedSkills.Count}개 장착)");
    }

    public virtual void ApplyHpChange(int finalamount, ElementData hitElement = null, WeaknessSetting rules = null, bool isSharedDamage = false)
    {
        if (characterData == null) return;

        if (finalamount < 0)
        {
            // 방어 중이라면 최종 데미지 반감
            if (isDefending) finalamount = Mathf.RoundToInt(finalamount * 0.5f);

            // 브레이크 시스템 처리
            if (elementSystem != null && hitElement != null && rules != null && !IsFullyBroken)
            {
                elementSystem.ProcessBreak(hitElement, rules);
            }
        }

        currentHp = Mathf.Clamp(currentHp + finalamount, 0, MaxHp);
        BattleEvents.OnHealthChanged?.Invoke(this, finalamount);

        if (currentHp <= 0)
        {
            Debug.Log($"[{gameObject.name}] 사망!");

            effectSystem.ActiveEffectsClear();

            StartCoroutine(HandleDeathSequence());
        }
    }

    protected virtual int InterceptFinalDamage(int finalDamage)
    {
        return finalDamage;
    }

    public void ApplyMpChange(int amount)
    {
        if (characterData == null) return;
        if (amount == 0) return;

        // 계산 진행 (0 ~ MaxMP 제한)
        currentMp = Mathf.Clamp(currentMp + amount, 0, MaxMp);

        BattleEvents.OnMpChanged?.Invoke(this, currentMp);
    }

    // 사망 연출
    private IEnumerator HandleDeathSequence()
    {
        if (visualizer != null)
        {
            Sprite deathSprite = characterData != null ? characterData.DeathSprite : null;

            // ActionVisualizer의 연출 코루틴이 완전히 끝날 때까지 여기서 대기합니다.
            yield return StartCoroutine(visualizer.ExecuteDeathVisuals(deathSprite));
        }
        else Debug.Log("visualizer null");

            Debug.Log($"[{gameObject.name}] 시각 연출 완료. 시스템에 사망 이벤트 발송!");

        BattleEvents.OnCharacterDied?.Invoke(this);
    }

    public void Revive(int healPercent)
    {
        if (currentHp > 0) return;

        // 시각 연출(스프라이트 복구, 애니메이터 켜기)을 Visualizer에게 위임
        if (visualizer != null)
        {
            Sprite basicSprite = characterData != null ? characterData.BasicSprite : null;
            visualizer.ExecuteReviveVisuals(basicSprite);
        }

        // 체력 회복 및 UI 갱신 로직
        currentHp = Mathf.RoundToInt((healPercent / 100f) * MaxHp);
        BattleEvents.OnHealthChanged.Invoke(this, currentHp);

        Debug.Log($"[{gameObject.name}] 부활! (체력: {currentHp})");
        BattleEvents.OnTimelineUpdateRequested?.Invoke();
    }

    // 파티원 중 나를 대신해줄 보호자가 있는지 찾습니다.
    public CharacterStatus GetActiveProtector()
    {
        // 전체 전투원 목록 가져오기
        List<ITurnEntity> allCombatants = BattleEvents.RequestAllCombatants?.Invoke();
        if (allCombatants == null) return null;

        foreach (var c in allCombatants)
        {
            CharacterStatus s = c.EntityTransform.GetComponent<CharacterStatus>();

            // 1. 내가 아니고, 2. 살아있고, 3. 같은 편이며, 4. DamageShare 버프가 있는 캐릭터
            if (s != null && s != this && s.CurrentHP > 0 && s.IsPlayer == this.IsPlayer)
            {
                if (s.effectSystem.HasStatusEffect(StatusEffectType.DamageShare))
                {
                    return s; // 든든한 탱커 발견!
                }
            }
        }
        return null;
    }

    public bool HasEffect(StatusEffectType type)
    {
        return effectSystem != null && effectSystem.HasStatusEffect(type);
    }

    public EffectData GetStatusEffect(StatusEffectType type)
    {
        return effectSystem.GetStatusEffect(type);
    }

    public void AddStatEffect(StatusEffectType type, float amount, int turns, EffectModifierType modifierType)
    {
        effectSystem.AddStatEffect(type, amount, turns, modifierType);
    }

    public void RemoveStatusEffect(StatusEffectType type)
    {
        effectSystem.RemoveStatusEffect(type);
    }

    public void CleanseDebuffs()
    {
        effectSystem.CleanseDebuffs();
    }

    public void CleanseBuffs()
    {
        effectSystem.CleanseBuffs();
    }

    public int ConsumeStatusEffect(StatusEffectType effect)
    {
        return effectSystem.ConsumeStatusEffect(effect);
    }

    public void TickEffects()
    {
        if (effectSystem != null)
        {
            effectSystem.TickEffects();
        }
    }

    public bool ProcessTurnStartEffects()
    {
        // effectSystem이 null일 경우를 대비해 안전하게 호출
        if (effectSystem != null)
        {
            return effectSystem.ProcessTurnStartEffects();
        }

        // 컴포넌트가 없으면 도트 데미지로 죽을 일도 없음(false)
        return false;
    }

    public bool CheckWillBreak(ElementData hitElement, WeaknessSetting rules)
    {
        // effectSystem이 null일 경우를 대비해 안전하게 호출
        if (elementSystem != null)
        {
            return elementSystem.CheckWillBreak(hitElement, rules);
        }

        // 컴포넌트가 없으면 도트 데미지로 죽을 일도 없음(false)
        return false;
    }

    public void AddShockGauge(float amount)
    {
        // 이미 감전 상태라면 수치를 올리지 않음 (선택 사항)
        if (effectSystem.HasStatusEffect(StatusEffectType.Electrocute)) return;

        currentShockGauge += amount;
        Debug.Log($"[{gameObject.name}] 감전 수치 상승: {currentShockGauge} / {maxShockGauge}");

        // 수치가 꽉 찼는지 확인
        if (currentShockGauge >= maxShockGauge)
        {
            currentShockGauge = 0f; // 게이지 초기화

            // 감전 상태이상 부여! (턴수나 데미지는 기획에 맞게 설정)
            AddStatEffect(StatusEffectType.Electrocute, 0f, 1, EffectModifierType.None);
            Debug.Log($"[{gameObject.name}] 감전 게이지 폭발! 감전 상태이상 부여!");

            // TODO: 여기서 감전이 터지는 짜릿한 이펙트를 하나 소환
        }
    }

    public bool CanUseSkill(SkillData skill)
    {
        // MP가 부족하면 사용 불가
        if (CurrentMP < skill.mpCost) return false;

        if (skill.isUltimate)
        {
            // 이미 썼다면 사용 불가
            if (hasUsedUltimate)
                return false;

            // 지정된 턴 수(예: 5턴)가 아직 안 되었다면 사용 불가
            if (currentBattleTurnCount < skill.requiredTurns)
                return false;
        }

        // TODO: 그 외 침묵(Silenced) 등의 상태이상 체크가 있다면 이곳에 추가...

        return true; // 위 조건을 다 통과하면 사용 가능!
    }

    public void TryConsumeShield()
    {
        effectSystem.TryConsumeShield();
    }
}