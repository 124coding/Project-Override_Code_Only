using UnityEngine;

public class FieldAI_Chase : IFieldAIState
{
    public void Enter(FieldAIBrain brain) {
        brain.anim.speed = 2.0f;
        brain.anim.Play("Move");
    }

    public void FixedUpdateState(FieldAIBrain brain)
    {
        if (brain.targetPlayer == null) return;

        float dir = Mathf.Sign(brain.transform.localScale.x);

        // 플레이어가 내 등 뒤로 몰래 도망갔다면 즉시 홱! 하고 뒤돌기
        if ((brain.targetPlayer.position.x > brain.transform.position.x && dir == -1) ||
            (brain.targetPlayer.position.x < brain.transform.position.x && dir == 1))
        {
            brain.Flip();
            dir *= -1; // 방향 갱신
        }

        // 아무리 쫓아갈 때라도 눈앞이 낭떠러지면 멈춰서 노려봄
        bool isLedge = !Physics2D.Raycast(brain.ledgeCheck.position, Vector2.down, 1f, brain.groundLayer);
        if (isLedge)
        {
            brain.rb.linearVelocity = new Vector2(0, brain.rb.linearVelocity.y);
            return;
        }

        // 전속력으로 이동!
        brain.rb.linearVelocity = new Vector2(brain.chaseSpeed * dir, brain.rb.linearVelocity.y);
    }

    public void Exit(FieldAIBrain brain) { }
}