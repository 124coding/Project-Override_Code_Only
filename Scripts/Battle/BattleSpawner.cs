using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSpawner : MonoBehaviour
{
    [Header("LineSet")]
    public Vector3 playerCenter = new Vector3(-5f, 0f, 0f); // 아군 진영 중앙
    public Vector3 enemyCenter = new Vector3(5f, 0f, 0f);   // 적군 진영 중앙
    public Vector3 playerLineDirection = new Vector3(1f, 0f, 1f); // 줄을 서는 방향 (X축으로 줄서기)
    public Vector3 enemyLineDirection = new Vector3(1f, 0f, -1f);
    public float spacing = 2.5f;                            // 캐릭터 간격

    [Header("DirectingSet")]
    public float runInDuration = 3f; // 뛰어오는 데 걸리는 시간
    public float offScreenOffset = 15f; // 화면 밖 얼마나 멀리서 스폰하는지

    [Header("Prefab")]
    public GameObject characterPrefab; // CharacterStatus, Action 등이 붙어있는 빈 껍데기

    public BattleManager battleManager;

    // 연출 코루틴에서 조작하기 위해 멤버 변수로 캐싱
    private List<Transform> spawnedPlayers = new List<Transform>();
    private List<Transform> spawnedEnemies = new List<Transform>();
    private List<Animator> playerAnimators = new List<Animator>();
    private List<Animator> enemyAnimators = new List<Animator>();

    private List<Vector3> playerTargetPos = new List<Vector3>();
    private List<Vector3> enemyTargetPos = new List<Vector3>();

    private void OnEnable()
    {
        BattleEvents.OnBattleReadyToStart += StartDirecting;
    }

    private void OnDisable()
    {
        BattleEvents.OnBattleReadyToStart -= StartDirecting;
    }

    private void Start()
    {
        if (DataManager.Instance == null)
        {
            return;
        }
        playerTargetPos = CalculateFormation(DataManager.Instance.currentPartyMembers.Count, playerCenter, playerLineDirection);
        enemyTargetPos = CalculateFormation(DataManager.Instance.currentEnemyParty.Count, enemyCenter, enemyLineDirection);

        SpawnAndAssemble();

        SetInitialPositions();
    }

    private IEnumerator NormalRunInRoutine()
    {
        foreach (var p in spawnedPlayers)
        {
            if (p != null)
            {
                Vector3 scale = p.localScale;
                scale.x = Mathf.Abs(scale.x); // 원래 크기 비율을 유지하며 양수로 만듦
                p.localScale = scale;
            }
        }

        // 적군은 무조건 왼쪽을 바라보게 (Scale X = 음수)
        foreach (var e in spawnedEnemies)
        {
            if (e != null)
            {
                Vector3 scale = e.localScale;
                scale.x = -Mathf.Abs(scale.x); // 원래 크기 비율을 유지하며 음수(왼쪽)로 만듦
                e.localScale = scale;
            }
        }

        List<bool> isPlayerAlive = new List<bool>();

        for (int i = 0; i < spawnedPlayers.Count; i++)
        {
            var p = spawnedPlayers[i];
            CharacterStatus status = (p != null) ? p.GetComponent<CharacterStatus>() : null;

            // 체력이 0 초과인 살아있는 캐릭터만 구분
            bool alive = (status != null && status.CurrentHP > 0);
            isPlayerAlive.Add(alive);

            if (alive)
            {
                // 살아있는 캐릭터만 달리기 애니메이션 재생
                if (playerAnimators[i] != null) playerAnimators[i].Play("Run");
            }
            else
            {
                // 체력이 0인 캐릭터는 등장 연출 시작 전부터 제자리(TargetPos)에 배치
                p.position = playerTargetPos[i];
            }
        }

        // 달리기 애니메이션 On
        foreach (var anim in enemyAnimators) if (anim != null) anim.Play("Run");

        // 부드러운 이동 (Lerp + EaseOut)
        float elapsedTime = 0f;
        while (elapsedTime < runInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / runInDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            for (int i = 0; i < spawnedPlayers.Count; ++i)
                spawnedPlayers[i].position = Vector3.Lerp(playerTargetPos[i] + (Vector3.left * offScreenOffset), playerTargetPos[i], easeT);

            for (int i = 0; i < spawnedEnemies.Count; ++i)
                spawnedEnemies[i].position = Vector3.Lerp(enemyTargetPos[i] + (Vector3.right * offScreenOffset), enemyTargetPos[i], easeT);

            yield return null;
        }

        // 위치 강제 보정 및 대기(Idle) 상태 전환
        for (int i = 0; i < spawnedPlayers.Count; i++)
        {
            spawnedPlayers[i].position = playerTargetPos[i];
            if (isPlayerAlive[i] && playerAnimators[i] != null)
            {
                playerAnimators[i].Play("Idle");
            }
        }
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            spawnedEnemies[i].position = enemyTargetPos[i];
            if (enemyAnimators[i] != null) enemyAnimators[i].Play("Idle");
        }

        List<CharacterStatus> allCombatantsStatus = new List<CharacterStatus>();

        // 아군 상태 수집
        foreach (var t in spawnedPlayers)
            allCombatantsStatus.Add(t.GetComponent<CharacterStatus>());

        // 적군 상태 수집
        foreach (var t in spawnedEnemies)
            allCombatantsStatus.Add(t.GetComponent<CharacterStatus>());

        BattleEvents.OnBattleUIReady?.Invoke(allCombatantsStatus);

        Debug.Log("--- 기본 등장 연출 완료! 전투를 시작합니다! ---");
        battleManager.InitBattle(); // FSM 가동!
    }

    // 미구현 연출 (기획 대기중)
    private IEnumerator SkyDropRoutine()
    {
        Debug.Log("하늘 드랍 연출 미구현 - 기본 설정으로 즉시 전투 시작");
        battleManager.InitBattle();
        yield break;
    }

    private IEnumerator BossEncounterRoutine()
    {
        Debug.Log("보스전 연출 미구현 - 기본 설정으로 즉시 전투 시작");
        battleManager.InitBattle();
        yield break;
    }

    // 위치 계산
    private List<Vector3> CalculateFormation(int count, Vector3 center, Vector3 dir)
    {
        List<Vector3> positions = new List<Vector3>();
        if (count == 0) return positions;

        float totalLength = (count - 1) * spacing;
        float startOffset = -totalLength / 2f;

        for (int i = 0; i < count; ++i)
        {
            Vector3 pos = center + dir.normalized * (startOffset + (i * spacing));
            positions.Add(pos);
        }
        return positions;
    }

    private void SpawnAndAssemble()
    {
        List<CharacterData> currentPlayerParty = DataManager.Instance.currentPartyMembers;
        // 아군 조립
        for (int i = 0; i < currentPlayerParty.Count; ++i)
        {
            CharacterData data = currentPlayerParty[i];

            // 공용 껍데기 생성
            GameObject baseObj = Instantiate(characterPrefab, playerTargetPos[i], Quaternion.identity);
            baseObj.name = $"Player_{data.CharacterName}";

            // 스크립트 가져오기
            CharacterStatus status = baseObj.GetComponent<CharacterStatus>();
            EffectSystem effectSys = baseObj.GetComponent<EffectSystem>();
            effectSys.Initialize(status);

            // DataManager에서 이 캐릭터의 세이브 데이터가 있는지 찾아보기
            CharacterSaveData savedData = DataManager.Instance.LoadCharacterStatus(data.CharacterName);

            // 찾은 데이터들을 통째로 Initialize 함수에 찔러넣기!
            status.Initialize(data, savedData);

            // 외형(Visual) 부착 및 Animator 추출
            Animator anim = null;
            if (data.visualModelPrefab != null)
            {
                GameObject visual = Instantiate(data.visualModelPrefab, baseObj.transform);
                visual.transform.localPosition = Vector3.zero;
                anim = visual.GetComponent<Animator>();

                if (status.CurrentHP <= 0)
                {
                    SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
                    if (sr != null && data.DeathSprite != null)
                    {
                        sr.sprite = data.DeathSprite;
                    }

                    if (anim != null) anim.enabled = false;
                }
            }

            spawnedPlayers.Add(baseObj.transform);
            playerAnimators.Add(anim);
        }

        List<CharacterData> enemyParty = DataManager.Instance.currentEnemyParty;

        // 적군 조립
        for (int i = 0; i < enemyParty.Count; ++i)
        {
            CharacterData data = enemyParty[i];

            // 공용 껍데기 생성
            GameObject baseObj = Instantiate(characterPrefab, enemyTargetPos[i], Quaternion.identity);
            baseObj.name = $"Enemy_{data.CharacterName}";

            EffectSystem effectSys = baseObj.GetComponent<EffectSystem>();

            // Status 및 AI 부착 (보스 여부에 따라 분기)
            if (data.isBoss)
            {
                // 보스인 경우 프리팹에 미리 붙어있던 기본 CharacterStatus를 제거
                CharacterStatus defaultStatus = baseObj.GetComponent<CharacterStatus>();
                if (defaultStatus != null) DestroyImmediate(defaultStatus);

                // BossStatus를 부착하고 초기화
                BossStatus bossStatus = baseObj.AddComponent<BossStatus>();
                bossStatus.Initialize(data, null);

                effectSys.Initialize(bossStatus);
            }
            else
            {
                CharacterStatus status = baseObj.GetComponent<CharacterStatus>();
                status.Initialize(data, null);

                effectSys.Initialize(status);
            }

            EnemyAI ai = baseObj.AddComponent<EnemyAI>();
            ai.InitializeAI(data);

            // 외형(Visual) 부착 (적군은 왼쪽을 바라보도록 180도 회전)
            Animator anim = null;
            if (data.visualModelPrefab != null)
            {
                GameObject visual = Instantiate(data.visualModelPrefab, baseObj.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                anim = visual.GetComponent<Animator>();
            }

            spawnedEnemies.Add(baseObj.transform);
            enemyAnimators.Add(anim);
        }
    }

    private void StartDirecting()
    {
        switch (DataManager.Instance.currentDirectingType)
        {
            case DirectingType.NormalRunIn:
                StartCoroutine(NormalRunInRoutine());
                break;
            case DirectingType.SkyDropAmbush:
                StartCoroutine(SkyDropRoutine());
                break;
            case DirectingType.BossEncounter:
                StartCoroutine(BossEncounterRoutine());
                break;
            default:
                StartCoroutine(NormalRunInRoutine());
                break;
        }
    }

    private void SetInitialPositions()
    {
        switch (DataManager.Instance.currentDirectingType)
        {
            case DirectingType.NormalRunIn:
            default:
                for (int i = 0; i < spawnedPlayers.Count; i++)
                    spawnedPlayers[i].position = playerTargetPos[i] + (Vector3.left * offScreenOffset);

                // 적군은 오른쪽 화면 밖으로 숨김
                for (int i = 0; i < spawnedEnemies.Count; i++)
                    spawnedEnemies[i].position = enemyTargetPos[i] + (Vector3.right * offScreenOffset);
                break;

                // TODO: 맞는 Directing에 맞게 수정
            case DirectingType.SkyDropAmbush:
                break;
            case DirectingType.BossEncounter:
                break;
        }
    }
}
