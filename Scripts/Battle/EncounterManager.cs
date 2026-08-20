using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance { get; private set; }

    [Header("Encounter Settings")]
    public float fadeDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TriggerEncounter(List<CharacterData> enemyList, int enemyLevel,  EncounterType encounterType, DirectingType directingType, string monsterID, bool isRespawnable, BattleStageData stageData)
    {
        Debug.Log("전투 발생! 전역 매니저에서 화면 전환 시작...");

        DataManager.Instance.SetPlayerLastPositionAndScene();

        DataManager.Instance.isReturningFromBattle = true;
        GameStateManager.Instance.ChangeState(GameState.Battle);

        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeOut(fadeDuration, () =>
            {
                StartCoroutine(LoadBattleSceneAsync(enemyList, enemyLevel, encounterType, directingType, monsterID, isRespawnable, stageData));
            });
        }
        else
        {
            StartCoroutine(LoadBattleSceneAsync(enemyList, enemyLevel, encounterType, directingType, monsterID, isRespawnable, stageData));
        }
    }

    private IEnumerator LoadBattleSceneAsync(List<CharacterData> enemyList, int enemyLevel, EncounterType encounterType, DirectingType directingType, string monsterID, bool isRespawnable, BattleStageData stageData)
    {
        // DataManager에 전투 데이터를 세팅합니다. 
        DataManager.Instance.StartBattle(enemyList, enemyLevel, encounterType, directingType, monsterID, isRespawnable, stageData);

        // 비동기 씬 로드 시작
        AsyncOperation op = SceneManager.LoadSceneAsync("BattleTestScene");
        op.allowSceneActivation = false; // 씬이 다 로드되어도 강제로 시작되지 않게 멱살 잡기!

        // 씬이 90% 이상 로드될 때까지 대기
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // 로드가 끝났으니 씬을 활성화(깨우기) 시킵니다.
        op.allowSceneActivation = true;

        // 씬이 완전히 켜질 때까지 잠깐 대기 (이때 BattleManager의 Awake/Start가 실행됨)
        yield return new WaitUntil(() => op.isDone);

        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeIn(fadeDuration, () =>
            {
                BattleEvents.OnBattleReadyToStart?.Invoke();
            });
        }
        else
        {
            BattleEvents.OnBattleReadyToStart?.Invoke();
        }
    }
}