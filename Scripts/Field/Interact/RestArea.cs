using UnityEngine;
using UnityEngine.SceneManagement;

public class RestArea : InteractObject
{
    [Header("Rest Area Settings")]
    public string restAreaID = ""; // 이 장소의 고유 ID
    public string restAreaName = "";

    public Transform spawnPoint;

    public Sprite disableSprite;
    public Sprite enableSprite;

    private SpriteRenderer sr;

    private void Awake()
    {
        if(sr == null) sr = GetComponent<SpriteRenderer>();
    }

    protected override void OnInteraction()
    {

        Vector3 targetSpawnPos = spawnPoint != null ? spawnPoint.position : transform.position;

        if (!DataManager.Instance.IsUnlockedRestArea(restAreaID))
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            RestAreaData data = new RestAreaData(restAreaID, restAreaName, currentSceneName, targetSpawnPos);
            DataManager.Instance.UnlockRestArea(data);
            DataManager.Instance.SetObjectState(uniqueID, true);

            sr.sprite = enableSprite;
            sr.color = Color.white;
        }

        DataManager.Instance.SetPlayerLastPositionAndScene();

        DataManager.Instance.lastRestAreaID = this.restAreaID;

        DataManager.Instance.SaveGame();
        Debug.Log("게임 자동 저장 완료!");

        // 휴식 UI 메뉴 띄우기 (스킬 트리, 텔레포트 등)
        OpenRestAreaMenu();
    }

    protected override void LoadState(bool isActivated)
    {
        if (isActivated) {
            sr.sprite = enableSprite;
            sr.color = Color.white;
        }
    }

    private void HealParty()
    {
        // 파티원 전원 체력/마나 풀 회복 로직
        DataManager.Instance.FullHealParty();

        DataManager.Instance.ResetRespawnableMonsters();
    }

    private void QuitRestArea()
    {
        GameEvents.OnClickedRestButton -= HealParty;
        GameEvents.OnClickedTeleportButton -= OnClickFastTravelButton;
        GameEvents.OnClickedRestAreaQuitButton -= QuitRestArea;
    }

    private void OpenRestAreaMenu()
    {
        GameStateManager.Instance.TogglePause();
        GameEvents.OnEnableRestArea?.Invoke(this);

        GameEvents.OnClickedRestButton -= HealParty;
        GameEvents.OnClickedTeleportButton -= OnClickFastTravelButton;
        GameEvents.OnClickedRestAreaQuitButton -= QuitRestArea;

        GameEvents.OnClickedRestButton += HealParty;
        GameEvents.OnClickedTeleportButton += OnClickFastTravelButton;
        GameEvents.OnClickedRestAreaQuitButton += QuitRestArea;
    }

    public void OnClickFastTravelButton(string targetScene, Vector3 targetPos)
    {
        TeleportManager.Instance.TeleportPlayer(targetScene, targetPos);
    }
}
