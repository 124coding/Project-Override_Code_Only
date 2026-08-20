using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EffectIconDatabase", menuName = "Battle/Effect Icon Database")]
public class EffectIconDatabase : ScriptableObject
{
    [System.Serializable]
    public struct EffectIconMapping
    {
        public StatusEffectType effectType;

        [Tooltip("일반적인 턴제 버프/디버프 아이콘")]
        public Sprite icon;

        [Tooltip("영구 지속(턴 수 -1)일 때 사용할 아이콘 (비워두면 기본 아이콘 사용)")]
        public Sprite permanentIcon;

        [Header("Text Settings")]
        [Tooltip("UI에 표시할 이름")]
        public string displayName;

        [Tooltip("일반 턴제 버프 설명 (예: {0}%만큼 공격력이 상승합니다.)")]
        [TextArea(2, 4)]
        public string description;

    }

    public static EffectIconDatabase Instance;

    private void OnEnable()
    {
        Instance = this;
    }

    public List<EffectIconMapping> iconMappings;

    // Value를 Sprite 하나만 저장하는 것이 아니라, Mapping 구조체 전체를 저장하도록 변경
    private Dictionary<StatusEffectType, EffectIconMapping> iconDict;

    // 매개변수로 turns를 추가로 받습니다. 기본값은 0으로 두어 에러를 방지합니다.
    public EffectIconMapping GetEffectMapping(StatusEffectType type)
    {
        if (iconDict == null)
        {
            iconDict = new Dictionary<StatusEffectType, EffectIconMapping>();
            foreach (var m in iconMappings)
            {
                iconDict[m.effectType] = m;
            }
        }

        if (iconDict.TryGetValue(type, out var mapping))
        {
            return mapping;
        }

        return default;
    }
}