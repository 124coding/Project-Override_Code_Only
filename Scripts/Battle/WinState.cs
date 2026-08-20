using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinState : IBattleState
{
    private bool isResultConfirmed = false;
    private BattleManager battleManager;

    public WinState(BattleManager manager)
    {
        this.battleManager = manager;
    }

    public void Enter()
    {
        BattleEvents.OnResultConfirmed += ConfirmedHandler;
    }

    public IEnumerator Execute()
    {
        // 승리 연출 기다리기
        yield return new WaitForSeconds(3.0f);

        // 보상 정산 및 데이터 반영
        var (exp, items) = battleManager.ApplyEncounterRewards();

        // 승리 연출 방송
        BattleEvents.OnBattleEnded?.Invoke(true);

        // 결과창 띄우기 이벤트
        BattleEvents.OnShowResultUI?.Invoke(exp, items);

        // 확인 누를때까지 대기
        yield return new WaitUntil(() => isResultConfirmed);

        BattleEvents.OnReturnToField?.Invoke(true);
    }

    private void ConfirmedHandler()
    {
        isResultConfirmed = true;
    }

    public void Exit()
    {
        BattleEvents.OnResultConfirmed -= ConfirmedHandler;
    }
}
