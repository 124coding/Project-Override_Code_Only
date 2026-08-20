using DG.Tweening;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FieldManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform defaultSpawnPoint;

    public AudioClip fieldBGM;

    [Tooltip("화면이 밝아지는데 걸리는 시간 (초)")]
    public float fadeDuration = 1.0f;

    public void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.StopPlayer();
        }

        Debug.Log("[FieldManager] 페이드 인 연출 요청!");

        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeIn(fadeDuration, () =>
            {
                Debug.Log("[FieldManager] 페이드 인 완료! 조작 가능.");
                GameStateManager.Instance.ChangeState(GameState.Field);
            });
        }
    }

    private void Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.FadeOutBGM(1.5f);

            SoundManager.Instance.FadeInBGM(fieldBGM, 2.0f);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            player = Instantiate(playerPrefab);
        }

        // 좌표 복귀 로직
        if (DataManager.Instance.isReturningFromBattle)
        {
            // 전투 후 복귀일 때
            player.transform.position = DataManager.Instance.lastPlayerPosition;
            DataManager.Instance.isReturningFromBattle = false;
        }
        else if (DataManager.Instance.isLoadedFromSave)
        {
            // 세이브 불러오기일 때
            player.transform.position = DataManager.Instance.unlockedRestAreaDict[DataManager.Instance.lastRestAreaID].spawnPosition;
            DataManager.Instance.isLoadedFromSave = false;
        }
        else if (!string.IsNullOrEmpty(DataManager.Instance.targetSpawnID))
        {
            // 특정 룸/리스폰 포인트 이동일 때
            RestArea spawnRest = FindObjectsByType<RestArea>(FindObjectsSortMode.None)
                                .FirstOrDefault(r => r.restAreaID == DataManager.Instance.targetSpawnID);
            if (spawnRest != null) player.transform.position = spawnRest.spawnPoint.position;
            DataManager.Instance.targetSpawnID = "";
        }
        else
        {
            player.transform.position = defaultSpawnPoint.position;
        }

        DataManager.Instance.SetPlayerLastPositionAndScene();

    }
}