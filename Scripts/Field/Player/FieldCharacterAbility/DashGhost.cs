using UnityEngine;

public class DashGhost : MonoBehaviour
{
    private PlayerMovementEffect poolManager; // 오브젝트 풀링 매니저

    private SpriteRenderer sr;
    public float fadeSpeed = 3f; // 잔상이 사라지는 속도 (클수록 빨리 사라짐)

    public void SetPool(PlayerMovementEffect manager)
    {
        poolManager = manager;
    }

    // 플레이어 스크립트에서 잔상을 생성할 때 정보를 넘겨주는 함수
    public void Setup(Sprite playerSprite, bool flipX, Color color)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.sprite = playerSprite; // 플레이어의 현재 애니메이션 프레임 복사
        sr.flipX = flipX;         // 플레이어가 바라보는 방향 복사
        sr.color = color;         // 투명도 및 색상 적용
    }

    void Update()
    {
        Color c = sr.color;
        c.a -= fadeSpeed * Time.deltaTime;
        sr.color = c;

        // 완전히 투명해지면 파괴(Destroy)하지 않고 창고(Pool)로 반환!
        if (c.a <= 0f)
        {
            if (poolManager != null)
            {
                poolManager.ReturnGhost(this);
            }
            else
            {
                Destroy(gameObject); // 만약 매니저가 없다면 예비로 파괴
            }
        }
    }
}