using UnityEngine;

public class PillarKillZone : MonoBehaviour
{
    // OnTriggerEnter가 아닌 OnTriggerStay를 사용해야 합니다.
    // 공중에서 기둥에 맞은 상태로 바닥까지 밀려 내려왔을 때(실시간)를 감지하기 위함입니다.
    private void OnTriggerStay2D(Collider2D collision)
    {
        // 1. 충돌한 대상이 '플레이어'인지 확인합니다.
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // 2. 변경됨: 이제 바닥 감지(IsGrounded)는 Movement 스크립트가 관리합니다.
            FieldPlayerMovement movement = collision.GetComponent<FieldPlayerMovement>();

            // (안전장치) 이동 스크립트가 잘 붙어있다면?
            if (movement != null)
            {
                // 3. 기둥과 충돌 중인데, 플레이어의 발이 땅에 닿아있다면 = '깔렸다'고 판정!
                if (movement.IsGrounded)
                {
                    Debug.Log("플레이어가 기둥에 깔려 사망했습니다!");

                    // TODO: 플레이어 사망 처리 (예: 체력 깎기, 체크포인트로 리스폰 등)
                    // 예시: collision.GetComponent<FieldPlayerStatus>().TakeDamage(999);
                }
            }
        }
    }
}