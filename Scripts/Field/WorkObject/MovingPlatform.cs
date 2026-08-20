using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour, IWorkObject
{
    [Header("이동 설정")]
    public List<Transform> waypoints; // [A, B, C, D] 딱 한 번씩만 넣으세요!
    public float speed = 2f;

    [Header("가감속 및 경로 설정")]
    public bool useSmoothEasing = true;
    [Tooltip("플랫폼의 가감속 곡선 (0~1 기준)")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("스위치/신호 설정")]
    public bool useSignal = false;

    public Vector2 PlatformVelocity { get; private set; }

    private bool isMoving = true;
    private Rigidbody2D rb;
    private int currentWaypointIndex = 0;
    private Vector2 lastPosition;

    private float distanceTraveled = 0f;
    private float totalDistance = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (useSignal) isMoving = false;

        // [핵심] 첫 번째 웨이포인트(A)에서 시작하여 두 번째(B)로 가게 정렬합니다.
        if (waypoints != null && waypoints.Count >= 2)
        {
            // 플랫폼의 위치를 강제로 첫 번째 노드(A) 좌표로 맞춥니다.
            rb.position = waypoints[0].position;
            transform.position = waypoints[0].position;

            // 다음 목적지는 index 1 (B 노드)
            currentWaypointIndex = 1;
            CalculateDistanceToNext();
        }

        lastPosition = rb.position;
    }

    // 다음 목적지까지의 순수한 물리적 거리를 계산하는 함수
    private void CalculateDistanceToNext()
    {
        if (waypoints.Count < 2) return;

        int prevIndex = (currentWaypointIndex - 1 + waypoints.Count) % waypoints.Count;
        totalDistance = Vector2.Distance(waypoints[prevIndex].position, waypoints[currentWaypointIndex].position);

        // 주의: 여기서 distanceTraveled를 0으로 무조건 초기화하지 않습니다! (초과분 이월을 위해)
    }

    void FixedUpdate()
    {
        if (!isMoving || waypoints == null || waypoints.Count < 2)
        {
            PlatformVelocity = Vector2.zero;
            return;
        }

        // 1. 이동 진행률 계산
        float step = speed * Time.fixedDeltaTime;
        distanceTraveled += step;

        float t = Mathf.Clamp01(distanceTraveled / totalDistance);

        // 2. 가감속 적용
        float curveT = useSmoothEasing ? moveCurve.Evaluate(t) : t;

        // 3. 실제 위치 계산
        int prevIndex = (currentWaypointIndex - 1 + waypoints.Count) % waypoints.Count;
        Vector2 startPos = waypoints[prevIndex].position;
        Vector2 targetPos = waypoints[currentWaypointIndex].position;

        Vector2 nextPos = Vector2.Lerp(startPos, targetPos, curveT);

        // 4. [핵심 수정] 속도 계산을 먼저 하고 이동!
        // rb.position이 아니라, '실제로 이번에 이동할 위치(nextPos)'를 기준으로 속도를 구해야 
        // 위에 탄 플레이어가 덜덜 떨지 않습니다.
        PlatformVelocity = (nextPos - lastPosition) / Time.fixedDeltaTime;
        lastPosition = nextPos;

        rb.MovePosition(nextPos);

        // 5. [핵심 수정] 지점 도달 시 초과분(Overshoot) 처리
        if (t >= 1.0f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;

            // 목표를 초과해서 이동한 남은 잉여 거리를 다음 구간으로 이월시킵니다.
            float overshoot = distanceTraveled - totalDistance;
            CalculateDistanceToNext();

            // 등속 이동일 경우 남은 거리를 더해줘야 멈칫하는 현상이 사라집니다.
            distanceTraveled = useSmoothEasing ? 0f : overshoot;
        }
    }

    public void WorkOn() { if (useSignal) isMoving = true; }
    public void WorkOff() { if (useSignal) isMoving = false; }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count; i++)
        {
            // Gizmos 선도 마지막 노드에서 첫 번째 노드로 이어지도록 Loop 처리
            Gizmos.DrawLine(waypoints[i].position, waypoints[(i + 1) % waypoints.Count].position);
            Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);
        }
    }
}