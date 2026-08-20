using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(FieldPlayerMovement))]
public class FieldPlayerWallJump : MonoBehaviour
{
    private PlayerMovementEffect effectManager;

    [Header("해금 및 설정")]
    public string requiredRelicId = "Relic_WallJump";
    public Transform wallCheck;
    public float wallCheckDistance = 0.2f;
    public LayerMask wallLayer;
    public float wallSlideSpeed = 2f;
    public Vector2 wallJumpForce = new Vector2(8f, 12f);
    public float wallJumpDuration = 0.2f;

    private Rigidbody2D rb;
    private FieldPlayerMovement movement;
    private bool isWallSliding;
    private int facingDirection;

    // 마지막으로 벽 점프를 했던 방향을 저장 (1: 오른쪽 벽 점프, -1: 왼쪽 벽 점프, 0: 없음)
    [SerializeField] private int lastJumpDirection = 0;

    public bool IsWallSliding => isWallSliding;

    private InputAction jumpAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<FieldPlayerMovement>();
        effectManager = GetComponent<PlayerMovementEffect>();
    }

    private void Start()
    {
        jumpAction = InputManager.Instance.inputActions.Field.Jump;
    }

    private void Update()
    {
        if (!DataManager.Instance.HasItem(requiredRelicId)) return;

        if (movement.IsGrounded)
        {
            lastJumpDirection = 0;
        }

        // 벽 감지 및 상태 처리 로직을 하나로 통합
        CheckWall();

        // 벽타기 중일 때 슬라이딩 처리
        HandleWallSlide();

        // 점프 입력
        if (jumpAction.WasPressedThisFrame() && isWallSliding)
        {
            StartCoroutine(WallJumpRoutine());
        }
    }

    private void CheckWall()
    {
        // 방향 계산
        facingDirection = transform.localScale.x > 0 ? 1 : -1;

        // 레이캐스트 실행
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, Vector2.right * facingDirection, wallCheckDistance, wallLayer);

        // 상태 체크
        bool isTouchingWall = (hit.collider != null && Mathf.Abs(hit.normal.y) < 0.1f);

        // 강제 리셋 조건 추가
        // 아래 조건 중 하나라도 만족하면 무조건 벽타기 해제
        if (movement.IsGrounded || !isTouchingWall)
        {
            if (isWallSliding)
            {
                isWallSliding = false;
                Debug.Log($"{isWallSliding}");
            }
            return; // 아래쪽 로직 수행 안 함
        }

        // 벽 점프 실행 조건
        bool isDifferentWall = (facingDirection != lastJumpDirection);

        if (isTouchingWall && !movement.IsGrounded && isDifferentWall)
        {
            isWallSliding = true;
            Debug.Log($"{isWallSliding}");
        }
    }

    private void HandleWallSlide()
    {
        if (isWallSliding && rb.linearVelocity.y < -wallSlideSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
    }

    private IEnumerator WallJumpRoutine()
    {
        // 점프 방향 기록 (현재 내가 붙어있는 벽의 방향을 기억)
        lastJumpDirection = facingDirection;

        movement.enabled = false;
        isWallSliding = false;
        Debug.Log($"{isWallSliding}");

        // 반대 방향으로 튕기기
        float jumpDirection = -facingDirection;
        rb.linearVelocity = Vector2.zero;

        if (effectManager != null)
        {
            effectManager.PlayWallJumpEffect(facingDirection);
        }

        rb.AddForce(new Vector2(wallJumpForce.x * jumpDirection, wallJumpForce.y), ForceMode2D.Impulse);

        transform.localScale = new Vector3(jumpDirection, 1f, 1f);

        // 이동 스크립트 속도 동기화
        movement.SetCurrentSpeed(wallJumpForce.x * jumpDirection);
        transform.position += new Vector3(jumpDirection * 0.1f, 0, 0);

        yield return new WaitForSeconds(wallJumpDuration);

        movement.enabled = true;
    }

    private void OnDrawGizmos()
    {
        if (wallCheck != null)
        {
            // 레이저가 쏘는 방향을 빨간색 선으로 그려줌
            Gizmos.color = Color.red;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + (Vector3.right * facingDirection * wallCheckDistance));
        }
    }
}