
using System;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState
{
    Init, // 초기 상태
    CheckNextTurn,
    PlayerTurn, // 플레이어 턴
    EnemyTurn, // 적 턴
    Win, // 승리
    Lose // 패배
}

// 전투 진입 상태
public enum EncounterType
{
    Normal,             // 평범한 조우 (서로 걷다가 부딪힘)
    PlayerAdvantage,    // 플레이어 선제공격 (필드에서 플레이어가 적을 먼저 때림)
    EnemyAdvantage,     // 적의 기습 (플레이어가 뒤를 잡힘)
    Parried             // 패링 성공 (적 공격을 타이밍 맞춰 튕겨냄)
}

// TODO: 스킬 효과들 더 채우기
public enum EffectType
{
    Damage,         // 적에게 데미지
    Heal,           // 체력 회복
    HealPercentMaxHP,    // 대상의 최대 체력 비례 힐 (퍼센트 포션)
    HealFlat,            // 고정 수치 힐
    RestoreMP,      // 마나 회복
    TurnBaton,      // 턴 새치기 (다음 턴 부여)
    Revive,         // 부활
    Buff,           // 이로운 상태 변화 (공격력 증가, 방어막, 도발 등)
    Debuff,         // 해로운 상태 변화 (독, 기절, 방어력 감소 등)
    Cleanse,         // 디버프 해제 (정화)
    ConsumeStatusAndRestoreMP,
    ConsumeStatusAndDamage,
    CleanseBuffs // 버프 삭제
}

public enum StatusEffectType
{
    None,

    // [1. 스탯 증감류] (Flat은 영구, Percent는 턴 기반으로 활용)
    AtkUp, AtkDown,
    DefUp, DefDown,
    SpeedUp, SpeedDown,
    ResUp, ResDown,
    ExtraHitChance, // 추가타 발동 확률 버프
    Evasion, // 회피 버프

    // [2. 군중 제어 및 어그로 (CC기)]
    Electrocute,   // 감전 (스턴)
    Taunt,         // 도발
    Blind,         // 실명
    Nightmare,     // 악몽
    DamageShare,   // 아군의 피해를 대신 받음
    Thorn,

    // [3. 원소 기믹 및 특수 상태이상]
    Burn,          // 화상
    Wet,           // 젖음
    Curse,         // 저주 (힐 반전)
    Corrosion,      // 부식
    Shield
}

public enum TargetType
{
    Enemy,
    Self,
    Ally,
    AllyExceptSelf,
    All
}

// 이번 스킬 혹은 공격 타겟이 누군지 정의
public enum EffectTargetCategory
{
    MainTarget,      // 스킬의 메인 타겟 (가장 일반적)
    Self,            // 나 자신 (흡혈, 자기 버프 등)
    LowestHpAlly,    // 파티 내 가장 피가 적은 아군 (스마트 힐 등)
    AllAllies,       // 아군 전체
    AllEnemies,       // 적군 전체
    LowestHpEnemies,  // 체력이 가장 낮은 적 N명
    RandomEnemies,     // 무작위 적 N명
    AllAlliesExceptSelf
}

public enum EffectModifierType
{
    None,
    Flat,       // 단순 덧셈/뺄셈 
    Percent     // 곱셈 (비율)
}

[System.Serializable]
public struct EffectPayload
{
    [Header("Basic Settings")]
    public EffectType effectType; // 어떤 효과 카테고리인가?
    public float effectValue;     // 데미지 배율, 힐량, 버프 수치 등

    [Header("Damage Type")]
    public DamageFormulaType formulaType;

    [Header("Electricity Setting")]
    [Tooltip("감전 수치가 존재하는 스킬일 시 추가")]
    public float electricityFigure;
    public float electiricityRandomPercent;

    [Header("Targeting Setting")]
    [Tooltip("이 효과가 누구에게 적용될지 결정합니다.")]
    public EffectTargetCategory effectTarget;

    [Tooltip("LowestHpEnemies 등 특수 타겟팅 시 몇 명에게 적용할 것인가?")]
    public int targetCount;

