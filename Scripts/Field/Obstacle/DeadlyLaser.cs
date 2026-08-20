using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DeadlyLaser : MonoBehaviour, IWorkObject
{
    [Header("Deadly Laser Settings")]
    public bool isOn = true;               // 레이저가 켜져 있는지
    public float maxDistance = 30f;        // 레이저 최대 사거리
    public LayerMask obstacleLayer;        // 레이저가 막히는 레이어 (Wall, Ground, Player 등)

    [Header("Fire Point")]
    public Transform firePoint;            // 레이저가 발사되는 위치

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2; // 선의 시작점과 끝점
    }

    // IWorkObject 구현: 레버나 발판으로 레이저를 켭니다.
    public void WorkOn()
    {
        isOn = true;
    }

    // IWorkObject 구현: 레버나 발판으로 레이저를 끕니다.
    public void WorkOff()
    {
        isOn = false;
        line.positionCount = 0; // 선 지우기
    }

    private void Update()
    {
        if (!isOn) return;

        line.positionCount = 2;
        ShootDeadlyLaser();
    }

    private void ShootDeadlyLaser()
    {
        Vector2 startPos = firePoint.position;
        Vector2 direction = -firePoint.up;

        line.SetPosition(0, startPos);

        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, maxDistance, obstacleLayer);

        if (hit.collider != null)
        {
            line.SetPosition(1, hit.point);

            if (hit.collider.CompareTag("Player"))
            {
                bool isDashing = hit.collider.TryGetComponent(out FieldPlayerDash dashScript) && dashScript.IsDashing;

                if (isDashing)
                {
                    Debug.Log("대시 중이라 레이저를 통과했습니다!");
                }
                else
                {
                    // 대시 중이 아니라면 사망 처리
                    if (hit.collider.TryGetComponent(out FieldPlayerDeath playerDeath))
                    {
                        playerDeath.Die();
                    }
                }
            }
        }
        else
        {
            line.SetPosition(1, startPos + (direction * maxDistance));
        }
    }
}