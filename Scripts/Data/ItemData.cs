using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Battle/ItemData")]
public class ItemData : ScriptableObject, IEffectProvider
{
    [Header("아이템 기본 정보")]
    public string itemID;      // 아이템 ID
    public string itemName;     // 아이템 이름 (예: 하급 포션)
    public int postActionGaugeDelay; // 액션 게이지
    public bool isAoE;

    public int GetPostActionGauge() => postActionGaugeDelay;

    public ElementData element;
    [TextArea]
    public string description;  // 아이템 설명
    public Sprite itemIcon;     // UI에 표시될 아이콘

    [Header("TargettingSet")]
    // 이 스킬을 쓸 때 누구 머리 위에 화살표를 켤 것인가?
    public TargetType validTargetGroup;

    public GameObject hitEffectPrefab;

    // 이 스킬이 발동될 때 순차적으로 터질 '효과 리스트' (바구니)
    public List<EffectGroup> itemEffects = new List<EffectGroup>();

    public List<EffectGroup> GetEffects() => itemEffects;
}
