using UnityEngine;

[CreateAssetMenu(fileName = "NewBattleSettings", menuName = "Battle/Weakness Settings")]
public class WeaknessSetting : ScriptableObject
{
    [Header("Damage Multipliers (수정 가능!)")]
    [Tooltip("BREAK가 터졌을 때, 또는 BREAK 상태일 때 배율")]
    public float breakMultiplier = 1.8f;   // 1.8배

    [Tooltip("일반적인 약점을 찔렀을 때 배율")]
    public float weakMultiplier = 1.25f;   // 1.25배

    [Tooltip("반대 속성(내성)으로 때렸을 때 배율")]
    public float resistMultiplier = 0.75f; // 0.75배

    [Tooltip("아무 상성도 아닐 때 배율")]
    public float defaultMultiplier = 1.0f; // 1.0배
}
