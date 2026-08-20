using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnPrediction
{
    public ITurnEntity Character;
    public float TimeToTurn;
}

public class TurnCalculator : MonoBehaviour
{
    // 게임 씬에 있는 모든 캐릭터 명단
    public List<ITurnEntity> allCombatants = new List<ITurnEntity>();

    // 현재 턴을 기다리는 대기열 (UI 담당자 B에게 전달할 데이터)
    public List<ITurnEntity> turnQueue = new List<ITurnEntity>();

    // 스킬 등으로 인해 '다음 턴'으로 강제 지정된 캐릭터
    private ITurnEntity forcedNextEntity = null;

    // 행동 게이지의 결승선 (트랙 길이)
    private const float TARGET_GAUGE = 10000f;

    public int maxTimelineSlots = 10;

    private bool isTimelineDirty = false;

    private void OnEnable()
    {
        BattleEvents.OnTurnOverrideRequested += SetNextTurnOverride;
        BattleEvents.OnTimelineUpdateRequested += RequestTimelineUpdate;

        BattleEvents.RequestAllCombatants += ReturnAllCombatants;
    }

    private void OnDisable()
    {
        BattleEvents.OnTurnOverrideRequested -= SetNextTurnOverride;
        BattleEvents.OnTimelineUpdateRequested -= RequestTimelineUpdate;

        BattleEvents.RequestAllCombatants -= ReturnAllCombatants;
    }

    public void RequestTimelineUpdate()
    {
        isTimelineDirty = true;
    }

    private void LateUpdate()
    {
        if (isTimelineDirty)
        {
            UpdateTimeline();
            isTimelineDirty = false; // 깃발 회수
        }
    }

    // 최초 턴계산
    public void InitializeCombatants()
    {
        // 씬에 존재하는 모든 ITurnEntity 명찰을 단 오브젝트를 찾아 명단에 넣습니다.
        allCombatants = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<ITurnEntity>()
            .OrderBy(e => e.EntityTransform.position.x) // 핵심: X좌표 오름차순(왼쪽->오른쪽) 정렬
            .ToList();

        ApplyEncounterAdvantage();

        // 최초 타임라인 갱신
        UpdateTimeline();
    }

    private void ApplyEncounterAdvantage()
    {
        // TODO: 기획에 맞게 수정
        foreach (var entity in allCombatants)
        {
            switch (DataManager.Instance.currentEncounterType)
            {
                case EncounterType.PlayerAdvantage:
                    // 아군 선제공격: 아군들의 행동 게이지를 절반(5000) 채우고 시작!
                    if (entity.IsPlayer) entity.CurrentActionGauge = 5000f;
                    break;

                case EncounterType.EnemyAdvantage:
                    // 적 기습: 적군들의 행동 게이지를 절반 채우고 시작!
                    if (!entity.IsPlayer) entity.CurrentActionGauge = 5000f;
                    break;

                case EncounterType.Parried:
                    // 패링 성공: 아군은 5000 채우고, 적군은 기절(게이지 -2000 등 페널티) 적용!
                    if (entity.IsPlayer) entity.CurrentActionGauge = 5000f;
                    else entity.CurrentActionGauge = -2000f;
                    break;

                case EncounterType.Normal:
                default:
                    // 평범한 조우면 모두 게이지 0에서 스피드 경쟁 시작
                    entity.CurrentActionGauge = 0f;
                    break;
            }
        }
    }

    // 턴 순서 정렬 및 UI 업데이트 (미래 예측)
    public void UpdateTimeline()
    {
        List<TurnPrediction> predictions = new List<TurnPrediction>();

        // 각 캐릭터의 미래 턴을 N바퀴 앞까지 미리 계산합니다.
        foreach (var c in allCombatants)
        {
            if (c.Speed <= 0) continue; // 속도가 0이면 제외

            int predictCount = 10;
            for (int i = 0; i < predictCount; i++)
            {
                // i = 0: 이번 턴, i = 1: 다음 턴, i = 2: 다다음 턴...
                // 목표치 = (결승선 - 현재 위치) + (바퀴 수 * 결승선)
                float requiredGauge = (TARGET_GAUGE - c.CurrentActionGauge) + (i * TARGET_GAUGE);
                float timeToReach = requiredGauge / (float)c.Speed;

                predictions.Add(new TurnPrediction { Character = c, TimeToTurn = timeToReach });
            }
        }

        // 계산된 모든 미래의 턴들을 '남은 시간' 기준으로 오름차순 정렬!
        predictions = predictions.OrderBy(p => p.TimeToTurn).ToList();

        // 정렬된 예측 결과에서 캐릭터 알맹이만 빼내어 UI 개수만큼만 자릅니다.
        // 이때 빠른 캐릭터는 리스트 안에 2~3번 중복해서 들어갈 수 있습니다!
        turnQueue = predictions.Select(p => p.Character).Take(maxTimelineSlots).ToList();

        // 새치기(궁극기 등) 처리
        if (forcedNextEntity != null)
        {
            // 타임라인 어딘가에 있는 강제 턴 캐릭터를 지우고 맨 앞에 꽂아줍니다.
            turnQueue.Remove(forcedNextEntity);
            turnQueue.Insert(0, forcedNextEntity);
        }

        // [B와의 협업] "턴 대기열 10칸이 갱신되었어!" 라고 방송
        BattleEvents.OnTurnOrderUpdated?.Invoke(turnQueue);
    }

    private List<ITurnEntity> ReturnAllCombatants()
    {
        return allCombatants;
    }

    // 특정 아군에게 다음 턴 넘기기(새치기)
    public void SetNextTurnOverride(ITurnEntity targetEntity)
    {
        forcedNextEntity = targetEntity;
        RequestTimelineUpdate(); // 강제 갱신 예약
    }

    public ITurnEntity GetNextTurnEntity()
    {
        ITurnEntity currentTurnEntity = null;

        if (forcedNextEntity != null)
        {
            currentTurnEntity = forcedNextEntity;
            forcedNextEntity = null;
            currentTurnEntity.CurrentActionGauge = TARGET_GAUGE;
        }
        else
        {
            float minTicksNeeded = float.MaxValue;

            foreach (var entity in allCombatants)
            {
                if (entity.Speed <= 0) continue;

                float ticksNeeded = (TARGET_GAUGE - entity.CurrentActionGauge) / entity.Speed;
                if (ticksNeeded < minTicksNeeded)
                {
                    minTicksNeeded = ticksNeeded;
                    currentTurnEntity = entity;
                }
            }

            foreach (var entity in allCombatants)
            {
                entity.CurrentActionGauge += (entity.Speed * minTicksNeeded);
            }
        }

        RequestTimelineUpdate();

        return currentTurnEntity;
    }

    // 특정 팀 점멸했는지 확인
    public bool IsTeamWipedOut(bool isPlayerTeam)
    {
        // 명단(allCombatants)을 뒤져서,
        // 해당 팀(isPlayerTeam) 소속이면서 속도가 0보다 큰(살아있는) 사람이 있는지 검사합니다.
        bool hasAliveMember = allCombatants.Any(e => e.IsPlayer == isPlayerTeam && e.CurrentHP > 0);

        // 살아있는 사람이 한 명도 없다면(!hasAliveMember) 전멸(true)입니다!
        return !hasAliveMember;
    }
}