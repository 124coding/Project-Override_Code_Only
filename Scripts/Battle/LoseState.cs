using System;
using System.Collections;
using UnityEngine;

public class LoseState : IBattleState
{
    private bool isResultConfirmed = false;

    public void Enter()
    {
        BattleEvents.OnResultConfirmed += ConfirmedHandler;
    }

    public IEnumerator Execute()
    {
        // 패배 연출 기다리기
        yield return new WaitForSeconds(3.0f);

        Debug.Log("--- 패배 연출 ---");


        // 패배 연출 방송
        BattleEvents.OnBattleEnded?.Invoke(false);

        // 결과창 띄우기 이벤트
        BattleEvents.OnShowResultUI?.Invoke(0, null);

        // 확인 누를때까지 대기
        yield return new WaitUntil(() => isResultConfirmed);

        BattleEvents.OnReturnToField?.Invoke(false);
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
