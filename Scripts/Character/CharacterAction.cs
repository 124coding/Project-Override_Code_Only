using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterAction : MonoBehaviour
{
    [SerializeField] private CommonBattleVFX commonVFX;

    private BattleLogicHandler logic;
    private ActionVisualizer visualizer;

    private Vector3 myStartPos;

    private Dictionary<EffectGroup, List<CharacterStatus>> currentSkillSnapshot;

    public void Awake()
    {
        this.logic = GetComponent<BattleLogicHandler>();
        this.visualizer = GetComponent<ActionVisualizer>();
        this.visualizer.commonVFX = commonVFX;
    }

    public void SetCombatants(List<ITurnEntity> turnCombatants)
    {
        logic.SetCombatants(turnCombatants);
    }

    // 액션 호출 코루틴
    private IEnumerator ActionRoutine(CharacterStatus target, bool isRanged, SkillData skill)
    {
        myStartPos = transform.position;

        float originalScaleX = transform.localScale.x;

        logic.ProcessCost(skill.mpCost, skill.postActionGaugeDelay);

        // 스킬의 모든 페이로드 수집 (기본 효과 + 단계별 효과)
        List<EffectGroup> allPayloads = new List<EffectGroup>();
        if (skill.skillSteps != null)
        {
            foreach (var step in skill.skillSteps)
                if (step.stepEffects != null) allPayloads.AddRange(step.stepEffects);
        }

        currentSkillSnapshot = logic.CreateTargetSnapshot(target, allPayloads);

        HashSet<CharacterStatus> uniqueVisualTargets = new HashSet<CharacterStatus>();
        foreach (var targets in currentSkillSnapshot.Values)
        {
            foreach (var t in targets) uniqueVisualTargets.Add(t);
        }
        List<CharacterStatus> targetsToHit = uniqueVisualTargets.ToList();

        bool amIPlayer = this.GetComponent<CharacterStatus>().IsPlayer;
        List<CharacterStatus> allyTargets = targetsToHit.Where(t => t.IsPlayer == amIPlayer).ToList();
        List<CharacterStatus> enemyTargets = targetsToHit.Where(t => t.IsPlayer != amIPlayer).ToList();

        Vector3 allTargetCenter = GetTargetCenter(targetsToHit, target, skill.isAoE);
        Vector3 allyTargetCenter = GetTargetCenter(allyTargets, target, skill.isAoE);
        Vector3 enemyTargetCenter = GetTargetCenter(enemyTargets, target, skill.isAoE);

        // 크기 보정 계산
        float targetSizeFactor = 1.0f;
        if (!skill.isAoE && targetsToHit.Count > 0)
        {
            targetSizeFactor = (float)targetsToHit[0].characterData.mySize;
        }
        else if (skill.isAoE)
        {
            targetSizeFactor = (float)CharacterData.CharacterSize.Large;
        }
        float finalEffectScale = skill.effectScale * targetSizeFactor;

        bool shouldMove = !isRanged && enemyTargets.Count > 0;

        // 근접 공격이면 적 앞으로 이동
        if (shouldMove)
        {
            visualizer.PlayAnimation("Run");
            float offsetDistance = skill.isAoE ? 3.0f : 1.5f;

            // 이동할 타겟은 무조건 적군이므로 enemyTargets 기준 진영 판별
            bool isEnemyPlayerSide = enemyTargets[0].IsPlayer;
            Vector3 targetPos = isEnemyPlayerSide ?
                enemyTargetCenter + new Vector3(offsetDistance, 0f, 0f) :
                enemyTargetCenter + new Vector3(-offsetDistance, 0f, 0f);

            yield return StartCoroutine(visualizer.MoveTo(targetPos, 0.3f));
        }

        // 시전 이펙트 (마법진 등 시전자 발밑)
        if (skill.castEffectPrefab != null)
            visualizer.PlayEffect(skill.castEffectPrefab, transform.position, skill.castEffectScale);

        if (!string.IsNullOrEmpty(skill.prepAnimName))
        {
            yield return StartCoroutine(visualizer.PlayAnimationAndHoldLastFrame(skill.prepAnimName));
        }

        if (skill.castDelay > 0f)
        {
            yield return new WaitForSeconds(skill.castDelay);
        }

        // 시전 애니메이션 재생
        visualizer.PlayAnimation(skill.animName, skill.animColor);

        // 이펙트 생성 위치 보정
        Vector3 spawnPos = transform.position + Vector3.up * 1.0f;

        Vector3 hitCenter;
        if (skill.validTargetGroup == TargetType.Enemy)
        {
            hitCenter = enemyTargetCenter + Vector3.up * 1.0f;
        }
        else if (skill.validTargetGroup == TargetType.All)
        {
            hitCenter = allTargetCenter + Vector3.up * 1.0f;
        }
        else
        {
            hitCenter = allyTargetCenter + Vector3.up * 1.0f;
        }

        List<EffectGroup> effectsToApply = new List<EffectGroup>();

        if (skill.laserPrefab != null)
        {
            if (skill.laserPrefab != null)
            {
                yield return new WaitForSeconds(0.5f);

                GameObject laserObj = visualizer.PlayLaser(skill.laserPrefab, spawnPos, hitCenter, finalEffectScale, skill.laserOvershoot);

                // 데미지 타이밍 바인딩 및 지속시간 후 삭제
                BindMultiHitEvents(laserObj, enemyTargets, skill);
                Destroy(laserObj, skill.effectDuration);
            }
        }
        else if (skill.projectilePrefab != null)
        {
            skill.projectilePrefab.transform.localScale = Vector3.one * skill.projectileEffectScale;
            if (skill.spawnOnEachTarget)
            {
                // ----------------------------------------------------
                // 케이스 1: 적 한명 한명에게 개별 유도 투사체를 쏘는 모드
                // ----------------------------------------------------
                foreach (var t in targetsToHit)
                {
                    if (t == null || t.CurrentHP <= 0) continue;

                    bool isFriendlyTargetInEnemySkill = (skill.validTargetGroup == TargetType.Enemy && t.IsPlayer == amIPlayer);
                    if (isFriendlyTargetInEnemySkill)
                    {
                        // 본인에게 투사체를 날리거나 폭발시키지 않고, 힐/버프 로직만 즉시 실행 후 패스!
                        logic.ApplyPayloadsWithSnapshot(new List<CharacterStatus> { t }, effectsToApply, currentSkillSnapshot, skill.skillElement);
                        continue;
                    }

                    Vector3 targetPos = GetCharacterCenter(t) + Vector3.up * 1.0f;

                    // 목적지 계산 분기
                    Vector3 finalDestination = targetPos;
                    if (skill.projectileMotionType == ProjectileMotionType.Penetrate)
                    {
                        Vector3 direction = (targetPos - spawnPos).normalized;
                        finalDestination = targetPos + (direction * skill.flyThroughDistance);
                    }

                    // 투사체 발사 (이동 연출 시작)
                    Coroutine projCoroutine;
                    GameObject projectileObj = visualizer.PlayProjectileWithReturn(skill.projectilePrefab, spawnPos, finalDestination, skill.projectileSpeed, out projCoroutine);

                    // 이동 방식(MotionType)에 따른 확실한 분리!
                    if (skill.projectileMotionType == ProjectileMotionType.StopAtTarget)
                    {
                        // 폭발형: 투사체가 적 몸에 도달할 때까지 코루틴을 멈추고 기다림
                        GameObject hitObj = null;
                        if (skill.hitEffectPrefab != null)
                        {
                            hitObj = visualizer.PlayEffect(skill.hitEffectPrefab, targetPos, finalEffectScale);
                        }

                        BindMultiHitEvents(hitObj, new List<CharacterStatus> { t }, skill);
                    }
                    else
                    {
                        // 관통형: 기다리지 않고 비동기로 감시 코루틴 가동!
                        yield return StartCoroutine(TrackPenetratingHits(projectileObj, new List<CharacterStatus> { t }, skill, effectsToApply, finalEffectScale));
                    }
                }
            }
            else
            {
                // ----------------------------------------------------
                // 케이스 2: 적 진영 전체를 쓸어버리는 거대 광역 투사체 모드 (거대 참격)
                // ----------------------------------------------------
                Vector3 finalDestination = hitCenter;
                if (skill.projectileMotionType == ProjectileMotionType.Penetrate)
                {
                    Vector3 direction = (hitCenter - spawnPos).normalized;
                    finalDestination = hitCenter + (direction * skill.flyThroughDistance);
                }

                Coroutine projCoroutine;
                GameObject projectileObj = visualizer.PlayProjectileWithReturn(skill.projectilePrefab, spawnPos, finalDestination, skill.projectileSpeed, out projCoroutine);

                // 이동 방식(MotionType)에 따른 확실한 분리!
                if (skill.projectileMotionType == ProjectileMotionType.StopAtTarget)
                {
                    // 폭발형: 진영 중심점에 도달할 때까지 대기 후 폭발 데미지
                    yield return projCoroutine;

                    GameObject mainObj = null;
                    if (skill.hitEffectPrefab != null)
                    {
                        mainObj = visualizer.PlayEffect(skill.hitEffectPrefab, hitCenter, finalEffectScale);
                    }

                    BindMultiHitEvents(mainObj, targetsToHit, skill);
                }
                else
                {
                    // 관통형: 기다리지 않고 비동기로 감시 코루틴 가동! 
                    yield return StartCoroutine(TrackPenetratingHits(projectileObj, targetsToHit, skill, effectsToApply, finalEffectScale));
                }
            }
        }
        else
        {
            if (skill.spawnOnEachTarget)
            {
                foreach (var t in targetsToHit)
                {
                    bool isFriendlyTargetInEnemySkill = (skill.validTargetGroup == TargetType.Enemy && t.IsPlayer == amIPlayer);
                    if (isFriendlyTargetInEnemySkill)
                    {
                        // 본인에게 투사체를 날리거나 폭발시키지 않고, 힐/버프 로직만 즉시 실행 후 패스!
                        logic.ApplyPayloadsWithSnapshot(new List<CharacterStatus> { t }, effectsToApply, currentSkillSnapshot, skill.skillElement);
                        continue;
                    }

                    if (t == null || t.CurrentHP <= 0) continue;
                    Vector3 targetPos = GetCharacterCenter(t) + Vector3.up * 1.0f;
                    GameObject hitObj = visualizer.PlayEffect(skill.hitEffectPrefab, targetPos, finalEffectScale);
                    BindMultiHitEvents(hitObj, new List<CharacterStatus> { t }, skill);
                }
            }
            else
            {
                GameObject mainObj = visualizer.PlayEffect(skill.hitEffectPrefab, hitCenter, finalEffectScale);
                BindMultiHitEvents(mainObj, targetsToHit, skill);
            }
        }

        // 이펙트 유지 시간만큼 대기
        yield return new WaitForSeconds(skill.effectDuration);

        // 후처리 대기 및 복귀
        yield return new WaitForSeconds(skill.postHitDelay);

        if (shouldMove)
        {
            visualizer.PlayAnimation("Run");
            yield return StartCoroutine(visualizer.MoveTo(myStartPos, 0.3f));
        }

        Vector3 finalScale = transform.localScale;
        float flipSign = this.GetComponent<CharacterStatus>().IsPlayer ? 1f : -1f;
        finalScale.x = Mathf.Abs(finalScale.x) * flipSign;
        transform.localScale = finalScale;

        visualizer.PlayAnimation("Idle");
        StartCoroutine(EndAction());
    }

    private IEnumerator TrackPenetratingHits(GameObject projectileObj, List<CharacterStatus> targets, SkillData skill, List<EffectGroup> effectsToApply, float finalEffectScale)
    {
        // 아직 안 맞은 적들의 목록 (복사본 생성)
        List<CharacterStatus> remainingTargets = new List<CharacterStatus>(targets);

        // 투사체의 시작 위치 고정
        Vector3 startPos = projectileObj.transform.position;

        // 투사체가 화면 밖으로 나가 파괴되거나, 모든 적을 다 때릴 때까지 매 프레임 감시
        while (projectileObj != null && remainingTargets.Count > 0)
        {
            Vector3 projPos = projectileObj.transform.position;

            // 역순 루프 (목록에서 요소를 지워야 하므로 안전하게 역순 진행)
            for (int i = remainingTargets.Count - 1; i >= 0; i--)
            {
                CharacterStatus t = remainingTargets[i];
                if (t == null || t.CurrentHP <= 0)
                {
                    remainingTargets.RemoveAt(i);
                    continue;
                }

                Vector3 targetCenter = GetCharacterCenter(t);

                // 2.5D 대응 완벽 통과 판정 (벡터 투영 내적 활용)
                // 투사체의 이동 방향 벡터
                Vector3 moveDir = (targetCenter - startPos).normalized;
                // 발사점에서 현재 투사체까지의 벡터
                Vector3 toProj = projPos - startPos;

                // 내적(Dot)을 이용해 투사체가 진행 방향으로 얼마나 날아갔는지 '거리'를 구합니다.
                // 이 방식은 적이 공중에 떠있거나 z축 깊이가 달라도 진행선 기준으로 완벽한 타이밍을 잡습니다.
                float distanceToTarget = Vector3.Distance(startPos, targetCenter);
                float currentProjDistance = Vector3.Dot(toProj, moveDir);

                // 투사체의 진행 거리가 적까지의 거리를 넘어서는 순간! (0.1f는 판정 마진)
                if (currentProjDistance >= distanceToTarget - 0.1f)
                {
                    // 논리적 데미지/효과 즉시 적용
                    List<CharacterStatus> singleTarget = new List<CharacterStatus> { t };
                    logic.ApplyPayloadsWithSnapshot(singleTarget, effectsToApply, currentSkillSnapshot, skill.skillElement);

                    // 피격 성공 이펙트 재생 (적 발밑 또는 중심)
                    if (skill.hitEffectPrefab != null)
                        visualizer.PlayEffect(skill.hitEffectPrefab, targetCenter, finalEffectScale);

                    // 이번 적은 때렸으므로 감시 리스트에서 제거
                    remainingTargets.RemoveAt(i);
                }
            }

            yield return null; // 다음 프레임까지 대기
        }
    }

    public void TriggerDamage(int stepIndex, List<CharacterStatus> hitTargets, SkillData skill)
    {
        // TODO: 카메라 쉐이크 필요할 것 같으면 BattleManager에 제작
        //if (skill.cameraShakeIntensity > 0)
        //{
        //    BattleEvents.OnCameraShake?.Invoke(skill.cameraShakeIntensity, skill.cameraShakeDuration);
        //}

        List<EffectGroup> stepPayloads = GetEffectsForStep(stepIndex, skill);

        logic.ApplyPayloadsWithSnapshot(hitTargets, stepPayloads, currentSkillSnapshot, skill.skillElement);
    }

    private void BindMultiHitEvents(GameObject triggerObj, List<CharacterStatus> hitTargets, SkillData skill)
    {
        if (triggerObj == null)
        {
            logic.ApplyPayloadsWithSnapshot(hitTargets, skill.GetEffects(), currentSkillSnapshot, skill.skillElement);
            return;
        }

        MultiHitEffect hitEffect = triggerObj.GetComponent<MultiHitEffect>();
        if (hitEffect != null)
        {
            hitEffect.Setup(this, hitTargets, skill);
        }
        else
        {
            logic.ApplyPayloadsWithSnapshot(hitTargets, skill.GetEffects(), currentSkillSnapshot, skill.skillElement);
        }
    }

    private List<EffectGroup> GetEffectsForStep(int stepIndex, SkillData skill)
    {
        if (skill.skillSteps != null && stepIndex < skill.skillSteps.Count)
        {
            return skill.skillSteps[stepIndex].stepEffects;
        }

        // 만약 세팅이 비어있거나, 애니메이션 이벤트의 index가 초과되었다면 빈 리스트 반환
        Debug.LogWarning($"[Skill System] {skill.skillName}의 {stepIndex}번째 Step 데이터를 찾을 수 없습니다!");
        return new List<EffectGroup>();
    }

    public void ExecuteItem(CharacterStatus target, ItemData item)
    {
        // 아이템 루틴 코루틴 시작
        StartCoroutine(ItemRoutine(target, item));
    }

    private IEnumerator ItemRoutine(CharacterStatus target, ItemData item)
    {
        // 자원 및 게이지 소모 처리
        logic.ProcessCost(0, item.postActionGaugeDelay);

        // 아이템 사용 애니메이션 재생
        visualizer.PlayAnimation("UseItem");
        yield return new WaitForSeconds(0.5f); // 모션이 끝날 때까지 대기

        // 아이템의 효과 리스트 가져오기 (ItemData 구조에 맞게 변수명 확인 필요: 예: item.itemEffects)
        List<EffectGroup> itemPayloads = item.itemEffects;

        // 타겟 스냅샷 생성 (범용 함수 사용)
        var itemSnapshot = logic.CreateTargetSnapshot(target, itemPayloads);

        // 시각적 타겟(맞을 대상) 리스트 추출 (광역 아이템 대비)
        HashSet<CharacterStatus> uniqueTargets = new HashSet<CharacterStatus>();
        foreach (var targets in itemSnapshot.Values)
        {
            foreach (var t in targets) uniqueTargets.Add(t);
        }
        List<CharacterStatus> hitTargets = uniqueTargets.ToList();

        // 타겟들에게 이펙트 재생 (회복/버프 이펙트 등)
        if (item.hitEffectPrefab != null)
        {
            if (item.isAoE)
            {
                // 광역 아이템
                Vector3 centerPos = GetTargetCenter(hitTargets, target, true);
                visualizer.PlayEffect(item.hitEffectPrefab, centerPos);
            }
            else
            {
                // 단일/개별 아이템: 맞은 대상 각각의 발밑에 이펙트 생성
                foreach (var t in hitTargets)
                {
                    visualizer.PlayEffect(item.hitEffectPrefab, t.transform.position);
                }
            }
        }

        // 효과 즉시 적용! 
        logic.ApplyPayloadsWithSnapshot(hitTargets, itemPayloads, itemSnapshot, null);

        yield return new WaitForSeconds(0.5f); // 효과 확인 대기 시간

        // 원래 상태로 복귀 및 턴 완전 종료
        visualizer.PlayAnimation("Idle");
        StartCoroutine(EndAction());
    }

    // 방어
    public IEnumerator ExecuteDefend()
    {
        if (visualizer != null && visualizer.commonVFX != null && visualizer.commonVFX.defendEffect != null)
        {
            visualizer.PlayEffect(visualizer.commonVFX.defendEffect, transform.position);
        }

        yield return new WaitForSeconds(1f);

        Debug.Log("방어 발동");

        logic.ExecuteDefendLogic();

        StartCoroutine(EndAction());
    }

    public void ExecuteSkill(CharacterStatus target, SkillData skill)
    {
        if (this == null || !gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[CharacterAction] 오브젝트가 파괴되어 스킬을 실행할 수 없습니다.");
            return;
        }

        if (skill == null)
        {
            StartCoroutine(ExecuteDefend());
            return;
        }

        StartCoroutine(ActionRoutine(target, skill.isRanged, skill));
    }

    public void ExecuteTurnBaton(CharacterStatus target)
    {
        BattleEvents.OnTurnOverrideRequested?.Invoke(target);
        StartCoroutine(EndAction());
    }

    private Vector3 GetCharacterCenter(CharacterStatus character)
    {
        // 2D인 경우 Collider2D, 3D인 경우 Collider를 사용하세요.
        Collider2D col = character.GetComponentInChildren<Collider2D>();

        if (col != null)
        {
            return col.bounds.center; // 콜라이더의 정확한 한가운데 좌표 반환
        }

        // 콜라이더가 없다면 임시방편으로 +1.0f
        return character.transform.position + Vector3.up * 1.0f;
    }

    private Vector3 GetTargetCenter(List<CharacterStatus> targets, CharacterStatus defaultTarget, bool isAoE)
    {
        if (isAoE && targets.Count > 0)
        {
            Vector3 center = Vector3.zero;
            foreach (var t in targets)
            {
                center += GetCharacterCenter(t);
            }
            return center / targets.Count;
        }

        return GetCharacterCenter(defaultTarget);
    }

    private IEnumerator EndAction()
    {
        // FSM(PlayerTurnState 등)이 이 방송을 듣고 State를 빠져나가게 됩니다.
        BattleEvents.OnActionCompleted?.Invoke();
        yield break;
    }
}
