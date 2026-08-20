using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EffectData
{
    public StatusEffectType effectType;
    public float amount;    // 변화량 (버프면 양수, 디버프면 음수)
    public int remainingTurns;   // 남은 턴 수
    public EffectModifierType modifierType;

    public Sprite EffectIcon;
    public string displayName;     // UI에 표시할 한국어 이름
    public string description;     // UI 툴팁에 표시할 설명

    public bool isJustApplied;

    // 생성자
    public EffectData(StatusEffectType type, float amount, int turns, EffectModifierType modifierType)
    {
        effectType = type;
        this.amount = amount;
        remainingTurns = turns;
        this.modifierType = modifierType;
        this.isJustApplied = true;

        if (EffectIconDatabase.Instance != null) {
            var mapping = EffectIconDatabase.Instance.GetEffectMapping(type);

            this.displayName = mapping.displayName;

            // 영구 지속(-1)인 경우 영구용 데이터 할당
            if (turns == -1)
            {
                // 영구 아이콘이 있으면 쓰고, 없으면 기본 아이콘
                this.EffectIcon = (mapping.permanentIcon != null) ? mapping.permanentIcon : mapping.icon;
            }
            else
            {
                this.EffectIcon = mapping.icon;
                this.description = mapping.description;
            }
        }
    }
}

public class EffectSystem : MonoBehaviour
{
    public CharacterStatus characterStatus;

    // 몸에 걸린 모든 버프/디버프를 관리
    [SerializeField] public List<EffectData> activeEffects = new List<EffectData>();

    public int CalculateModifiedStat(int baseValue, StatusEffectType upEffect, StatusEffectType downEffect, int minValue = 0)
    {
        if (characterStatus != null && characterStatus.CurrentHP <= 0 && upEffect == StatusEffectType.SpeedUp) return 0;

        int flatBonus = 0;
        float percentMultiplier = 1.0f; // 1.0 = 100% (기본 배율)

        foreach (var effect in activeEffects)
        {
            // 영구 버프 (Flat) -> amount 수치를 더하거나 뺌 
            if (effect.modifierType == EffectModifierType.Flat)
            {
                if (effect.effectType == upEffect) flatBonus += (int)effect.amount;
                else if (effect.effectType == downEffect) flatBonus -= (int)effect.amount;
            }
            // 턴제 버프 (Percent) -> 기획된 고정 배율을 곱함 (amount 무시)
            else if (effect.modifierType == EffectModifierType.Percent)
            {
                if (effect.effectType == upEffect) percentMultiplier *= GetFixedMultiplier(upEffect);
                else if (effect.effectType == downEffect) percentMultiplier *= GetFixedMultiplier(downEffect);
            }
        }

        // 최종 계산: (기본값 + 영구 고정 증가량) * 턴제 고정 배율
        int finalStat = Mathf.RoundToInt((baseValue + flatBonus) * percentMultiplier);

        return Mathf.Max(minValue, finalStat);
    }

