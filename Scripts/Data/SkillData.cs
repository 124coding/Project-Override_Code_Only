using System.Collections.Generic;
using UnityEngine;

public enum ProjectileMotionType
{
    StopAtTarget,
    Penetrate
}

[System.Serializable]
public class SkillStep
{
    [Tooltip("해당 타격 순서(Index)에서 터질 효과 리스트")]
    public List<EffectGroup> stepEffects = new List<EffectGroup>();

    public SkillStep() { }

    public SkillStep(List<EffectGroup> effects)
    {
        this.stepEffects = new List<EffectGroup>(effects);
    }
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "Battle/Skill Data")]
public class SkillData : ScriptableObject, IEffectProvider
{
    [Header("Basic Settings")]
    public string skillID;
    public string skillName;
    [TextArea(1, 5)]
    public string skillDescription;
    public int mpCost;
    public int postActionGaugeDelay;
    public int requiredSkillPoint;

    [Header("Ultimate Settings")]
    public bool isUltimate = false;
    public int requiredTurns = 5;

    [Header("Timing Settings")]
    public float castDelay = 0f;
    public float effectDuration = 1.5f;

    public int GetPostActionGauge() => postActionGaugeDelay;

    public bool isAoE;

    [Header("Effect Settings")]
    public float castEffectScale = 1.0f;
    public float projectileEffectScale = 1.0f;
    public float effectScale = 1.0f;
    public bool spawnOnEachTarget = false;
    public float postHitDelay = 0.4f;

    [Header("Visuals")]
    public Sprite skillIcon;

    [Header("Attribute")]
    public ElementData skillElement;

    [Header("Animation Settings")]
    public bool isRanged;
    public string prepAnimName;
    public string animName = "Skill";
    public Color animColor = Color.white;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public ProjectileMotionType projectileMotionType = ProjectileMotionType.StopAtTarget;
    public float flyThroughDistance = 15f;

    [Header("Laser Settings")]
    public float laserOvershoot = 15f;

    [Header("Prefab")]
    public GameObject laserPrefab;
    public GameObject castEffectPrefab;
    public GameObject hitEffectPrefab;
    public GameObject beneficialEffectPrefab;

    [Header("TargettingSet")]
    public TargetType validTargetGroup;

    [Header("Logic Settings (Steps)")]
    [Tooltip("타격 단계별 효과 리스트 (1타 스킬은 1개의 Step만 생성하면 됩니다)")]
    public List<SkillStep> skillSteps = new List<SkillStep>();

    [HideInInspector, SerializeField]
    private List<EffectGroup> skillEffects = new List<EffectGroup>();

    public List<EffectGroup> GetEffects()
    {
        List<EffectGroup> allEffects = new List<EffectGroup>();
        if (skillSteps != null)
        {
            foreach (var step in skillSteps)
            {
                if (step.stepEffects != null)
                    allEffects.AddRange(step.stepEffects);
            }
        }
        return allEffects;
    }

    [Header("AI Setting")]
    public int lastUsedValue;

    private void OnValidate()
    {
        // 기존 skillEffects에 데이터가 남아있고, 아직 skillSteps로 옮겨지지 않았다면?
        if (skillEffects != null && skillEffects.Count > 0)
        {
            if (skillSteps == null) skillSteps = new List<SkillStep>();

            if (skillSteps.Count == 0)
            {
                // 기존 데이터를 skillSteps의 1번째 스텝(Index 0)으로 이전
                skillSteps.Add(new SkillStep(skillEffects));
                Debug.Log($"[{name}] 기존 skillEffects 데이터를 skillSteps[0]으로 성공적으로 이관했습니다!");
            }

            skillEffects.Clear(); 
        }
    }
}