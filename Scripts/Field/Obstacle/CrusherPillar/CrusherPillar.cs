using System.Collections;
using UnityEngine;

public class CrusherPillar : MonoBehaviour
{
    [Header("Transform Settings")]
    public Transform topPoint;      // 꼭대기 위치
    public Transform bottomPoint;   // 바닥 위치

    [Header("Timing Settings")]
    public float topWaitTime = 1.0f;      // 꼭대기에서 떨어지기 전 대기하는 시간
    public float dropDuration = 0.15f;    // 쾅! 떨어지는데 걸리는 시간 (매우 짧게)
    public float bottomWaitTime = 0.5f;   // 바닥을 찍고 멈춰있는 시간
    public float returnDuration = 2.0f;   // 스스스스 올라가는데 걸리는 시간 (길게)

    [Header("KillZone Collider")]
    public Collider2D killZoneCollider;

    private void Start()
    {
        // 시작할 때 꼭대기로 위치를 강제로 맞추고 패턴 시작
        transform.position = topPoint.position;
        StartCoroutine(CrushRoutine());
    }

    private IEnumerator CrushRoutine()
    {
        while (true) // 게임 내내 무한 반복
        {
            // 꼭대기에서 대기 (기 모으기)
            killZoneCollider.enabled = false;
            yield return new WaitForSeconds(topWaitTime);

            // 쾅! 떨어지기
            killZoneCollider.enabled = true;

            float t = 0f;
            Vector3 startPos = transform.position;

            while (t < 1f)
            {
                t += Time.deltaTime / dropDuration;
                // t * t 로 떨어지면 중력처럼 갈수록 빨라지며 꽂히는 타격감이 생깁니다.
                transform.position = Vector3.Lerp(startPos, bottomPoint.position, t * t);
                yield return null;
            }
            // 오차 보정을 위해 완벽하게 바닥 위치로 딱 맞춤
            transform.position = bottomPoint.position;

            // TODO: 여기서 CameraShake.Instance.Shake() 같은 함수를 부르거나 
            // 흙먼지 파티클을 재생하면 타격감이 미치도록 좋아집니다!

            yield return new WaitForSeconds(bottomWaitTime);

            killZoneCollider.enabled = false;

            t = 0f;
            startPos = transform.position;

            while (t < 1f)
            {
                t += Time.deltaTime / returnDuration;

                float easeT = t * t * t;
                transform.position = Vector3.Lerp(startPos, topPoint.position, easeT);
                yield return null;
            }
            transform.position = topPoint.position;
        }
    }
}