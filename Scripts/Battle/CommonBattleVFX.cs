using UnityEngine;

public enum CommonVFXType
{
    Heal,
    RestoreMP,
    Revive,
    CurseHeal,
    Defend,
    Dodge
}

[CreateAssetMenu(fileName = "CommonBattleVFX", menuName = "Battle/Common VFX Data")]
public class CommonBattleVFX : ScriptableObject
{
    public GameObject defendEffect;
    public GameObject dodgeEffect;
    public GameObject curseHealEffect;
    public GameObject healEffect;
    public GameObject restoreMPEffect;
    public GameObject reviveEffect;
}