    [Header("Status Effect Settings")]
    [Tooltip("Buff나 Debuff일 경우 어떤 상태이상을 걸 것인지 선택 / 특수한 경우 어떤 상태이상이 표적인지(ex. 적의 화상 턴 수만틈 MP 회복 시 화상이라고 설정용)")]
    public StatusEffectType statusEffectType;

    [Tooltip("버프 혹은 디버프 시 이 수치를 고정값(Flat)으로 더할지, 퍼센트(Percent)로 곱할지 결정합니다.")]
    public EffectModifierType modifierType;

    [Tooltip("Status Effect Duration")]
    public int durationTurns;

    [Header("Skill Specific Synergy (조건부 특수 기믹)")]
    [Tooltip("대상이 이 상태이상일 때 특수 기믹이 발동합니다.")]
    public StatusEffectType conditionTargetStatus;

    [Range(0f, 100f)]
    [Tooltip("조건을 만족했을 때, 이 특수 기믹이 실제로 발동할 확률 (100 = 무조건 발동)")]
    public float conditionApplyChance;

    public bool conditionIgnoreResistance;

    [Tooltip("조건 만족 시 최종 데미지/힐량 배율")]
    public float conditionMultiplier;

    [Tooltip("조건 만족 시 덮어씌울 추가 상태이상 (예: 감전)")]
    public StatusEffectType extraStatusOnCondition;

    [Tooltip("조건 만족 시 덮어씌울 추가 상태이상의 수치")]
    public float extraStatusValue;

    [Tooltip("조건 만족 시 덮어씌울 추가 상태이상의 수치를 고정값(Flat)으로 더할지, 퍼센트(Percent)로 곱할지 결정")]
    public EffectModifierType extraStatusModifierType;

    [Tooltip("조건 만족 시 덮어씌울 추가 상태이상의 턴")]
    public int extraStatusTurn;

    [Tooltip("기믹 발동 후 대상의 조건 상태이상을 삭제할 것인가?")]
    public bool removeConditionAfterHit;

    [Header("EnemyAI Only")]
    [Tooltip("AI가 이 효과를 평가할 때의 기본 가중치 점수")]
    public float aiWeight;
}

public enum DirectingType
{
    NormalRunIn,    // 양옆에서 달려오는 기본 연출
    SkyDropAmbush,  // 적들이 하늘에서 쿵! 하고 떨어지는 기습 연출
    BossEncounter   // 보스는 이미 가운데 서있고, 플레이어만 걸어 들어가는 연출
}

[System.Serializable] // 인스펙터에 보이게 해줌
public enum DamageFormulaType
{
    Standard,       // 기본 데미지
    FixedDamage,    // 고정 데미지: 방어력 무시
}

public static class GameEvents
{
    public static Action<GameState> OnStateChanged; // 게임 상태 변경

    // --------------------------------------------------------
    // 컷신 연출 관련
    // --------------------------------------------------------
    public static Action<DialogueData> OnRequestDialogue; // 대화창 열어달라는 요청
    public static Action OnCutsceneStart;                  // 컷신 시작됨 알림
    public static Action OnCutsceneEnd;                    // 컷신 완전히 종료됨 알림

    // RestArea
    public static Action<RestArea> OnEnableRestArea; // 휴식처 상호작용
    public static Action OnClickedRestButton; // 휴식처 휴식 버튼 클릭
    public static Action<string, Vector3> OnClickedTeleportButton; // 휴식처 텔레포트 버튼 클릭
    public static Action OnClickedRestAreaQuitButton; // 휴식처 퇴장 버튼 클릭
}

public static class BattleEvents
{
    // --------------------------------------------------------
    // 1. 턴 & 타임라인 관련
    // --------------------------------------------------------
    public static Action<List<ITurnEntity>> OnTurnOrderUpdated; // 계산기 -> UI: "타임라인 갱신해!"
    public static Action<ITurnEntity> OnTurnOverrideRequested;
    public static Action OnTimelineUpdateRequested; // 누군가 부활하거나, 속도 버프를 받아서 타임라인 순서를 다시 계산해야 할때

