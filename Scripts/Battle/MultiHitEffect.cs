using System.Collections.Generic;
using UnityEngine;

public class MultiHitEffect : MonoBehaviour
{
    private CharacterAction owner;
    private List<CharacterStatus> myTargets; // 이 이펙트가 공격할 타겟들!
    private SkillData skill;

    public void Setup(CharacterAction owner, List<CharacterStatus> targets, SkillData skill)
    {
        this.owner = owner;
        this.myTargets = targets;
        this.skill = skill;
    }

    public void OnHitTriggered(int stepIndex)
    {
        Debug.Log("OnHitTriggered");
        owner?.TriggerDamage(stepIndex, myTargets, skill);
    }
}