using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 이동 스크립트가 있어야만 대시도 쓸 수 있습니다.
[RequireComponent(typeof(Rigidbody2D), typeof(FieldPlayerMovement))]
public class FieldPlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;

    private Rigidbody2D rb;
    private FieldPlayerMeleeAttack meleeAttack;
    private FieldPlayerMovement movement;

    private PlayerMovementEffect effectManager;

    public bool IsDashing { get; private set; } = false;


    private float nextDashTime = 0f;
    private float defaultGravity;

    private bool canDash = true;

    private InputAction dashAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<FieldPlayerMovement>();
        effectManager = GetComponent<PlayerMovementEffect>();

        defaultGravity = rb.gravityScale; // 원래 중력값 기억해두기
    }

    private void Start()
    {
        dashAction = InputManager.Instance.inputActions.Field.Dash;
    }

    private void Update()
    {
        if (movement.IsGrounded)
        {
            canDash = true;
        }

        bool isAttacking = (meleeAttack != null && meleeAttack.IsAttacking);

        // 대시 중이 아니고 & 쿨타임이 찼을 때만 발동
        if (canDash && dashAction.WasPressedThisFrame() && !IsDashing && Time.time >= nextDashTime && !isAttacking)
        {
            StartCoroutine(DashRoutine());
        }

    }

    private IEnumerator DashRoutine()
    {
        IsDashing = true;
        canDash = false;

        UI_DashCooltime.Instance.Dash(); // UI활성화
        if (effectManager != null) effectManager.StartDashTrail();

        movement.ConsumeCoyoteTime();
        movement.enabled = false;
        rb.gravityScale = 0f;

        // 1. 대시 시작 시: 반경 내의 모든 DeadlyObject 콜라이더 가져오기
        // 플레이어 주변에 충돌 중인 DeadlyObject들을 찾습니다.
        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D[] deadlyColliders = Physics2D.OverlapCircleAll(transform.position, 5f); // 적절한 탐색 반경

        List<Collider2D> ignoredColliders = new List<Collider2D>();

        foreach (var col in deadlyColliders)
        {
            if (col.GetComponent<DeadlyObstacle>() != null)
            {
                Physics2D.IgnoreCollision(playerCollider, col, true);
                ignoredColliders.Add(col); // 나중에 다시 복구하기 위해 리스트에 저장
            }
        }

        // 대시 이동 로직
        float dashDir = Mathf.Sign(transform.localScale.x);
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        if (effectManager != null) effectManager.StopDashTrail();

        // 2. 대시 종료 시: 무시했던 콜라이더들 복구
        foreach (var col in ignoredColliders)
        {
            if (col != null) // 콜라이더가 파괴되었을 수도 있으므로 체크
            {
                Physics2D.IgnoreCollision(playerCollider, col, false);
            }
        }

        rb.gravityScale = defaultGravity;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        movement.enabled = true;
        IsDashing = false;
        nextDashTime = Time.time + dashCooldown;
    }

    // 핵심: 대시 도중에 캐릭터를 교체해버리면 코루틴이 허공에서 붕 떠버리므로 강제 종료 처리
    private void OnDisable()
    {
        if(IsDashing)
        {
            StopAllCoroutines();
            IsDashing = false;

            if (effectManager != null) effectManager.StopDashTrail();

            if (movement != null) movement.enabled = true;
            if (rb != null)
            {
                rb.gravityScale = defaultGravity;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
    }
}