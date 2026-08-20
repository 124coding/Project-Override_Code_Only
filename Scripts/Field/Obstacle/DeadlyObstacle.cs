using UnityEngine;

public class DeadlyObstacle : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 닿은 대상이 플레이어인지 태그로 확인
        if (collision.CompareTag("Player"))
        {
            // 플레이어의 CharacterStatus(또는 HealthSystem)를 가져와 사망 처리
            if (collision.TryGetComponent(out FieldPlayerDeath playerDeath))
            {
                Debug.Log("플레이어가 치명적인 장애물에 닿아 사망했습니다!");
                playerDeath.Die();
            }
        }
    }
}