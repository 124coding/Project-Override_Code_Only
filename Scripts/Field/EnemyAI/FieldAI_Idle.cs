using UnityEngine;

public class FieldAI_Idle : IFieldAIState
{
    private float timer;

    public void Enter(FieldAIBrain brain)
    {
        brain.anim.speed = 1.0f;
        brain.anim.Play("Idle");

        // 대기 시작 시: 멈춰 서서 타이머 세팅
        brain.rb.linearVelocity = new Vector2(0, brain.rb.linearVelocity.y);
        timer = brain.idleWaitTime;
    }

    public void FixedUpdateState(FieldAIBrain brain)
    {
        timer -= Time.fixedDeltaTime;

        if (timer <= 0f)
        {
            brain.Flip(); // 다 쉬었으면 뒤로 돌고
            brain.ChangeState(brain.patrolState); // 다시 순찰!
        }
    }

    public void Exit(FieldAIBrain brain) { }
}