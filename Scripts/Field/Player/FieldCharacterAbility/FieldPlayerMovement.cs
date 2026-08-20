using UnityEngine;
using Unity.Cinemachine;

public class FieldPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float moveAcceleration = 50f;
    public float moveDeceleration = 50f;
    public bool isInputInverted = false;
    private float currentSpeed = 0f;
    private float currentAcceleration = 0f;
    private float currentDeceleration = 0f;
    private float currentPlayerVelocityX = 0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    public float coyoteTime = 0.15f;      // 발이 떨어져도 점프 가능한 시간
    public float CoyoteTimeCounter { get; private set; } // 외부에서 읽을 수 있게 프로퍼티화

    private bool isGrounded;

    private FieldPlayerMeleeAttack meleeAttack;

    public bool IsGrounded => isGrounded;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private MovingPlatform currentPlatform;

    public void SetCurrentSpeed(float speed)
    {
        currentPlayerVelocityX = speed;
    }

    public void SetOverrideSpeed(float newSpeed, float newAcceleration, float newDeceleration)
    {
        currentSpeed = newSpeed;
        currentAcceleration = newAcceleration;
        currentDeceleration = newDeceleration;
    }

    public void ResetSpeed()
    {
        currentSpeed = moveSpeed;
        currentAcceleration = moveAcceleration;
        currentDeceleration = moveDeceleration;
    }

    private void Awake()
    {
        meleeAttack = GetComponent<FieldPlayerMeleeAttack>();
        rb = GetComponent<Rigidbody2D>();
        ResetSpeed();
    }

    private void Update()
    {
        Collider2D groundHit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isGrounded = groundHit != null;

        if (isGrounded)
        {
            CoyoteTimeCounter = coyoteTime;
            currentPlatform = groundHit.GetComponentInParent<MovingPlatform>();
        }
        else
        {
            CoyoteTimeCounter -= Time.deltaTime;
            currentPlatform = null;
        }

        if (meleeAttack != null && meleeAttack.IsAttacking)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = InputManager.Instance.inputActions.Field.Move.ReadValue<Vector2>();

        if(moveInput.y > 0f)
        {
            moveInput.y = 0f;
        }

        if (isInputInverted) moveInput *= -1f;

        if (Mathf.Abs(moveInput.x) < 0.1f) moveInput.x = 0f;

        if (moveInput.x != 0)
        {
            FieldPlayerPushPull pushPull = GetComponent<FieldPlayerPushPull>();
            bool canFlip = (pushPull == null || !pushPull.IsGrabbing);

            if (canFlip)
            {
                float facingDir = moveInput.x > 0 ? 1f : -1f;
                transform.localScale = new Vector3(facingDir, 1f, 1f);
            }
        }
    }

    private void FixedUpdate()
    {
        if (meleeAttack != null && meleeAttack.IsAttacking)
        {
            currentPlayerVelocityX = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // 공중에서 키 입력이 없을 때, 물리적인 밀림(넉백 등)이 발생하면 속도 동기화
        if (!isGrounded && moveInput.x == 0)
        {
            if (Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(currentPlayerVelocityX))
            {
                currentPlayerVelocityX = rb.linearVelocity.x;
            }
        }

        float targetVelocityX = moveInput.x * currentSpeed;

        // 키를 뗐을 때의 감속도 (공중이면 0이 되어 관성이 유지됨)
        float activeDeceleration = isGrounded ? currentDeceleration : 0f;

        if (moveInput.x == 0 && activeDeceleration >= 100f)
        {
            currentPlayerVelocityX = 0f;
        }
        else
        {
            float accelRate = 0f;

            if (moveInput.x == 0)
            {
                // 키를 뗐을 때: 마찰력 적용 (공중에서는 0이 적용되어 날아가던 관성 유지)
                accelRate = activeDeceleration;
            }
            else if (Mathf.Sign(moveInput.x) != Mathf.Sign(currentPlayerVelocityX) && currentPlayerVelocityX != 0)
            {
                // 방향을 반대로 틀 때 (Turn Around)
                // 방향을 틀 때는 감속도(Decel)가 아니라 가속도(Accel)를 써야 공중에서도 조작

                float turnMultiplier = isGrounded ? 1f : 0.5f;
                accelRate = currentAcceleration * turnMultiplier;
            }
            else
            {
                // 같은 방향으로 계속 가속할 때
                accelRate = currentAcceleration;
            }

            currentPlayerVelocityX = Mathf.MoveTowards(
                currentPlayerVelocityX,
                targetVelocityX,
                accelRate * Time.fixedDeltaTime
            );
        }

        float finalVelocityX = currentPlayerVelocityX;

        if (currentPlatform != null) finalVelocityX += currentPlatform.PlatformVelocity.x;

        rb.linearVelocity = new Vector2(finalVelocityX, rb.linearVelocity.y);
    }

    public void ConsumeCoyoteTime()
    {
        CoyoteTimeCounter = 0f;
    }

    private void OnDisable()
    {
        // 컷씬이나 메뉴 창을 열어서 스크립트가 꺼지면 관성 없이 즉시 멈춤
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        currentPlayerVelocityX = 0f;
    }
}
