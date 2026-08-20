using UnityEngine;

// 인스펙터에서 고를 수 있는 두 가지 회전 모드
public enum RotationMode
{
    Continuous, // 평소처럼 윙~ 하고 계속 도는 모드
    Step        // 지정된 각도(예: 90도)씩 딱딱 끊어서 도는 모드
}

[RequireComponent(typeof(Rigidbody2D))]
public class RotatingObstacle : MonoBehaviour, IWorkObject
{
    [Header("Rotation Mode")]
    public RotationMode mode = RotationMode.Continuous;

    [Header("Continuous Settings")]
    [Tooltip("회전 속도 (양수: 반시계, 음수: 시계 방향)")]
    public float rotationSpeed = 150f;

    [Header("Step Settings")]
    [Tooltip("한 번에 회전할 각도 (예: 90, 180)")]
    public float stepAngle = 90f;
    [Tooltip("목표 각도까지 돌아가는 데 걸리는 시간 (초) - 숫자가 작을수록 휙! 돕니다")]
    public float stepDuration = 0.5f;
    [Tooltip("신호 없이 혼자 돌 때, 몇 초마다 한 번씩 돌 것인가?")]
    public float autoStepInterval = 2f;

    [Header("Signal Settings")]
    [Tooltip("체크하면 레버/버튼의 신호를 받아야만 회전합니다.")]
    public bool useSignal = false;

    private bool isActivated = true;
    private Rigidbody2D rb;

    // Step 회전 계산을 위한 변수들
    private bool isStepping = false;
    private float currentAngle;
    private float targetAngle;
    private float stepProgress;
    private float autoStepTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        currentAngle = rb.rotation;
        targetAngle = currentAngle;

        if (useSignal) isActivated = false;

        autoStepTimer = autoStepInterval;
    }

    void FixedUpdate()
    {
        // 연속 회전 모드
        if (mode == RotationMode.Continuous)
        {
            if (isActivated)
                rb.angularVelocity = rotationSpeed;
            else
                rb.angularVelocity = 0f;
        }
        // 스텝(각도) 회전 모드
        else if (mode == RotationMode.Step)
        {
            rb.angularVelocity = 0f; // 물리 엔진의 강제 회전 방지

            if (isStepping)
            {
                // SmoothStep을 사용하여 "처음에 휙! 돌고 끝에서 부드럽게 탁!" 멈추는 찰진 조작감 구현
                stepProgress += Time.fixedDeltaTime / stepDuration;
                float t = Mathf.SmoothStep(0f, 1f, stepProgress);

                // LerpAngle: 360도를 넘어가도 꼬이지 않게 각도를 안전하게 계산해 줍니다
                float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, t);
                rb.MoveRotation(newAngle);

                // 목표치에 도달하면 회전 종료
                if (stepProgress >= 1f)
                {
                    isStepping = false;
                    currentAngle = targetAngle;
                }
            }
            // 신호를 안 받는데 스텝 모드라면 -> 시계 초침처럼 알아서 일정 시간마다 탁! 탁! 돕니다.
            else if (isActivated && !useSignal)
            {
                autoStepTimer -= Time.fixedDeltaTime;
                if (autoStepTimer <= 0f)
                {
                    StartStepRotation(stepAngle);
                    autoStepTimer = autoStepInterval; // 타이머 초기화
                }
            }
        }
    }

    // IWorkObject 인터페이스 구현
    public void WorkOn()
    {
        if (!useSignal) return;

        if (mode == RotationMode.Continuous)
        {
            isActivated = true;
        }
        else if (mode == RotationMode.Step)
        {
            // 레버를 당기면 설정한 각도(예: 90도)만큼 '정방향'으로 돕니다!
            StartStepRotation(stepAngle);
        }
    }

    public void WorkOff()
    {
        if (!useSignal) return;

        if (mode == RotationMode.Continuous)
        {
            isActivated = false;
        }
        else if (mode == RotationMode.Step)
        {
            // 레버를 끄면 방금 돌았던 각도만큼 '역방향(-)'으로 되돌아옵니다!
            StartStepRotation(-stepAngle);
        }
    }

    // 부드러운 회전을 시작시키는 내부 함수
    private void StartStepRotation(float angleToMove)
    {
        // 도는 중에 또 신호가 들어오면 무시할지, 각도를 누적할지 결정 (여기선 각도 누적으로 구현)
        targetAngle = currentAngle + angleToMove;
        stepProgress = 0f;
        isStepping = true;
    }
}