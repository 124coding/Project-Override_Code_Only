using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewElement", menuName = "Battle/Element Data")]
public class ElementData : ScriptableObject
{
    [Header("UI & Visuals")]
    public string elementName;       // 속성 이름 (예: "화염")
    public Sprite elementIcon;       // 속성 아이콘 (UI에 띄울 이미지)
    public Color elementColor = Color.white; // 속성 고유 색상 (예: Red)

    [Header("Relations (상성표)")]
    [Tooltip("이 속성들에게 맞으면 약점이 찔립니다")]
    public List<ElementData> weakAgainst;

    [Tooltip("이 속성들에게 맞으면 데미지가 감소합니다")]
    public List<ElementData> resistAgainst;

    public float GetMultiplier(ElementData attackElement, WeaknessSetting rules)
    {
        if (attackElement == null) return rules.defaultMultiplier;

        // 약점 리스트에 공격 속성이 들어있다면?
        if (weakAgainst.Contains(attackElement)) return rules.weakMultiplier;

        // 내성 리스트에 공격 속성이 들어있다면?
        if (resistAgainst.Contains(attackElement)) return rules.resistMultiplier;

        return rules.defaultMultiplier;
    }
}