    public static Action OnBattleReadyToStart; // 배틀 준비 완료
    public static Action<List<CharacterStatus>> OnBattleUIReady;
    public static Action<BossStatus, bool> OnHpLocked;

    // --------------------------------------------------------
    // 2. 플레이어 UI 통제 관련
    // --------------------------------------------------------
    public static Action<CharacterStatus> OnTurnStarted; // FSM -> UI: "얘 턴이니까 스킬 메뉴 띄워!"
    public static Action<CharacterStatus> OnBreakOccurred;                // FSM -> UI: "Break 발생!"
    public static Action<bool> OnBattleEnded;            // FSM -> UI: "전투 종료!"
    public static Action<int, Dictionary<string, int>> OnShowResultUI;
    public static Action OnResultConfirmed;              // State -> UI: "결과창 [확인] 클릭 시"
    public static Action<bool> OnReturnToField;              // State -> UI: "결과창 [확인] 클릭 시"


    public static Action<CharacterStatus> OnCharacterClicked; // 공격 혹은 스킬 사용 시 클릭이 필요할 때
    public static Action<CharacterStatus> OnCharacterHovered; // 마우스가 호버되었을 때
    public static Action OnCharacterHoveredOut; // 마우스 호버 아웃되었을 때
    public static Action<bool, TargetType, List<CharacterStatus>> OnTargetingStateChanged; // Targeting 모드가 켜졌는지 아닌지 확인 (true = 켜짐, false = 꺼짐)

    // --------------------------------------------------------
    // 3. 플레이어 행동 '선택' 관련
    // --------------------------------------------------------
    public static Action<CharacterStatus, SkillData> OnSkillSelected; // UI -> FSM: "이 스킬 쓴대!"
    public static Action<CharacterStatus> OnNormalAttackSelected;     // UI -> FSM: "일반 공격 한대!"
    public static Action OnDefenseSelected;          // UI -> FSM: "방어!"
    public static Action<CharacterStatus, ItemData> OnItemSelected; // UI -> FSM: "이 아이템 쓴대!"
    public static Action<CharacterStatus> OnTrunBatonSelected; // UI -> FSM: "얘한테 턴 넘긴대!"
    public static Action OnMenuCanceled;

    // --------------------------------------------------------
    // 4. 행동 '종료' 관련
    // --------------------------------------------------------
    // 아군/적군 구별 없이 퉁칩니다! 현재 활성화된 State가 알아서 듣습니다.
    public static Action OnActionCompleted;

    // --------------------------------------------------------
    // 5. 데이터 갱신 관련
    // --------------------------------------------------------
    public static Action<CharacterStatus, int> OnHealthChanged; // 체력 변경 (데미지 팝업, 체력바 갱신)
    public static Action<CharacterStatus, int> OnMpChanged;     // MP 변경 (마나바 갱신)
    public static Action<CharacterStatus> OnEffectsChanged; // Effect 변경 (버프, 디버프 갱신)
    public static Action<CharacterStatus> OnCharacterDied; // 캐릭터 사망

    // 현재 전투 명단 가져오기
    public static Func<List<ITurnEntity>> RequestAllCombatants;

    // 현재 약점 세팅 가져오기
    public static Func<WeaknessSetting> RequestWeaknessSettings;

    // 아이템 리스트 업데이트
    public static Action OnInventoryUpdated;

    // Common 이펙트 호출
    public static Action<CharacterStatus, CommonVFXType> OnCommonVFXRequested;

    // public static Action<float, float> OnCameraShake;
}

// 전투에 참여하는 모든 캐릭터(아군/적군)가 반드시 상속받아야 하는 인터페이스
public interface ITurnEntity
{
    bool IsPlayer { get; }              // 아군이면 true, 몬스터면 false
    int CurrentHP { get; }                  // 현재 속도 스탯 (버프/디버프 반영된 최종값)
    int Speed { get; }                  // 현재 속도 스탯 (버프/디버프 반영된 최종값)

    float CurrentActionGauge { get; set; } // 현재 행동 게이지
    Transform EntityTransform { get; }  // 연출(데미지 팝업 등)을 띄울 캐릭터의 위치 좌표
}

public static class Utils
{
}
