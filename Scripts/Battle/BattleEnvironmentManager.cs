using UnityEngine;

public class BattleEnvironmentManager : MonoBehaviour
{
    public Transform environmentRoot; // 배경이 생성될 부모 위치 (0,0,0)

    private void Start()
    {
        // DataManager에 저장되어 있는 현재 스테이지 데이터를 쏙 빼옵니다.
        BattleStageData stageData = DataManager.Instance.currentStageData;

        if (stageData != null && stageData.backgroundPrefab != null)
        {
            // 배경 프리팹 동적 생성!
            Instantiate(stageData.backgroundPrefab, environmentRoot.position, Quaternion.identity, environmentRoot);

            // TODO: BGM이나 환경 조명 색상까지 세팅 가능
            // SoundManager.Instance.PlayBGM(stageData.battleBGM);
            // RenderSettings.ambientLight = stageData.ambientLightColor;
        }
        else
        {
            Debug.LogWarning("[BattleEnvironmentManager] DataManager에 세팅된 배경 데이터가 없습니다!");
        }
    }
}