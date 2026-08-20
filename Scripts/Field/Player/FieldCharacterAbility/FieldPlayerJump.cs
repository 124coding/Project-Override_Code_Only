using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(FieldPlayerMovement))]
public class FieldPlayerJump : MonoBehaviour
{
    private PlayerMovementEffect effectManager;

    [Header("Jump Settings")]
    public float jumpForce = 5f;

    [Header("Jump Buffer (조작감 보정)")]
    public float jumpBufferTime = 0.15f;  // 땅에 닿기 전 미리 눌러도 예약되는 시간
    private float jumpBufferCounter;

    [Header("Gravity Multipliers")]
    public float fallMultiplier = 3f;     // 떨어질 때 가해질 중력 배수
    public float lowJumpMultiplier = 2f;    // 점프 키를 짧게 눌렀을 때 가해질 중력 배수
    private float defaultGravity;           // 원래 중력 스케일 저장용

    private Rigidbody2D rb;
    private FieldPlayerMeleeAttack meleeAttack;
    private FieldPlayerMovement movement;
    private FieldPlayerDash dash;
    private FieldPlayerWallJump wallJump;

    private InputAction jumpAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravity = rb.gravityScale;

        movement = GetComponent<FieldPlayerMovement>();
        dash = GetComponent<FieldPlayerDash>();
        meleeAttack = GetComponent<FieldPlayerMeleeAttack>();
        wallJump = GetComponent<FieldPlayerWallJump>();

        effectManager = GetComponent<PlayerMovementEffect>();
    }

    private void Start()
    {
        jumpAction = InputManager.Instance.inputActions.Field.Jump;
    }

    private void Update()
    {
        // 1. 점프 입력 버퍼링 (누르는 순간 타이머 충전)
        if (jumpAction.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime; // 안 누르고 있으면 타이머 깎임
        }

        bool canJumpBecauseNotDashing = (dash == null || !dash.IsDashing);
        bool canJumpBecauseNotAttacking = (meleeAttack == null || !meleeAttack.IsAttacking);

        // 2. 점프 실행!
        // 조건: (점프 키가 최근에 눌렸음) && (아직 코요테 타임이 남아있음)
        if (jumpBufferCounter > 0f && movement.CoyoteTimeCounter > 0f && canJumpBecauseNotDashing && canJumpBecauseNotAttacking)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            if (effectManager != null) effectManager.PlayJumpEffect();

            jumpBufferCounter = 0f;
            movement.ConsumeCoyoteTime();
        }
    }

    private void FixedUpdate()
    {
        if (dash != null && dash.IsDashing)
        {
            return;
        }

        if (wallJump != null && wallJump.IsWallSliding)
        {
            rb.gravityScale = defaultGravity;
            return;
        }

        // [비대칭 중력 로직]
        if (rb.linearVelocity.y < 0)
        {
            // 떨어지는 중일 때: 묵직하게 떨어지도록 중력 증가
            rb.gravityScale = defaultGravity * fallMultiplier;
        }
        else if (rb.linearVelocity.y > 0 && !jumpAction.IsPressed())
        {
            // 올라가는 중인데 점프 키를 뗐을 때: 즉시 떨어지도록 중력 증가 (소점프)
            rb.gravityScale = defaultGravity * lowJumpMultiplier;
        }
        else
        {
            // 평상시 (바닥에 있거나 점프 키를 꾹 누르며 상승 중일 때): 기본 중력
            rb.gravityScale = defaultGravity;
        }
    }
}