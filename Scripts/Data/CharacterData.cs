using System.Collections.Generic;
using UnityEngine;

// 개별 아이템의 드랍 정보를 담는 클래스
[System.Serializable]
public class DropItemData
{
    public string itemID;

    [Range(0f, 1f)]
    [Tooltip("1.0 = 100% 확정 드랍, 0.3 = 30% 확률 드랍")]
    public float dropChance = 1.0f;

    [Tooltip("최소 드랍 수량")]
    public int minQuantity = 1;

    [Tooltip("최대 드랍 수량")]
    public int maxQuantity = 1;

    [Tooltip("이 몬스터가 몇 레벨 이상일 때부터 이 아이템을 떨굴 것인가?")]
    public int minLevelRequired = 1;

    [Tooltip("체크 시, 여러 몬스터가 드랍하더라도 이번 전투에서 최대 1개만 획득됩니다.")]
    public bool isUniqueItem = false;

    public DropItemData() { }

    public DropItemData(string id, float chance, int minQty = 1, int maxQty = 1, int reqLevel = 1)
    {
        itemID = id;
        dropChance = chance;
        minQuantity = minQty;
        maxQuantity = maxQty;
        minLevelRequired = reqLevel;
    }
}

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Battle/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Visuals")]
    public Sprite characterIcon;
    public Sprite characterMiniIcon;
    public GameObject visualModelPrefab;

    [Header("AI Phases & Patterns")]
    public List<EnemyPhase> phaseList = new List<EnemyPhase>();
    public List<SpecialPattern> specialPatterns = new List<SpecialPattern>();

    public enum CharacterSize { Small = 1, Medium = 2, Large = 3}
    public CharacterSize mySize = CharacterSize.Medium;

    [Header("Player Only")]
    [SerializeField] private int aggroLevel;

    [Header("Base stats")]
    [SerializeField] private int baseLevel = 1;
    [SerializeField] private string characterName;
    [SerializeField] private int maxHp;
    [SerializeField] private int maxMp;
    [SerializeField] private int attack;
    [SerializeField] private int defense;
    [SerializeField] private int speed;
    [SerializeField] private int effectResistance;
    [SerializeField] private bool isPlayer;
    [SerializeField] private int elementCount = 1;
    [SerializeField] private bool elementSkillSet = true;
    [SerializeField] private Sprite basicSprite;
    [SerializeField] private Sprite deathSprite;

    [Tooltip("이 캐릭터가 레벨업 할 때 오르는 고유 스탯량")]
    [SerializeField] public int hpGrowth = 10;
    [SerializeField] public int attackGrowth = 3;
    [SerializeField] public int defenseGrowth = 2;
    
    [Header("Basic Action")]
    [Tooltip("이 캐릭터의 평타")]
    public SkillData basicAttackData;

    [Header("Skills")]
    [Tooltip("이 몬스터(또는 초기 플레이어)가 기본적으로 가질 스킬들")]
    public List<SkillData> defaultSkills = new List<SkillData>();

    [Header("EnemyOnly")]
    [Tooltip("이 몬스터가 가질 스킬 갯수")]
    public int maxSkillEquipCount = 4;
    public bool isBoss = false;

    [Header("Reward Settings (Enemy Only)")]
    [Tooltip("이 몬스터(1레벨) 처치 시 기본으로 주는 경험치")]
    [SerializeField] private int baseRewardExp = 2;

    [Tooltip("몬스터의 레벨이 1 오를 때마다 추가로 주는 경험치")]
    [SerializeField] private float expGrowth = 1;

    [Tooltip("이 몬스터가 드랍할 수 있는 아이템 목록")]
    public List<DropItemData> dropTable = new List<DropItemData>()
    {
        new DropItemData("item_01", 0.2f, 1, 1, 1),
        new DropItemData("item_02", 0.2f, 1, 1, 1),
        new DropItemData("item_03", 0.1f, 1, 1, 1)
    };

    [Tooltip("이 몬스터가 가질 수 있는 속성")]
    public List<ElementData> allAvailableElement = new List<ElementData>() { };

    [Tooltip("이 몬스터가 가진 속성별 스킬 풀")]
    public List<SkillData> allAvailableSkills = new List<SkillData>();

    public AIPersonalityProfile aiProfile;

    public int AggroLevel => aggroLevel;

    public int BaseLevel => baseLevel;
    public string CharacterName => characterName;
    public int MaxHp => maxHp;
    public int MaxMp => maxMp;
    public int Attack => attack;
    public int Defense => defense;
    public int Speed => speed;
    public int EffectResistance => effectResistance;
    public bool IsPlayer => isPlayer;

    public bool ElementSkillSet => elementSkillSet;
    public int ElementCount => elementCount;
    public Sprite BasicSprite => basicSprite;
    public Sprite DeathSprite => deathSprite;

    public int GetHpAtLevel(int level) => maxHp + (hpGrowth * (level - 1));
    public int GetAttackAtLevel(int level) => attack + (attackGrowth * (level - 1));
    public int GetDefenseAtLevel(int level) => defense + (defenseGrowth * (level - 1));

    public int GetRewardExpAtLevel(int level) => baseRewardExp + (int)(expGrowth * (Mathf.Max(1, level) - 1));

    public List<DropItemData> GetAvailableDropsAtLevel(int level)
    {
        List<DropItemData> availableDrops = new List<DropItemData>();
        foreach (var drop in dropTable)
        {
            // 몬스터의 현재 레벨이 아이템 드랍 요구 레벨 이상일 때만 추가
            if (level >= drop.minLevelRequired)
            {
                availableDrops.Add(drop);
            }
        }
        return availableDrops;
    }
}
