using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 스터(또는 적 무리)가 들고 있을 보상 보따리
[System.Serializable]
public class EncounterReward
{
    public int totalEXP = 0;

    // 단순 string 리스트에서 DropItemData 리스트로 변경!
    public List<DropItemData> dropItems = new List<DropItemData>();
}

public class FieldMonster : MonoBehaviour
{
    [Header("Reward Settings")]
    [Tooltip("이 적 무리를 처치했을 때 얻을 총 보상")]
    public EncounterReward encounterReward;

    [Header("Unique Settings")]
    public string monsterID;

    [Header("Respawn Settings")]
    public bool isRespawnable = true;

    [Header("BattleEnvironment Setting")]
    public BattleStageData myStageData;

    [Header("EncounterDirecting")]
    public DirectingType directingType = DirectingType.NormalRunIn;

    [Header("EnemyParty")]
    // 필드에서 몬스터와 부딪힐 때마다 덮어씌워질 적 파티 명단
    public List<CharacterData> enemyParty = new List<CharacterData>();
    public int enemyLevel = 1;

    private void OnEnable()
    {
        if (DataManager.Instance != null)
        {
            if ((DataManager.Instance.tempDefeatedIDs.Contains(monsterID) && isRespawnable) ||
                DataManager.Instance.permanentDefeatedIDs.Contains(monsterID))
            {
                gameObject.SetActive(false);
                Debug.Log("해당 적 삭제");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gameObject.SetActive(false);
            // 플레이어의 현재 위치(좌표)를 넘겨줍니다.
            TriggerBattle(EncounterType.Normal, other.transform.position);
        }
    }

    // 플레이어의 무기나 투사체에 맞았을 때 호출될 함수 (매개변수로 플레이어 위치 전달 필요)
    public void OnHitByPlayerWeapon(Vector3 playerPosition)
    {
        TriggerBattle(EncounterType.PlayerAdvantage, playerPosition);
    }

    // 플레이어가 패링에 성공했을 때 호출될 함수
    public void OnParriedByPlayer(Vector3 playerPosition)
    {
        TriggerBattle(EncounterType.Parried, playerPosition);
    }

    private void TriggerBattle(EncounterType type, Vector3 playerPosition)
    {
        Debug.Log($"[필드 몬스터] 플레이어와 조우! (타입: {type})");

        if (DataManager.Instance != null)
        {
            DataManager.Instance.currentEncounterReward = this.encounterReward;
        }

        EncounterManager.Instance.TriggerEncounter(
            enemyParty,
            enemyLevel,
            type,
            directingType,
            monsterID,
            isRespawnable,
            myStageData
        );
    }

    private void OnValidate()
    {
        CalculateTotalRewards();
    }

    [ContextMenu("RewardAutoFill")]
    private void CalculateTotalRewards()
    {
        if (enemyParty == null || enemyParty.Count == 0) return;

        if (encounterReward == null) encounterReward = new EncounterReward();

        int totalExp = 0;
        List<DropItemData> combinedDropTable = new List<DropItemData>();

        // 유니크 아이템이 중복해서 들어가는 것을 막기 위한 기록장
        HashSet<string> uniqueItemTracker = new HashSet<string>();

        foreach (var enemy in enemyParty)
        {
            if (enemy != null)
            {
                // 경험치 합산
                totalExp += enemy.GetRewardExpAtLevel(enemyLevel);

                // 해당 적의 드랍 테이블 가져오기
                List<DropItemData> drops = enemy.GetAvailableDropsAtLevel(enemyLevel);

                foreach (var drop in drops)
                {
                    // 유니크 아이템일 경우 검사 로직
                    if (drop.isUniqueItem)
                    {
                        // 이미 기록장에 있는 아이템이라면 추가하지 않고 넘어감(패스)
                        if (uniqueItemTracker.Contains(drop.itemID))
                        {
                            continue;
                        }
                        else
                        {
                            // 처음 보는 유니크 아이템이라면 기록장에 적어둠
                            uniqueItemTracker.Add(drop.itemID);
                        }
                    }

                    // 리스트에 추가 (유니크가 아니거나, 처음 등장한 유니크 아이템인 경우)
                    combinedDropTable.Add(drop);
                }
            }
        }

        // 합산된 데이터를 보따리에 넣기
        encounterReward.totalEXP = totalExp;
        encounterReward.dropItems = combinedDropTable;
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateUniqueID()
    {
        monsterID = System.Guid.NewGuid().ToString();
    }
}