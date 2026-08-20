using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTurnState : BaseTurnState
{
    private EnemyAI enemyAI;
    private CharacterStatus characterStatus;
    private BossStatus bossStatus;

    // base 키워드를 통해 부모의 생성자 로직을 그대로 실행합니다.
    public EnemyTurnState(ITurnEntity turnOwner, List<ITurnEntity> allCombatants)
        : base(turnOwner, allCombatants)
    {
        enemyAI = turnOwner.EntityTransform.GetComponent<EnemyAI>();
        characterStatus = turnOwner.EntityTransform.GetComponent<CharacterStatus>();
        bossStatus = turnOwner.EntityTransform.GetComponent<BossStatus>();
    }

    public override void Enter()
    {
        base.Enter(); // 부모의 공통 초기화 실행
        Debug.Log($"Enemy_{currentActor.characterData.name} Turn Enter");

        // 적의 턴이 '시작(Enter)'될 때 브레이크 상태 검사
        if (characterStatus != null && characterStatus.IsFullyBroken)
        {
            if (bossStatus != null)
            {
                // 보스라면 껍질을 다시 여러 개 랜덤 생성하여 재생성
                bossStatus.RecoverFromBreak();
                Debug.Log($"[{characterStatus.gameObject.name}]가 브레이크 상태에서 회복되며 약점을 재정비합니다.");
            }

            // 연출 팁: 껍질이 재생성되거나 보호막이 쳐지는 UI/VFX 이펙트 연출을 위한 이벤트 호출
            // BattleEvents.OnShieldRegenerated?.Invoke(characterStatus);
        }
    }

    public override IEnumerator Execute()
    {
        if (enemyAI != null)
        {
            enemyAI.TakeTurn(allCombatants);
        }
        else
        {
            Debug.LogWarning($"{currentActor.name}에게 EnemyAI 컴포넌트가 없습니다! 턴 강제 종료.");
            isActionDone = true;
        }

        // 부모의 대기 루프 실행 (WaitUntil)
        yield return base.Execute();
    }

    // Exit()는 추가할 게 없으면 아예 안 적어도 부모의 Exit()가 자동으로 실행됩니다!
}