    private float GetFixedMultiplier(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.AtkUp: return 1.5f;     // 공격력 1.5배
            case StatusEffectType.AtkDown: return 0.75f;  // 공격력 0.75배
            case StatusEffectType.DefUp: return 1.25f;     // 방어력 1.25배
            case StatusEffectType.DefDown: return 0.5f;   // 방어력 0.5배
            case StatusEffectType.SpeedUp: return 1.3f;   // 속도 1.3배
            case StatusEffectType.SpeedDown: return 0.7f; // 속도 0.7배
            default: return 1.0f; // 해당 없으면 영향 없음
        }
    }



    public bool IsStunned => HasStatusEffect(StatusEffectType.Electrocute) || HasStatusEffect(StatusEffectType.Nightmare);

    public void Initialize(CharacterStatus myNewStatus)
    {
        this.characterStatus = myNewStatus;
    }

    public void ActiveEffectsClear()
    {
        activeEffects.Clear();
    }

    public void RemoveEffect(EffectData effect)
    {
        activeEffects.RemoveAll(e => e.effectType == effect.effectType && e.modifierType == effect.modifierType);
    }

    public EffectData GetEffect(EffectData effect) {
        return activeEffects.Find(e => e.effectType == effect.effectType && e.modifierType == effect.modifierType);
    }

    public void AddStatEffect(StatusEffectType type, float amount, int turns, EffectModifierType modifierType)
    {
        // 부식 상태일 때 방어력 증가 버프 불가
        if (type == StatusEffectType.DefUp && HasStatusEffect(StatusEffectType.Corrosion))
        {
            Debug.Log($"[Corrosion] 부식 상태이므로 방어력 증가를 획득할 수 없습니다!");
            return; // 버프 무시
        }

        // 부식이 걸릴 때, 기존 방어력 증가 버프 삭제
        if (type == StatusEffectType.Corrosion)
        {
            activeEffects.RemoveAll(e => e.effectType == StatusEffectType.DefUp && e.modifierType == EffectModifierType.Percent);
            Debug.Log($"[Corrosion] 녹이 슬었습니다! 기존 방어력 증가(턴제) 버프가 삭제됩니다.");
        }

        if (turns == -1)
        {
            StatusEffectType positiveType = type;
            StatusEffectType negativeType = type;
            bool isStatEffect = true;
            bool isPositive = true;

            // 짝꿍(반대되는) 상태이상이 무엇인지 매핑합니다.
            switch (type)
            {
                case StatusEffectType.AtkUp: positiveType = StatusEffectType.AtkUp; negativeType = StatusEffectType.AtkDown; isPositive = true; break;
                case StatusEffectType.AtkDown: positiveType = StatusEffectType.AtkUp; negativeType = StatusEffectType.AtkDown; isPositive = false; break;
                case StatusEffectType.DefUp: positiveType = StatusEffectType.DefUp; negativeType = StatusEffectType.DefDown; isPositive = true; break;
                case StatusEffectType.DefDown: positiveType = StatusEffectType.DefUp; negativeType = StatusEffectType.DefDown; isPositive = false; break;
                case StatusEffectType.SpeedUp: positiveType = StatusEffectType.SpeedUp; negativeType = StatusEffectType.SpeedDown; isPositive = true; break;
                case StatusEffectType.SpeedDown: positiveType = StatusEffectType.SpeedUp; negativeType = StatusEffectType.SpeedDown; isPositive = false; break;
                case StatusEffectType.ResUp: positiveType = StatusEffectType.ResUp; negativeType = StatusEffectType.ResDown; isPositive = true; break;
                case StatusEffectType.ResDown: positiveType = StatusEffectType.ResUp; negativeType = StatusEffectType.ResDown; isPositive = false; break;
                default: isStatEffect = false; break; // 스탯 증감이 아니면 아래의 일반 로직으로 넘김
            }

            if (isStatEffect)
            {
                // 현재 걸려있는 영구 짝꿍 버프/디버프를 찾음
                var existingPos = activeEffects.Find(e => e.effectType == positiveType && e.remainingTurns == -1 && e.modifierType == modifierType);
                var existingNeg = activeEffects.Find(e => e.effectType == negativeType && e.remainingTurns == -1 && e.modifierType == modifierType);

                // 기존 합산 수치 계산
                float currentNet = 0f;
                if (existingPos != null) currentNet += existingPos.amount;
                if (existingNeg != null) currentNet -= existingNeg.amount;

                // 새로 들어온 값 적용
                if (isPositive) currentNet += amount;
                else currentNet -= amount;

                // 기존 영구 이펙트 일단 전부 삭제
                if (existingPos != null) activeEffects.Remove(existingPos);
                if (existingNeg != null) activeEffects.Remove(existingNeg);

                // 최종 계산된 값이 양수면 Up, 음수면 Down으로 하나만 새로 추가 (0이면 아무것도 안함 = 완벽히 상쇄됨)
                if (currentNet > 0)
                {
                    EffectData newData = new EffectData(positiveType, currentNet, -1, modifierType);
                    activeEffects.Add(newData);
                }
                else if (currentNet < 0)
                {
                    EffectData newData = new EffectData(negativeType, Mathf.Abs(currentNet), -1, modifierType);
                    activeEffects.Add(newData);
                }

                // 상쇄 로직을 탔으므로 이벤트 호출 후 함수 종료!
                BattleEvents.OnEffectsChanged?.Invoke(characterStatus);
                return;
            }
        }
        // =========================================================================

        // 기존 중복 적용 로직 (상쇄되지 않는 영구 버프 및 턴제 버프)
        var existingEffect = activeEffects.Find(e => e.effectType == type && e.modifierType == modifierType);

        if (existingEffect != null)
        {
            if (existingEffect.remainingTurns == -1)
            {
                // [영구 버프] 수치(Amount)를 누적해서 합산함 (위의 switch문에 등록되지 않은 영구 상태이상들)
                existingEffect.amount += amount;
                Debug.Log($"[{type}] 영구 버프 중첩! 수치가 {amount}만큼 증가하여 총 {existingEffect.amount}이 되었습니다.");
            }
            else
            {
                // [턴제 상태이상] 턴(Turns) 수를 연장
                existingEffect.remainingTurns += turns;
                Debug.Log($"[{type}] 턴제 효과 갱신! 턴 수가 {existingEffect.remainingTurns}턴으로 늘어났습니다.");
                existingEffect.isJustApplied = true;
            }
        }
        else
        {
            EffectData newData = new EffectData(type, amount, turns, modifierType);
            activeEffects.Add(newData);
            Debug.Log($"[{type}] 새로운 효과 추가됨! (수치: {amount}, 턴: {turns})");
        }

        // UI 및 스탯 갱신 이벤트 호출 (버프 아이콘 업데이트, 최종 스탯 재계산 등)
        BattleEvents.OnEffectsChanged?.Invoke(characterStatus);
    }

    public void TickEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].remainingTurns == -1)
            {
                continue;
            }

            if (activeEffects[i].isJustApplied)
            {
                activeEffects[i].isJustApplied = false;
                continue;
            }

            activeEffects[i].remainingTurns--;

            if (activeEffects[i].remainingTurns <= 0)
            {
                if (activeEffects[i].effectType == StatusEffectType.Electrocute)
                {
                    characterStatus.currentShockGauge = 0;
                }
                activeEffects.RemoveAt(i);
            }
        }

        BattleEvents.OnEffectsChanged?.Invoke(characterStatus);
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.effectType == type) return true;
        }
        return false;
    }

    public EffectData GetStatusEffect(StatusEffectType type)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.effectType == type)
            {
                return effect;
            }
        }

        return null;
    }


    public void RemoveStatusEffect(StatusEffectType type)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.effectType == type)
            {
                activeEffects.Remove(effect);
                return;
            }
        }

        BattleEvents.OnEffectsChanged?.Invoke(characterStatus);
        BattleEvents.OnTimelineUpdateRequested?.Invoke();
    }

    public bool TryConsumeShield()
    {
        // 'Shield' 타입의 효과를 찾습니다.
        EffectData shieldEffect = activeEffects.Find(e => e.effectType == StatusEffectType.Shield);

        if (shieldEffect != null)
        {
            // 방어막을 하나 소모
            shieldEffect.amount -= 1;

            // 방어막이 없다면 삭제
            if(shieldEffect.amount <= 0)
            {
                activeEffects.Remove(shieldEffect);
            }

            // UI 갱신 알림
            BattleEvents.OnEffectsChanged?.Invoke(characterStatus);

            Debug.Log($"{gameObject.name}이 방어막으로 공격을 막았습니다!");
            return true; // 방어 성공
        }

        return false; // 방어막 없음
    }

    public void CleanseDebuffs()
    {
        // 해로운 디버프 타겟팅 조건 정의
        // 스탯 감소(Down), 감전, 실명, 화상, 젖음, 저주, 악몽, 부식 등 모든 부정적 효과 제거
        activeEffects.RemoveAll(e =>
            e.effectType == StatusEffectType.AtkDown ||
            e.effectType == StatusEffectType.DefDown ||
            e.effectType == StatusEffectType.SpeedDown ||
            e.effectType == StatusEffectType.ResDown ||
            e.effectType == StatusEffectType.Electrocute ||
            e.effectType == StatusEffectType.Blind ||
            e.effectType == StatusEffectType.Burn ||
            e.effectType == StatusEffectType.Wet ||
            e.effectType == StatusEffectType.Curse ||
            e.effectType == StatusEffectType.Nightmare ||
            e.effectType == StatusEffectType.Corrosion
        );

        Debug.Log($"[{gameObject.name}]의 모든 해로운 상태이상 및 디버프가 정화되었습니다!");

        // 상태가 변경되었음을 UI와 타임라인 매니저에 알림
        BattleEvents.OnEffectsChanged?.Invoke(characterStatus);
        BattleEvents.OnTimelineUpdateRequested?.Invoke();
    }

    public void CleanseBuffs()
    {
        // 해로운 버프 타겟팅 조건 정의
        activeEffects.RemoveAll(e =>
            e.effectType == StatusEffectType.AtkUp ||
            e.effectType == StatusEffectType.DefUp ||
            e.effectType == StatusEffectType.SpeedUp ||
            e.effectType == StatusEffectType.ResUp ||
            e.effectType == StatusEffectType.ExtraHitChance ||
            e.effectType == StatusEffectType.Taunt ||
            e.effectType == StatusEffectType.DamageShare ||
            e.effectType == StatusEffectType.Thorn ||
            e.effectType == StatusEffectType.Shield ||
            e.effectType == StatusEffectType.Evasion
        );

        Debug.Log($"[{gameObject.name}]의 모든 이로운 버프가 삭제되었습니다!");

        // 상태가 변경되었음을 UI와 타임라인 매니저에 알림
        BattleEvents.OnEffectsChanged?.Invoke(characterStatus);
        BattleEvents.OnTimelineUpdateRequested?.Invoke();
    }

    public int ConsumeStatusEffect(StatusEffectType type)
    {
        // 해당 상태 이상 찾기
        EffectData targetEffect = activeEffects.Find(e => e.effectType == type);

        if (targetEffect != null)
        {
            int remainingTurns = targetEffect.remainingTurns;

            // 상태 이상 제거
            activeEffects.Remove(targetEffect);

            // UI 갱신 알림
            BattleEvents.OnEffectsChanged?.Invoke(characterStatus);

            return remainingTurns; // 제거한 턴 수 반환
        }

        return 0; // 없으면 0 반환
    }

    public bool ProcessTurnStartEffects()
    {
        if (characterStatus.CurrentHP <= 0) return true; // 이미 시체면 무시

        int totalDotDamage = 0;

        // activeEffects는 이전 턴에 EffectData 타입으로 들어간 상태이상 리스트입니다.
        foreach (var effect in activeEffects)
        {
            if (effect.effectType == StatusEffectType.Burn)
            {
                int calculatedDamage = Mathf.Max(1, Mathf.RoundToInt(characterStatus.MaxHp * 0.05f));

                totalDotDamage += calculatedDamage;
            }
        }

        // 피해량 처리
        if (totalDotDamage > 0)
        {
            characterStatus.ApplyHpChange(-totalDotDamage, null, null);
        }

        // 도트 데미지를 입고 죽었는지 여부 반환
        return characterStatus.CurrentHP <= 0;
    }
}
