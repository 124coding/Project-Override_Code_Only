using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(FieldPlayerMovement))]
public class FieldPlayerDoubleJump : MonoBehaviour
{
    private PlayerMovementEffect effectManager;

    public string requiredRelicId = "Relic_DoubleJump";

    [Header("Double Jump Settings")]
    public float doubleJumpForce = 5f;

    [Header("Jump Buffer (조작감 보정)")]
    public float jumpBufferTime = 0.15f;
    private float jumpBufferCounter;

    private Rigidbody2D rb;
    private FieldPlayerMovement movement;
    private FieldPlayerDash dash;

    private bool canDoubleJump = false;

    private InputAction jumpAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<FieldPlayerMovement>();
        dash = GetComponent<FieldPlayerDash>();
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
            canDoubleJump = true;
        }

        // 스마트 입력 버퍼링 (슈퍼 점프 버그 방지)
        if (jumpAction.WasPressedThisFrame())
        {
            // 바닥에 있거나 코요테 타임 중일 때 누른 점프는 '기본 점프'의 몫입니다.
            // 따라서 허공에 있을 때(CoyoteTime <= 0) 누른 점프만 더블 점프로 예약합니다!
            if (movement.CoyoteTimeCounter <= 0f)
            {
                jumpBufferCounter = jumpBufferTime;
            }
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 실행 조건 검사
        bool canJumpBecauseNotDashing = (dash == null || !dash.IsDashing);

        // 더블 점프 실행!
        // 조건: (허공에서 누른 점프가 예약됨) && (허공임) && (이단점프 안 씀) && (대시 중 아님)
        if (jumpBufferCounter > 0f && movement.CoyoteTimeCounter <= 0f && canDoubleJump && canJumpBecauseNotDashing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
            if (effectManager != null) effectManager.PlayJumpEffect();

            canDoubleJump = false;
            jumpBufferCounter = 0f; // 점프를 뛰었으니 예약 초기화

            // TODO: 공중제비 애니메이션 및 이펙트
        }
    }
}