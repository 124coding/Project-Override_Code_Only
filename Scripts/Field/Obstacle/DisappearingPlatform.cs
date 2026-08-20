using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class DisappearingPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    public float delayBeforeDisappear = 0.5f;
    public float respawnTime = 2.0f;

    [Header("Shake Settings")]
    public float shakeIntensity = 0.05f; // 흔들림 강도

    private Collider2D col;
    private SpriteRenderer sr;
    private bool isTriggered = false;
    private Vector3 originalPosition; // 원래 위치 저장

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        originalPosition = transform.localPosition; // 초기 위치 기록
    }

    private void OnDisable()
    {
        ResetPlatform();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 태그 확인
        if (!collision.gameObject.CompareTag("Player") || isTriggered) return;

        // 플레이어가 발판을 위에서 밟았는지 확인
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                StartCoroutine(DisappearRoutine());
                break;
            }
        }
    }

    private IEnumerator DisappearRoutine()
    {
        isTriggered = true;

        float elapsed = 0f;
        while (elapsed < delayBeforeDisappear)
        {
            // Sin 함수를 이용해 좌우로 흔들림
            float shake = Mathf.Sin(elapsed * 50f) * shakeIntensity;
            transform.localPosition = originalPosition + new Vector3(shake, 0, 0);

            // 색상 변화 연출 (회색으로 점점 변함)
            if (sr != null) sr.color = Color.Lerp(Color.white, Color.gray, elapsed / delayBeforeDisappear);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition; // 위치 원상복구
        col.enabled = false;
        if (sr != null) sr.enabled = false;

        yield return new WaitForSeconds(respawnTime);

        ResetPlatform();
    }

    private void ResetPlatform()
    {
        col.enabled = true;
        if (sr != null)
        {
            sr.enabled = true;
            sr.color = Color.white;
        }
        transform.localPosition = originalPosition;
        isTriggered = false;
    }
}