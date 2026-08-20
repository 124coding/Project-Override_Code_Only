using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserEmitter : MonoBehaviour
{
    public bool isOn = true;       // 레이저 켜짐 여부
    public float maxDistance = 20f; // 레이저 최대 사거리
    public int maxBounces = 50;     // 레이저 최대 몇 번 튕길 수 있는지
    public LayerMask hitLayer;      // 벽, 동력원 등 레이저가 막히는 레이어

    [Header("Fire Point")]
    public Transform firePoint;

    private LineRenderer line;
    private List<Vector3> laserPoints = new List<Vector3>();

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2; // 선의 시작점과 끝점
    }

    private void Update()
    {
        if (!isOn)
        {
            line.positionCount = 0;
            return;
        }

        laserPoints.Clear();

        Vector3 currentPos = firePoint.position;
        currentPos.z = 0f;

        Vector2 currentDir = -firePoint.up;

        laserPoints.Add(currentPos);

        int mirrorLayerIndex = LayerMask.NameToLayer("Mirror");

        // 레이저가 튕기는 횟수만큼 반복하며 Raycast를 쏩니다.
        for (int i = 0; i < maxBounces; i++)
        {
            Physics2D.queriesHitTriggers = true;
            RaycastHit2D hit = Physics2D.Raycast(currentPos, currentDir, maxDistance, hitLayer);
            Physics2D.queriesHitTriggers = false;

            if (hit.collider != null)
            {

                Vector3 hitPoint = hit.point;
                hitPoint.z = 0f;
                laserPoints.Add(hit.point); // 닿은 곳을 점으로 추가

                // 거울에 맞았을 때 (태그 검사)
                if (hit.collider.gameObject.layer == mirrorLayerIndex)
                {
                    // 입사각과 거울의 표면(Normal)을 계산해 반사각을 알아냅니다.
                    currentDir = Vector2.Reflect(currentDir, hit.normal);

                    // 다음 레이캐스트가 자기 자신(거울)을 맞추지 않도록 시작점을 아주 살짝 앞으로 띄워줍니다.
                    currentPos = hit.point + currentDir * 0.01f;
                }
                // 동력원(센서)에 맞았을 때
                else if (hit.collider.TryGetComponent(out LaserReceiver laserReceiver))
                {
                    laserReceiver.ReceiveLaser();
                    break; // 센서에 닿았으니 레이저는 여기서 멈춤
                }
                // 일반 벽에 맞았을 때
                else
                {
                    break; // 거울이 아닌 벽이면 흡수되고 멈춤
                }
            }
            else
            {
                // 아무것도 맞지 않으면 허공으로 끝까지 뻗어나감
                Vector3 endPos = currentPos + (Vector3)(currentDir * maxDistance);
                endPos.z = 0f;
                laserPoints.Add(endPos);
                break;
            }
        }

        // 찾아낸 모든 꺾임 지점들을 LineRenderer에 적용
        line.positionCount = laserPoints.Count;
        line.SetPositions(laserPoints.ToArray());
    }
}