using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "CurrentEncounter", menuName = "Battle/Encounter Data")]
public class EncounterData : ScriptableObject
{
    [Header("EnemyParty")]
    // 필드에서 몬스터와 부딪힐 때마다 덮어씌워질 적 파티 명단
    public List<CharacterData> enemyParty = new List<CharacterData>();
}