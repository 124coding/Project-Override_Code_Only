using UnityEngine;
using System.Collections;
using UnityEngine.Pool;

public class FallingRock : MonoBehaviour
{
    private IObjectPool<FallingRock> myPool;
    public Sprite IdleSprite;
    private SpriteRenderer sr;
    private Animator anim;
    private Rigidbody2D rb;
    private Coroutine dropCoroutine; // 코루틴 제어용 변수

    private bool isCrashed = false; // 중복 충돌 방지용 플래그

    [SerializeField] private float shakeAmount = 0.1f;
    [SerializeField] private float shakeDuration = 1.0f;
    [SerializeField] private float fallGravity = 3.0f;
    [SerializeField] private float lifeTime = 5.0f;

    public void SetPool(IObjectPool<FallingRock> pool) => myPool = pool;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        isCrashed = false; // 충돌 플래그 초기화
        transform.rotation = Quaternion.identity;

        rb.simulated = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        anim.SetTrigger("Idle");

        if (dropCoroutine != null) StopCoroutine(dropCoroutine);
        dropCoroutine = StartCoroutine(DropRoutine());
    }

    void OnDestroy()
    {
        myPool = null;
    }

    private IEnumerator DropRoutine()
    {
        yield return null;

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        rb.simulated = false; // 물리 연산 끄기

        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-shakeAmount, shakeAmount);
            transform.position = startPos + new Vector3(offsetX, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPos;
        rb.simulated = true;
        rb.gravityScale = fallGravity;

        // 일정 시간 후 풀로 복귀 (Destroy 대신)
        yield return new WaitForSeconds(lifeTime);

        if (myPool != null && gameObject.activeInHierarchy)
        {
            myPool.Release(this);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isCrashed) return;

        // 땅이나 플레이어에 닿으면 즉시 복귀
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent(out FieldPlayerDeath playerDeath))
            {
                playerDeath.Die();
            }
            Crash();
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Crash();
        }
    }

    private void Crash()
    {
        isCrashed = true;
        rb.simulated = false;
        rb.linearVelocity = Vector2.zero;

        anim.SetTrigger("Crash");
    }

    public void OnCrashAnimationEnd()
    {
        if (myPool != null && gameObject.activeInHierarchy)
        {
            sr.sprite = IdleSprite;
            myPool.Release(this);
        }
    }
}