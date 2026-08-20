using System.Collections;
using UnityEngine;

public class ActionVisualizer : MonoBehaviour
{
    public CommonBattleVFX commonVFX;

    private Animator anim;
    private SpriteRenderer sr;
    private CharacterStatus myStatus;

    private void OnEnable()
    {
        BattleEvents.OnCommonVFXRequested += HandleCommonVFX;
    }

    private void OnDisable()
    {
        BattleEvents.OnCommonVFXRequested -= HandleCommonVFX;
    }

    protected virtual void Start()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (myStatus == null) myStatus = GetComponentInChildren<CharacterStatus>();
    }

    public void PlayAnimation(string stateName, Color animColor)
    {
        if (anim == null)
        {
            Debug.LogError("Animator를 찾을 수 없습니다! 모델링이 제대로 생성/연결되었는지 확인하세요.");
            return;
        }

        sr.color = animColor;

        anim.speed = 1f;
        anim.Play(stateName, 0, 0f);
    }

    public void PlayAnimation(string stateName)
    {
        PlayAnimation(stateName, Color.white);
    }

    public IEnumerator PlayAnimationAndHoldLastFrame(string stateName)
    {
        if (anim == null) yield break;

        anim.speed = 1f; // 정상 속도로 시작
        anim.Play(stateName, 0, 0f);
        yield return null; // 애니메이터 상태가 바뀌는 데 걸리는 1프레임 대기

        // 혹시 블렌딩 등으로 인해 상태가 바로 안 바뀌었을 경우를 위한 안전장치
        float timeout = 1.0f;
        while (!anim.GetCurrentAnimatorStateInfo(0).IsName(stateName) && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // 루프(반복) 애니메이션이 아닐 때만 마지막 프레임 고정 로직 작동
        if (!anim.GetCurrentAnimatorStateInfo(0).loop)
        {
            // normalizedTime이 0.99f(99% 재생 완료)가 될 때까지 대기
            while (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.99f)
            {
                yield return null;
            }

            // 마지막 프레임에 도달하면 재생 속도를 0으로 만들어 일시정지!
            anim.speed = 0f;
        }
    }

    public GameObject PlayEffect(GameObject prefab, Vector3 position, float scale = 1.0f)
    {
        if (prefab != null)
        {
            GameObject fx = Instantiate(prefab, position, Quaternion.identity);
            fx.transform.localScale = Vector3.one * scale; // 생성 직후 크기 조절

            return fx;
        }

        return null;
    }

    public IEnumerator MoveTo(Vector3 targetPos, float duration)
    {
        Vector3 startPos = transform.position;

        float elapsed = 0f;
        float moveDirX = targetPos.x - startPos.x;

        if (Mathf.Abs(moveDirX) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            // 원래 크기 비율(Mathf.Abs)은 유지하면서 방향(Sign)만 결정합니다.
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveDirX);
            transform.localScale = scale;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }
        transform.position = targetPos;
    }

    public GameObject PlayProjectileWithReturn(GameObject prefab, Vector3 spawnPos, Vector3 targetPos, float speed, out Coroutine projCoroutine)
    {
        GameObject projObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        projCoroutine = StartCoroutine(ProjectileMovementRoutine(projObj, targetPos, speed));

        return projObj;
    }

    // 투사체가 날아가고 도달할 때까지 대기하는 실제 로직 (기존 코드 재활용)
    private IEnumerator ProjectileMovementRoutine(GameObject projObj, Vector3 targetPos, float speed)
    {
        // 이미 위에서 생성된 projObj를 매개변수로 받아서 사용합니다.
        Projectile proj = projObj.GetComponent<Projectile>();
        bool isHit = false;

        GameObject dummy = new GameObject("Dummy");
        dummy.transform.position = targetPos;

        if (proj != null)
        {
            proj.Fire(dummy.transform, speed, () => isHit = true);
            yield return new WaitUntil(() => isHit);
        }
        else
        {
            Debug.LogWarning("투사체 프리팹에 Projectile 스크립트가 없습니다!");
            yield return new WaitForSeconds(0.5f);
        }

        // 목적지에 도달하면 더미 삭제
        Destroy(dummy);
    }

    // 레이저 연출
    public GameObject PlayLaser(GameObject prefab, Vector3 spawnPos, Vector3 targetPos, float effectScale = 1f, float overshoot = 0f)
    {
        if (prefab == null) return null;

        // 방향 및 관통 최종 목적지 계산
        Vector3 direction = (targetPos - spawnPos).normalized;
        Vector3 finalEndPos = targetPos + (direction * overshoot);

        // 레이저 생성
        GameObject laserObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        // LineRenderer 세팅 및 크기(두께) 조절
        LineRenderer line = laserObj.GetComponentInChildren<LineRenderer>();
        if (line != null)
        {
            line.SetPosition(0, spawnPos);
            line.SetPosition(1, finalEndPos);

            line.widthMultiplier *= effectScale;
        }
        else
        {
            // 라인 렌더러가 없는 일반 프리팹이면 기존처럼 Transform 스케일을 키워줍니다.
            laserObj.transform.localScale = Vector3.one * effectScale;
        }

        return laserObj;
    }

    public IEnumerator ExecuteDeathVisuals(Sprite deathSprite)
    {
        if (anim != null)
        {
            anim.Play("Die");
            yield return null;

            float timeout = 2.0f;
            while (!anim.GetCurrentAnimatorStateInfo(0).IsName("Die") && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            while (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            anim.enabled = false;
        }

        // 애니메이션이 끝나면 스프라이트를 죽음 상태로 교체
        if (sr != null && deathSprite != null)
        {
            sr.sprite = deathSprite;
        }
    }

    // 부활 시각 연출 전담 함수 (애니메이터 켜고, 기본 스프라이트로 복구)
    public void ExecuteReviveVisuals(Sprite basicSprite)
    {
        if (sr != null && basicSprite != null)
        {
            sr.sprite = basicSprite;
        }

        if (anim != null)
        {
            anim.enabled = true;
            anim.Play("Idle", 0, 0f);
        }
    }

    private void HandleCommonVFX(CharacterStatus target, CommonVFXType vfxType)
    {
        // 방송에서 부른 타겟이 '나(myStatus)'가 아니면 무시!
        if (target != myStatus) return;

        // 나를 불렀다면, 타입에 맞는 프리팹을 찾아서 재생
        GameObject prefabToPlay = null;

        switch (vfxType)
        {
            case CommonVFXType.Heal:
                prefabToPlay = commonVFX.healEffect;
                break;
            case CommonVFXType.CurseHeal:
                prefabToPlay = commonVFX.curseHealEffect;
                break;
            case CommonVFXType.RestoreMP:
                prefabToPlay = commonVFX.restoreMPEffect;
                break;
            case CommonVFXType.Revive:
                prefabToPlay = commonVFX.reviveEffect;
                break;
            case CommonVFXType.Defend:
                prefabToPlay = commonVFX.defendEffect;
                break;
            case CommonVFXType.Dodge:
                prefabToPlay = commonVFX.dodgeEffect;
                break;
        }

        // 기존에 만들어두신 PlayEffect 함수를 활용하여 이펙트 재생!
        if (prefabToPlay != null)
        {
            PlayEffect(prefabToPlay, transform.position);
        }
    }
}