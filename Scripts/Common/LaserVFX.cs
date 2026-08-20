using System.Collections;
using UnityEngine;

public class LaserVFX : MonoBehaviour
{
    // 레이저가 화면에 남아있는 시간 (예: 0.5초 뒤에 스르륵 사라짐)
    public float laserDuration = 0.5f;

    public void FireLaser(Vector3 startPos, Vector3 targetPos)
    {
        // 1. LineRenderer를 쓴다면 시작점과 끝점을 즉시 연결!
        LineRenderer line = GetComponent<LineRenderer>();
        if (line != null)
        {
            line.SetPosition(0, startPos);
            line.SetPosition(1, targetPos);
        }

        // 2. 파티클(Particle System)을 쓴다면 거리에 맞춰 길이를 쭈욱 늘려줌
        // (보통 Z축으로 뻗어나가는 파티클을 만들고, 거리를 계산해 Scale을 조절합니다)
        transform.position = startPos;
        transform.LookAt(targetPos);

        // 정해진 시간 뒤에 레이저 이펙트 스스로 파괴!
        Destroy(gameObject, laserDuration);
    }
}
