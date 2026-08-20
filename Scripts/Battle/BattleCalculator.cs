using UnityEngine;
using System.Collections.Generic;

public static class BattleCalculator
{
    // 방어 공식
    private static float GetDefenseMultiplier(float defense)
    {
        if (defense <= 0) return 1.0f;

        return 100f / (100f + defense);
    }

    public static bool GetElementMultiplier(ElementData attackElement, ElementData defendElement)
    {
        // 무속성이거나 방어자의 속성이 없으면 약점 아님
        if (attackElement == null || defendElement == null)
            return false;

        // 방어자의 약점 리스트에 공격 속성이 포함되어 있는지 확인
        return defendElement.weakAgainst.Contains(attackElement);
    }

    // 스킬 데미지 계산
    public static int CalculateDamage(
        CharacterStatus attacker,
        CharacterStatus defender,
        float skillMultiplier,
        DamageFormulaType formulaType,
        ElementData skillElement,
        WeaknessSetting rules)
    {
        float atk = attacker.Attack;
        float def = defender.Defense;

        // 기본 데미지 계산
        float baseDamage = atk * skillMultiplier / 100f;

        if (defender.HasEffect(StatusEffectType.Wet))
        {
            if(skillElement.elementName != null)
            {
                if (skillElement.elementName == "Water") baseDamage *= 1.5f;
                else if (skillElement.elementName == "Fire") baseDamage *= 0.75f;
            }
        }

        // 속성 배율 적용
        float elementMulti = 1.0f;
        List<ElementData> defenderElements = defender.ElementDatas;

        if (defenderElements != null && skillElement != null)
        {
            // 방어자의 ElementData 안에서 직접 배율을 계산해서 출력
            foreach(var e in defenderElements)
            {
                elementMulti *= e.GetMultiplier(skillElement, rules);
            }
        }

        if (defender.CheckWillBreak(skillElement, rules))
        {
            elementMulti = rules.breakMultiplier;
        }

        // 최종 데미지 산출
        float finalDamage = 0f;

        switch (formulaType)
        {
            case DamageFormulaType.FixedDamage:
                finalDamage = skillMultiplier;
                break;

            case DamageFormulaType.Standard:
            default:
                finalDamage = (baseDamage * elementMulti) * GetDefenseMultiplier(def);
                break;
        }

        // 최소 데미지 1 보장
        return Mathf.Max(1, Mathf.RoundToInt(finalDamage));
    }

    public static float CalculateResistance(CharacterStatus defender, ElementData skillElement, WeaknessSetting rule)
    {
        if (skillElement == null) return 1.0f;

        float finalMultiplier = 1f;

        foreach (var defenderElement in defender.ElementDatas)
        {
            if (defenderElement.weakAgainst.Contains(skillElement))
                finalMultiplier *= rule.weakMultiplier;

            if (defenderElement.resistAgainst.Contains(skillElement))
                finalMultiplier *= rule.resistMultiplier;
        }

        return Mathf.Clamp(finalMultiplier, 0.5f, 2.0f);
    }

    // 힐량 계산기
    public static int CalculateHeal(CharacterStatus caster, float healMultiplier)
    {
        // TODO: 기획자가 정확한 힐 공식을 주기 전까지 쓸 임시 공식
        // 예: 시전자의 공격력(또는 마법력) * 스킬의 힐 배율
        int healAmount = Mathf.RoundToInt(caster.characterData.Attack * healMultiplier / 100f);

        // 회복량이 0 이하가 되지 않도록 최소 1 보장
        return Mathf.Max(1, healAmount);
    }
}
