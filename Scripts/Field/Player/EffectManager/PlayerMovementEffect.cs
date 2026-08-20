using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementEffect : MonoBehaviour
{
    [Header("Dash Ghost Settings")]
    public GameObject ghostPrefab;       // 잔상 프리팹
    public float ghostDelay = 0.05f;     // 잔상 생성 간격
    public Color ghostColor = new Color(0.5f, 0.5f, 1f, 0.6f);
    public int poolSize = 10;

    private SpriteRenderer playerSr;     // 플레이어 스프라이트 렌더러
    private Coroutine dashTrailCoroutine;

    // 오브젝트 풀(창고) 역할을 할 Queue
    private Queue<DashGhost> ghostPool = new Queue<DashGhost>();

    [Header("Jump Effect Settings")]
    public GameObject jumpEffectPrefab;  // 점프 먼지 파티클 (세 점프 모두 공유)
    public int jumpPoolSize = 10;        // 3가지 점프가 공유하므로 넉넉하게 10개
    public float jumpEffectDuration = 0.45f;

    [Header("Spawn Points")]
    public Transform footPoint;          // 일반/더블 점프용 발밑 위치
    public Transform wallPoint;          // 벽 점프용 기준 위치 (선택 사항)

    // 각각의 점프가 서로 씹히지 않도록 관리하는 독립 플래그
    private bool isJumpEffectPlaying = false;
    private bool isDoubleJumpEffectPlaying = false;
    private bool isWallJumpEffectPlaying = false;

    private Queue<GameObject> jumpPool = new Queue<GameObject>();

    private void Awake()
    {
        // 플레이어의 SpriteRenderer를 가져옵니다.
        playerSr = GetComponent<SpriteRenderer>();
        InitializeDashPool();
        InitializeJumpPool(); // 점프 풀 초기화 함수
    }


    //================
    // Dash
    //================

    private void InitializeDashPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject ghostObj = Instantiate(ghostPrefab, transform.position, Quaternion.identity);
            ghostObj.SetActive(false); // 생성 후 바로 끄기

            DashGhost ghost = ghostObj.GetComponent<DashGhost>();
            ghost.SetPool(this); // 잔상에게 내가 매니저임을 알려줌

            ghostPool.Enqueue(ghost); // 창고에 넣기
        }
    }

    // 창고에서 잔상을 하나 꺼내오는 함수
    private DashGhost GetGhost()
    {
        if (ghostPool.Count > 0)
        {
            DashGhost ghost = ghostPool.Dequeue();
            ghost.gameObject.SetActive(true); // 꺼내서 켜기
            return ghost;
        }
        else
        {
            // 만약 대시를 너무 많이 해서 10개로 부족하다면 즉석에서 하나 더 만들기
            GameObject ghostObj = Instantiate(ghostPrefab);
            DashGhost ghost = ghostObj.GetComponent<DashGhost>();
            ghost.SetPool(this);
            return ghost;
        }
    }

    // 다 쓴 잔상을 다시 창고에 넣는 함수
    public void ReturnGhost(DashGhost ghost)
    {
        ghost.gameObject.SetActive(false); // 다시 끄기
        ghostPool.Enqueue(ghost);          // 창고에 반환
    }

    // 대시 스크립트에서 대시 시작 시 호출할 함수
    public void StartDashTrail()
    {
        if (dashTrailCoroutine != null) StopCoroutine(dashTrailCoroutine);
        dashTrailCoroutine = StartCoroutine(GhostTrailRoutine());
    }

    // 대시 스크립트에서 대시 종료 시 호출할 함수
    public void StopDashTrail()
    {
        if (dashTrailCoroutine != null)
        {
            StopCoroutine(dashTrailCoroutine);
            dashTrailCoroutine = null;
        }
    }

    // 일정한 간격으로 잔상을 생성하는 코루틴
    private IEnumerator GhostTrailRoutine()
    {
        while (true)
        {
            // Instantiate 대신 창고(Pool)에서 꺼내오기!
            DashGhost ghost = GetGhost();

            // 위치와 스케일 맞추기
            ghost.transform.position = transform.position;
            ghost.transform.rotation = transform.rotation;
            ghost.transform.localScale = transform.localScale;

            ghost.Setup(playerSr.sprite, playerSr.flipX, ghostColor);

            yield return new WaitForSeconds(ghostDelay);
        }
    }


    //================
    // Jump
    //================
    private void InitializeJumpPool()
    {
        for (int i = 0; i < jumpPoolSize; i++)
        {
            GameObject jumpObj = Instantiate(jumpEffectPrefab, transform.position, Quaternion.identity);
            jumpObj.SetActive(false);
            jumpPool.Enqueue(jumpObj);
        }
    }

    private void PlayJumpEffectCore(Vector3 position, Quaternion rotation, Action onStart, Action onComplete)
    {
        onStart?.Invoke(); // 해당 점프의 재생 상태를 true로 만듦

        GameObject effect;
        if (jumpPool.Count > 0) effect = jumpPool.Dequeue();
        else effect = Instantiate(jumpEffectPrefab);

        // 월드 좌표를 기준으로 세팅 (버그 방지)
        effect.transform.position = position;
        effect.transform.rotation = rotation;
        effect.transform.localScale = Vector3.one;

        effect.SetActive(true);

        // 재생 완료 및 반환 코루틴
        StartCoroutine(ReturnJumpEffectRoutine(effect, onComplete));
    }

    private IEnumerator ReturnJumpEffectRoutine(GameObject effect, Action onComplete)
    {
        yield return new WaitForSeconds(jumpEffectDuration);

        // 창고에 넣기 전 깨끗하게 초기화
        effect.transform.position = Vector3.zero;
        effect.transform.rotation = Quaternion.identity;
        effect.transform.localScale = Vector3.one;

        effect.SetActive(false);
        jumpPool.Enqueue(effect);

        onComplete?.Invoke(); // 해당 점프의 재생 상태를 false로 되돌림
    }

    // ==========================================
    // 외부 호출용 API (점프 스크립트들이 호출할 곳)
    // ==========================================

    // 1. 일반 점프
    public void PlayJumpEffect()
    {
        if (isJumpEffectPlaying) return;

        Vector3 spawnPos = (footPoint != null) ? footPoint.position : transform.position - new Vector3(0, 0.5f, 0);

        // 람다식(Lambda)을 이용해 자신의 플래그만 조작하도록 전달
        PlayJumpEffectCore(spawnPos, Quaternion.identity,
            () => isJumpEffectPlaying = true,
            () => isJumpEffectPlaying = false);
    }

    // 2. 더블 점프
    public void PlayDoubleJumpEffect()
    {
        if (isDoubleJumpEffectPlaying) return;

        // 더블 점프도 보통 발밑에서 생성되므로 footPoint를 사용합니다.
        Vector3 spawnPos = (footPoint != null) ? footPoint.position : transform.position - new Vector3(0, 0.5f, 0);

        PlayJumpEffectCore(spawnPos, Quaternion.identity,
            () => isDoubleJumpEffectPlaying = true,
            () => isDoubleJumpEffectPlaying = false);
    }

    // 3. 벽 점프
    public void PlayWallJumpEffect(float wallDirection)
    {
        if (isWallJumpEffectPlaying) return;

        Vector3 spawnPos = (wallPoint != null) ? wallPoint.position : transform.position + new Vector3(wallDirection * 0.5f, 0, 0);
        Quaternion spawnRot = Quaternion.Euler(0, 0, (wallDirection > 0) ? 90f : -90f);

        PlayJumpEffectCore(spawnPos, spawnRot,
            () => isWallJumpEffectPlaying = true,
            () => isWallJumpEffectPlaying = false);
    }
}