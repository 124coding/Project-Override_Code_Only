using System;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FieldAIBrain : MonoBehaviour
{
    [Header("Behavior Settings")]
    [Tooltip("체크하면 순찰 및 추격을 하지 않고 제자리에서 대기만 합니다")]
    public bool isStationary = false;
    private IFieldAIState currentState;

    [Header("Movement Settings")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4.5f;
    public float idleWaitTime = 2f;

    [Header("Detection")]
    public float sightDistance = 5f;
    public float loseAggroDistance = 8f;   // 추격을 포기하고 돌아가는 끈기 거리 (원형 범위)
    public LayerMask playerLayer;
    public LayerMask groundLayer;

    [Header("Sensors")]
    public Transform ledgeCheck;
    public Transform wallCheck;

    // State들이 접근할 수 있도록 공개(public)해 둡니다.
    public Rigidbody2D rb { get; private set; }
    public Animator anim { get; private set; }
    public Transform targetPlayer;

    // 상태 인스턴스들을 미리 만들어둡니다. (메모리 절약)
    public FieldAI_Idle idleState = new FieldAI_Idle();
    public FieldAI_Patrol patrolState = new FieldAI_Patrol();
    public FieldAI_Chase chaseState = new FieldAI_Chase();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (isStationary)
        {
            ChangeState(idleState);
        }
        else
        {
            ChangeState(patrolState);
        }
    }

    private void FixedUpdate()
    {
        if (!isStationary)
        {
            DetectPlayer();

            // 현재 켜져 있는 상태의 행동을 실행합니다.
            if (currentState != null)
            {
                currentState.FixedUpdateState(this);
            }
        }
    }

    public void ChangeState(IFieldAIState newState)
    {
        if (currentState != null) currentState.Exit(this); // 이전 상태 퇴근
        currentState = newState;
        currentState.Enter(this);                          // 새 상태 출근
    }

    private void DetectPlayer()
    {
        // 아직 쫓는 중이 아닐 때 (평화롭게 순찰/대기 중일 때)
        if (currentState != chaseState)
        {
            // 이때는 앞쪽으로 시야(레이저)를 쏴서 플레이어가 시야에 들어오는지 봅니다.
            float dir = Mathf.Sign(transform.localScale.x);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * dir, sightDistance, playerLayer);

            if (hit.collider != null)
            {
                targetPlayer = hit.transform;
                ChangeState(chaseState); // 발견! 추격 시작!
            }
        }
        // 이미 쫓는 중일 때 (흥분해서 쫓아갈 때)
        else
        {
            // 이때는 시야(레이저)가 닿는지 안 닿는지는 신경 쓰지 않습니다!
            if (targetPlayer != null)
            {
                float distance = Vector2.Distance(transform.position, targetPlayer.position);

                // 거리가 너무 멀어졌다면?
                if (distance > loseAggroDistance)
                {
                    targetPlayer = null;     // 타겟을 머릿속에서 지우고
                    ChangeState(idleState);  // 포기하고 멈춰서 두리번거립니다.
                }
            }
        }
    }

    public void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        float dir = Mathf.Sign(transform.localScale.x);

        // 1. 발견하는 시야 (빨간색 레이저)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector2.right * dir * sightDistance);

        // 2. 추격을 포기하는 거리 (노란색 원)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, loseAggroDistance);
    }
}
