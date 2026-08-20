using System;
using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    // 발사!
    public void Fire(Transform target, float speed, Action onHitCallback)
    {
        StartCoroutine(FlyRoutine(target, speed, onHitCallback));
    }

    private IEnumerator FlyRoutine(Transform target, float speed, Action onHitCallback)
    {
        Vector3 startPos = transform.position;

        // 대상의 몸통 중앙을 향하도록 약간 위쪽(up * 1f)으로 보정
        Vector3 targetPos = target.position + Vector3.up * 1.0f;

        // 대상을 바라보게 회전 (화살촉이 적을 향함)
        transform.LookAt(targetPos);

        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / speed; // 거꾸로 걸리는 시간 계산
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }

        // 도착 완료!
        transform.position = targetPos;

        // 나를 쏜 CharacterAction에게 "도착했어!" 라고 콜백 신호를 보냄
        onHitCallback?.Invoke();

        // 내 임무는 끝났으니 화살 오브젝트 파괴
        Destroy(gameObject);
    }
}