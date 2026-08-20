using UnityEngine;

public class FieldAI_Patrol : IFieldAIState
{
    // 확률 설정: 이동 중 1초마다 약 5% 확률로 뒤를 돈다.
    private float flipChancePerSecond = 0.05f;

    public void Enter(FieldAIBrain brain) {
        brain.anim.speed = 1.6f;
        brain.anim.Play("Move");
    }

    public void FixedUpdateState(FieldAIBrain brain)
    {
        float dir = Mathf.Sign(brain.transform.localScale.x);

        // 랜덤 확률로 방향 전환 결정
        // Time.fixedDeltaTime을 곱해 프레임과 상관없이 일정 확률로 발동
        if (Random.value < flipChancePerSecond * Time.fixedDeltaTime)
        {
            brain.ChangeState(brain.idleState); // 멈춰서 두리번거림
            return;
        }

        // 기본 이동 로직
        brain.rb.linearVelocity = new Vector2(brain.patrolSpeed * dir, brain.rb.linearVelocity.y);

        // 기존 장애물 감지 로직
        bool isLedge = !Physics2D.Raycast(brain.ledgeCheck.position, Vector2.down, 0.1f, brain.groundLayer);
        bool isWall = Physics2D.Raycast(brain.wallCheck.position, Vector2.right * dir, 0.5f, brain.groundLayer);

        if (isLedge || isWall)
        {
            brain.ChangeState(brain.idleState);
        }
    }

    public void Exit(FieldAIBrain brain) { }
}