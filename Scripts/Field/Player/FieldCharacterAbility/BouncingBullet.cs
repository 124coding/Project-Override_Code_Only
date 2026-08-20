using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BouncingBullet : MonoBehaviour
{
    [Header("Bounce Settings")]
    public int maxBounces = 3;       // 최대 튕길 수 있는 횟수
    public float bulletSpeed = 20f;  // 총알 속도 (벽에 닿아도 느려지지 않게 유지)
    public float lifeTime = 5f;      // 안전장치 (무한히 튕기는 것 방지, 5초 뒤 자동 파괴)

    private int currentBounces = 0;
    private Rigidbody2D rb;
    private Vector2 lastVelocity;    // 충돌 직전의 속도를 기억할 변수

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 생성되고 일정 시간이 지나면 무조건 파괴되도록 예약
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        // [핵심] 유니티 물리 엔진은 부딪히는 순간 속도를 0이나 엉뚱한 값으로 바꿔버립니다.
        // 따라서 매 프레임마다 '부딪히기 직전의 온전한 속도(방향)'를 기억해 두어야 합니다!
        lastVelocity = rb.linearVelocity;

        // (선택 사항) 총알의 머리(앞부분)가 항상 날아가는 방향을 바라보게 회전
        if (rb.linearVelocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 2. 벽이나 바닥에 맞았을 때 튕기기
        currentBounces++;

        if (currentBounces > maxBounces)
        {
            // 최대 튕김 횟수를 초과하면 총알 파괴
            Destroy(gameObject);
            // TODO: 파괴될 때 불꽃 파티클이나 효과음 재생
        }
        else
        {
            // [반사각 계산] 
            // collision.contacts[0].normal은 부딪힌 벽의 표면(법선 벡터)입니다.
            // Vector2.Reflect 함수에 '내가 날아가던 방향'과 '벽의 표면'을 넣으면 완벽한 반사각이 튀어나옵니다!
            Vector2 surfaceNormal = collision.contacts[0].normal;
            Vector2 reflectDir = Vector2.Reflect(lastVelocity.normalized, surfaceNormal);

            // 계산된 반사 방향으로 속도를 쏴줍니다. (감속되지 않도록 원래 스피드 강제 유지)
            rb.linearVelocity = reflectDir * Mathf.Max(lastVelocity.magnitude, bulletSpeed);

            // TODO: 팅! 하는 도탄 효과음 재생
        }

        InteractObject interactTarget = collision.gameObject.GetComponent<InteractObject>();

        if (interactTarget != null)
        {
            if (interactTarget.canInteractByBullet)
            {
                interactTarget.InteractByBullet();

                Destroy(gameObject);
                return;
            }
        }
    }
}