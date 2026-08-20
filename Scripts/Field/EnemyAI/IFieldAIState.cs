using UnityEngine;

public interface IFieldAIState
{
    void Enter(FieldAIBrain brain);            // 상태 진입 시 1번 실행
    void FixedUpdateState(FieldAIBrain brain); // 상태 머무는 동안 매 물리 프레임 실행
    void Exit(FieldAIBrain brain);             // 상태 종료 시 1번 실행
}
