using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTurnState : BaseTurnState
{
    public PlayerTurnState(ITurnEntity turnOwner, List<ITurnEntity> allCombatants)
        : base(turnOwner, allCombatants)
    {
    }

    public override void Enter()
    {
        base.Enter(); // 공통 설정 세팅
        Debug.Log($"Player_{currentActor.characterData.name} Turn Enter");

        // 플레이어 전용 UI 이벤트 구독
        BattleEvents.OnSkillSelected += HandleSkillSelected;
        BattleEvents.OnNormalAttackSelected += HandleNormalAttackSelected;
        BattleEvents.OnDefenseSelected += DefendHandler;
        BattleEvents.OnItemSelected += HandleItemSelected;
        BattleEvents.OnTrunBatonSelected += HandleTrunBatonSelected;
        BattleEvents.OnBreakOccurred += TriggerOneMore;
    }
    
    // 부모 Execute 실행

    public override void Exit()
    {
        base.Exit(); // 공통 구독 해제 및 틱 차감

        // 플레이어 전용 이벤트 구독 해제
        BattleEvents.OnSkillSelected -= HandleSkillSelected;
        BattleEvents.OnNormalAttackSelected -= HandleNormalAttackSelected;
        BattleEvents.OnDefenseSelected -= DefendHandler;
        BattleEvents.OnItemSelected -= HandleItemSelected;
        BattleEvents.OnTrunBatonSelected -= HandleTrunBatonSelected;
        BattleEvents.OnBreakOccurred -= TriggerOneMore;
    }

    // --- 아래는 플레이어 전용 핸들러들 (기존과 동일) ---
    private void HandleSkillSelected(CharacterStatus target, SkillData skill)
    {

        currentAction.ExecuteSkill(target, skill);
    }

    private void HandleNormalAttackSelected(CharacterStatus target)
    {
        if (currentAction == null) return;

        // 일반 공격 함수 실행!
        currentAction.ExecuteSkill(target, currentActor.characterData.basicAttackData);
    }

    private void HandleItemSelected(CharacterStatus target, ItemData item)
    {
        // 아이템 차감 시도
        if (DataManager.Instance.ConsumeItem(item))
        {
            // 실제 액션 실행
            currentAction.ExecuteItem(target, item);
        }
        else
        {
            // 차감 실패 시 (아이템 부족 등) 에러 사운드 출력 등
            Debug.Log("아이템을 사용할 수 없습니다.");
        }
    }

    private void DefendHandler()
    {
        if(currentAction == null)
        {
            Debug.LogWarning("[CharacterAction] 오브젝트가 파괴되어 스킬을 실행할 수 없습니다.");
            return;
        }

        currentAction.StartCoroutine(DefendHandlerRoutine());
    }

    // 실제 1초를 기다려주고 플래그를 제어할 진짜 코루틴 로직
    private IEnumerator DefendHandlerRoutine()
    {
        yield return currentAction.StartCoroutine(currentAction.ExecuteDefend());

        isActionDone = true;
    }

    private void HandleTrunBatonSelected(CharacterStatus target)
    {
        currentAction.ExecuteTurnBaton(target);
    }
    private void TriggerOneMore(CharacterStatus characterStatus = null)
    {
        Debug.Log("1 MORE 획득! 한 번 더 공격합니다!");
        BattleEvents.OnTurnOverrideRequested?.Invoke(currentActor);
    }
}