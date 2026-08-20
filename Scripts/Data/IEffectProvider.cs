using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EffectGroup
{
    [Tooltip("이 그룹 안의 효과들이 발동할 확률 (0~100)")]
    public float groupApplyChance = 100f;

    [Tooltip("속성 저항 무시")]
    public bool ignoreResistance;

    [Tooltip("확정 타격")]
    public bool undodgeableAttack;

    [Tooltip("확률에 당첨되면 일괄 적용될 효과들")]
    public List<EffectPayload> payloads = new List<EffectPayload>();
}

public interface IEffectProvider
{
    int GetPostActionGauge();

    List<EffectGroup> GetEffects();
}
