using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FieldPlayerMovement))]
public class FieldPlayerPushPull : MonoBehaviour
{
    public string requiredRelicId = "Relic_PushPull";

    [Header("PushPull Set")]
    public float grabDistance = 1f;
    public LayerMask pushableLayer;       // 1차 방어: Pushable 레이어만 레이저에 걸림
    public Transform rayPoint;

    [Header("Grab Cooldown")]
    public float grabCooldown = 1f;    // 상자를 놓고 다시 잡을 수 있을 때까지의 최소 대기 시간
    private float nextGrabTime = 0f;

    private PushableBox grabbedBox;       // GameObject 대신 우리가 만든 상자 스크립트를 저장!
    private FixedJoint2D joint;
    private FieldPlayerMovement movement;
    private Rigidbody2D rb;               // 플레이어 가속도 제어용 추가
    private bool isGrabbing = false;

    public bool IsGrabbing => isGrabbing;

    private InputAction interactAction;
    private InputAction moveAction;

    private void Awake()
    {
        movement = GetComponent<FieldPlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        interactAction = InputManager.Instance.inputActions.Field.Interact;
        moveAction = InputManager.Instance.inputActions.Field.Move;
    }

    void Update()
    {
        if (!DataManager.Instance.HasItem(requiredRelicId)) return;

        if (isGrabbing && !movement.IsGrounded)
        {
            StopGrabbing();
            return;
        }

        if (!isGrabbing)
        {
            if (movement.IsGrounded && Time.time >= nextGrabTime)
            {
                Vector2 rayDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                RaycastHit2D hit = Physics2D.Raycast(rayPoint.position, rayDirection, grabDistance, pushableLayer);

                if (interactAction.IsPressed())
                {
                    if (hit.collider != null)
                    {
                        PushableBox targetBox = hit.collider.GetComponent<PushableBox>();
                        if (targetBox != null) StartGrabbing(targetBox);
                    }
                }
            }
        }
        else
        {
            if (interactAction.WasReleasedThisFrame() || !interactAction.IsPressed())
            {
                StopGrabbing();
            }
            else
            {
                // 상자를 잡은 채로 방향키를 놓았다면, 상자의 미세한 관성도 즉시 죽임!
                if (Mathf.Abs(moveAction.ReadValue<Vector2>().x) < 0.1f)
                {
                    if (grabbedBox != null)
                    {
                        Rigidbody2D boxRb = grabbedBox.GetComponent<Rigidbody2D>();
                        if (boxRb != null)
                        {
                            // 상자의 X축 속도와 회전 관성을 강제로 0으로 만들어 플레이어를 끌고 가지 못하게 함
                            boxRb.linearVelocity = new Vector2(0f, boxRb.linearVelocity.y);
                            boxRb.angularVelocity = 0f;
                        }
                    }
                }
            }
        }
    }

    void StartGrabbing(PushableBox box)
    {
        isGrabbing = true;
        grabbedBox = box;

        // 상자 본인에게 '너 잡혔어' 라고 알려줌 (상자 쪽 스크립트 함수 실행)
        grabbedBox.OnGrabbed();

        // 밧줄(Joint) 연결
        joint = gameObject.AddComponent<FixedJoint2D>();
        joint.connectedBody = grabbedBox.GetComponent<Rigidbody2D>();

        joint.breakForce = float.PositiveInfinity;
        joint.breakTorque = float.PositiveInfinity;

        if (movement != null)
        {
            movement.SetOverrideSpeed(
                grabbedBox.boxPushSpeed,
                grabbedBox.boxPushAcceleration,
                grabbedBox.boxPushDeceleration
            );
        }
    }

    void StopGrabbing()
    {
        // 상자 본인에게 '너 놨어' 라고 알려줌
        if (grabbedBox != null)
        {
            grabbedBox.OnReleased();

            Rigidbody2D boxRb = grabbedBox.GetComponent<Rigidbody2D>();
            if (boxRb != null) boxRb.linearVelocity = new Vector2(0f, boxRb.linearVelocity.y);
        }

        isGrabbing = false;
        grabbedBox = null;

        // 밧줄 끊기
        if (joint != null)
        {
            Destroy(joint);
        }

        if (movement != null) movement.ResetSpeed();

        // 다시 잡기까지 쿨타임 적용
        nextGrabTime = Time.time + grabCooldown;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void OnDisable()
    {
        if (isGrabbing) StopGrabbing();
    }

    //private void OnDrawGizmos()
    //{
    //    if (rayPoint != null)
    //    {
    //        // 상자를 잡고 있을 때는 빨간색, 안 잡고 있을 때는 녹색으로 표시
    //        Gizmos.color = isGrabbing ? Color.red : Color.green;

    //        // 플레이어가 바라보는 방향 계산
    //        Vector2 rayDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

    //        // 시작점(RayPoint)부터 도착점(방향 * 거리)까지 선을 그립니다.
    //        Gizmos.DrawLine(rayPoint.position, (Vector2)rayPoint.position + rayDirection * grabDistance);
    //    }
    //}
}