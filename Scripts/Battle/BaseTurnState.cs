using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseTurnState : IBattleState
{
    protected CharacterStatus currentActor;
    protected CharacterAction currentAction;
    protected List<ITurnEntity> allCombatants;
    protected bool isActionDone = false;

    public BaseTurnState(ITurnEntity turnOwner, List<ITurnEntity> allCombatants)
    {
        currentActor = turnOwner.EntityTransform.GetComponent<CharacterStatus>();
        currentAction = turnOwner.EntityTransform.GetComponent<CharacterAction>();

        this.allCombatants = allCombatants;

        currentAction.SetCombatants(allCombatants);
    }

    public virtual void Enter()
    {
        isActionDone = false;
        currentActor.isDefending = false;

        bool isDeadFromDot = currentActor.ProcessTurnStartEffects();

        if (isDeadFromDot)
        {
            Debug.Log($"{currentActor.name}은(는) 턴을 시작하기도 전에 쓰러졌습니다!");
            ActionCompletedHandler(); // 강제 턴 종료
            return;
        }

        if (currentActor.IsStunned)
        {
            Debug.Log($"{currentActor.name}은(는) 기절하여 행동할 수 없습니다!");
            ActionCompletedHandler(); // 행동 스킵 및 강제 턴 종료
            return;
        }

        // 공통 이벤트 구독
        BattleEvents.OnActionCompleted += ActionCompletedHandler;
        BattleEvents.OnTurnStarted?.Invoke(currentActor);
    }

    public virtual IEnumerator Execute()
    {
        // 공통 대기 로직
        yield return new WaitUntil(() => isActionDone);
    }

    public virtual void Exit()
    {
        // 공통 버프 틱 차감 및 구독 해제
        currentActor.TickEffects();
        BattleEvents.OnActionCompleted -= ActionCompletedHandler;
    }

    protected virtual void ActionCompletedHandler()
    {
        if (isActionDone) return;

        isActionDone = true;
    }
}