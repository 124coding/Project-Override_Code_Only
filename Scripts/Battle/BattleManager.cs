using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{

    private IBattleState currentBattleState;

    public AudioClip battleBGM;

    [SerializeField] private TurnCalculator turnCalculator;
    [SerializeField] private WeaknessSetting weaknessSettings;

    private void OnEnable()
    {
        BattleEvents.RequestWeaknessSettings += () => weaknessSettings;
        BattleEvents.OnReturnToField += HandleReturnToField;
    }
    private void OnDisable()
    {
        BattleEvents.RequestWeaknessSettings -= () => weaknessSettings;
        BattleEvents.OnReturnToField -= HandleReturnToField;
    }

    private void Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.FadeOutBGM(1.5f);

            SoundManager.Instance.FadeInBGM(battleBGM, 1.0f);
        }
    }

    public void InitBattle()
    {
        Debug.Log("--- 스폰 완료! 전투 명단 수집 및 턴 계산 시작 ---");

        turnCalculator.InitializeCombatants();

        StartCoroutine(BattleLoop());
    }

    public void ChangeState(IBattleState newState)
    {
        if (currentBattleState != null) currentBattleState.Exit(); // 이전 상태 퇴근
        currentBattleState = newState;                       // 새로운 상태 교체
        currentBattleState.Enter();                          // 새로운 상태 출근
    }

    IEnumerator BattleLoop()
    {
        yield return new WaitForSeconds(1.0f); // 초기화 대기

        while (true)
        {
            // 다음 턴을 부르기 전에 승패 확인
            if (turnCalculator.IsTeamWipedOut(false)) // 적군(false)이 전멸했는가?
            {
                Debug.Log("--- 승리! 적이 모두 전멸했습니다! ---");
                ChangeState(new WinState(this));

                yield return StartCoroutine(currentBattleState.Execute());

                yield break; // 전투 루프 완전 종료
            }

            if (turnCalculator.IsTeamWipedOut(true)) // 아군(true)이 전멸했는가?
            {
                Debug.Log("--- 패배! 아군이 모두 전멸했습니다! ---");
                ChangeState(new LoseState());

                yield return StartCoroutine(currentBattleState.Execute());

                yield break; // 전투 루프 완전 종료
            }

            // 다음 턴을 계산
            ITurnEntity currentTurnOwner = turnCalculator.GetNextTurnEntity();

            // A의 답장에 따라 State 갈아끼우기
            if (currentTurnOwner.IsPlayer)
            {
                ChangeState(new PlayerTurnState(currentTurnOwner, turnCalculator.allCombatants));
            }
            else
            {
                ChangeState(new EnemyTurnState(currentTurnOwner, turnCalculator.allCombatants));
            }
            // 턴이 끝날 때까지 대기
            yield return StartCoroutine(currentBattleState.Execute());

            yield return new WaitForSeconds(1.0f);
        }
    }

    private void SaveBattleResults()
    {
        // 참여 캐릭터들의 최종 상태를 DataManager에 업데이트
        var survivors = FindObjectsByType<CharacterStatus>(FindObjectsSortMode.None);
        foreach (var p in survivors)
        {
            if (p.IsPlayer) // 아군만 저장
            {
                DataManager.Instance.SaveCharacterStatusWithSO(
                    p.characterData.CharacterName, p.CurrentLevel, p.CurrentEXP, p.CurrentHP, p.CurrentSkillPoints, p.equippedSkills);
            }
        }

        // 게임을 꺼도 유지되도록 파일에 기록!
        DataManager.Instance.SaveGame();
    }

    public (int totalEXP, Dictionary<string, int> items) ApplyEncounterRewards()
    {
        EncounterReward originalReward = DataManager.Instance.currentEncounterReward;
        if (originalReward == null) originalReward = new EncounterReward();

        int totalExp = originalReward.totalEXP;

        // 생존 아군 경험치 지급
        var playerCharacters = FindObjectsByType<CharacterStatus>(FindObjectsSortMode.None);
        foreach (var p in playerCharacters)
        {
            if (p.IsPlayer && p.CurrentHP > 0)
            {
                p.AddEXP(totalExp);
            }
        }

        // 주사위 굴려서 획득한 아이템 딕셔너리 정산
        Dictionary<string, int> finalItems = new Dictionary<string, int>();

        foreach (DropItemData dropData in originalReward.dropItems)
        {
            if (Random.value <= dropData.dropChance)
            {
                int amount = Random.Range(dropData.minQuantity, dropData.maxQuantity + 1);

                if (finalItems.ContainsKey(dropData.itemID))
                    finalItems[dropData.itemID] += amount;
                else
                    finalItems.Add(dropData.itemID, amount);
            }
        }

        // 튜플로 한 번에 리턴
        return (totalExp, finalItems);
    }

    private void HandleReturnToField(bool isWin)
    {
        Debug.Log("사용자 확인 완료! 필드 씬으로 이동합니다.");

        Camera.main.orthographic = true;

        // 화면을 어둡게(FadeOut) 만듭니다.
        FadeManager.Instance.FadeOut(1.0f, () =>
        {
            // 화면이 완전히 검은색이 되었을 때 전투 결과 계산 및 저장 진행
            if (isWin)
            {
                Debug.Log("전투 데이터 계산 및 저장 시작...");

                string defeatedID = DataManager.Instance.currentBattleMonsterID;
                bool isRespawnable = DataManager.Instance.currentBattleIsRespawnable;

                DataManager.Instance.MarkDefeated(defeatedID, isRespawnable);

                SaveBattleResults();

                DataManager.Instance.isReturningFromBattle = true;
            }
            else
            {
                Debug.Log("전투 패배... 마지막 휴식처로 강제 귀환합니다.");

                // 파티원 전원 풀피로 부활
                DataManager.Instance.FullHealParty();

                // 잡몹 리스폰
                DataManager.Instance.ResetRespawnableMonsters();

                DataManager.Instance.isReturningFromBattle = false;
                DataManager.Instance.isLoadedFromSave = true;
            }

            // 필드 씬 로드 진행
            if (!string.IsNullOrEmpty(DataManager.Instance.lastFieldSceneName))
            {
                GameStateManager.Instance.ChangeState(GameState.Field);

                // 씬 비동기 로딩 또는 씬 로드 실행
                UnityEngine.SceneManagement.SceneManager.LoadScene(DataManager.Instance.lastFieldSceneName);
            }
            else
            {
                Debug.LogWarning("돌아갈 필드 씬 이름이 없습니다!");
            }
        });
    }
}
