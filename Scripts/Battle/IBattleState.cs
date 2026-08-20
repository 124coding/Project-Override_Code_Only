using System.Collections;
using UnityEngine;

public interface IBattleState
{
    void Enter(); // 상태 진입
    IEnumerator Execute(); // 상태 머무는 중
    void Exit(); // 상태 퇴장
}
