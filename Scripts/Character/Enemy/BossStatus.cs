using System.Collections.Generic;
using UnityEngine;

public class BossStatus : CharacterStatus
{
    [Header("BossPattern")]
    public bool useHpLock = true;
    private bool isHpLocked = false;

    private Queue<int> lockThresholds = new Queue<int>();

    public override void Initialize(CharacterData data, CharacterSaveData saveData = null)
    {
        base.Initialize(data, saveData);

        if (useHpLock)
        {
            EnemyAI myAI = GetComponent<EnemyAI>();
            if (myAI != null)
            {
                foreach (var phase in myAI.phaseList)
                {
                    int thresholdHp = Mathf.RoundToInt(phase.hpThreshold * characterData.MaxHp);
                    lockThresholds.Enqueue(thresholdHp);
                }
            }
        }

    }

    protected override int InterceptFinalDamage(int finalDamage)
    {
        if (isHpLocked)
        {
            Debug.Log("체력 잠금 상태! 데미지 무효화!");
            return 0; // 데미지를 0으로 만들어서 부모에게 돌려보냄!
        }

        // finalDamage는 부모가 속성, 방어력 다 계산해 준 수치
        int expectedHp = CurrentHP + finalDamage;

        // 큐에 남은 잠금선이 있고 예상 체력이 다음 잠금선 이하로 뚫린다면?
        if (lockThresholds.Count > 0 && expectedHp <= lockThresholds.Peek())
        {
            // 다음 잠금선 수치를 뽑아냄 (Dequeue)
            int nextThreshold = lockThresholds.Dequeue();

            // 잠금선까지만 딱 깎이도록 데미지를 재계산 (예: 현재 1000, 잠금선 500이면 -500만 통과시킴)
            int adjustedAmount = nextThreshold - CurrentHP;

            isHpLocked = true;
            BattleEvents.OnHpLocked?.Invoke(this, isHpLocked);
            Debug.LogWarning("보스 체력 잠금 발동! 다음 페이즈로 넘어갑니다!");

            // 재계산된 데미지만 부모에게 돌려줍니다.
            return adjustedAmount;
        }

        // 잠금선에 안 닿았으면 원래 데미지 그대로 깎으라고 돌려줍니다.
        return finalDamage;
    }

    public void UnlockHp()
    {
        isHpLocked = false;
        BattleEvents.OnHpLocked?.Invoke(this, isHpLocked);
        Debug.Log("보스 체력 잠금 해제! 다시 데미지를 입습니다.");
    }

    public override void UpdateSkills()
    {
        foreach (SkillData skill in characterData.allAvailableSkills)
        {
            equippedSkills.Add(skill);
        }
    }

    public void RecoverFromBreak()
    {
        if (elementSystem.isFullyBroken)
        {
            Debug.Log("보스가 브레이크에서 회복하며 새로운 랜덤 약점을 생성합니다!");

            // 다시 랜덤하게 N개의 속성을 부여 (isFullyBroken = false 처리도 포함되어 있음)
            elementSystem.AssignRandomElements();

            // 필요 시 UI 갱신 이벤트 호출
            // BattleEvents.OnBossWeaknessRefreshed?.Invoke(this);
        }
    }
}
