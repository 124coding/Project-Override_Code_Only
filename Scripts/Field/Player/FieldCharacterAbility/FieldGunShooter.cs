using UnityEngine.InputSystem;
using UnityEngine;

public class FieldGunShooter : MonoBehaviour
{
    [Header("조준 설정")]
    public Transform gunArm;       // 회전할 팔 또는 무기
    public Transform firePoint;    // 총알이 나갈 위치

    [Header("발사 설정")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public float fireRate = 0.5f;

    private Camera mainCam;
    private float nextFireTime = 0f;
    private bool isAimingMode = false;

    public bool isActiveCharacter = false;

    // private InputAction skillAction;
    private InputAction aimingAction;
    private InputAction aimingCancel;

    private void Start()
    {
       // skillAction = InputManager.Instance.inputActions.Field.UseSkill;
        aimingAction = InputManager.Instance.inputActions.Aiming.AimShoot;
        aimingCancel = InputManager.Instance.inputActions.Aiming.Cancel;
    }

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        // 현재 조종 중인 캐릭터가 아니면 입력을 받지 않음
        if (!isActiveCharacter) return;

        if (!isAimingMode)
        {
            // 조준 모드 진입(SkillUse) 키만 검사합니다.
            //if (skillAction.WasPressedThisFrame())
            //{
            //    ToggleAimMode(true); // 강제로 켜기
            //}
        }
        else
        {
            // 조준 로직, 발사, 취소(Cancel) 키만 검사합니다.
            AimAtMouse();

            if (aimingAction.WasPressedThisFrame() && Time.time >= nextFireTime)
            {
                Shoot();
            }

            // (옵션) 스킬 키를 다시 누르거나, 우클릭 등으로 조준 취소
            if (aimingCancel.WasPressedThisFrame())
            {
                ToggleAimMode(false); // 강제로 끄기
            }
        }
    }

    // 모드 전환 통합 함수
    private void ToggleAimMode(bool? forceState = null)
    {
        isAimingMode = forceState ?? !isAimingMode;

        if (isAimingMode)
        {
            // Field(이동) 맵을 잠그고, Aim(조준) 맵을 켭니다.
            InputManager.Instance.SwitchActionMap("Aiming");
            Debug.Log("조준 모드 ON - 이동 불가");
            // TODO: 총을 꺼내는 애니메이션 (anim.SetBool("IsAiming", true))
        }
        else
        {
            // 다시 Field 맵을 켜서 이동 가능하게 만듭니다.
            InputManager.Instance.SwitchActionMap("Field");
            ResetArmRotation();
            Debug.Log("조준 모드 OFF - 이동 가능");
            // TODO: 총을 집어넣는 애니메이션 (anim.SetBool("IsAiming", false))
        }
    }

    private void AimAtMouse()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
        Vector3 mouseWorldPosition = mainCam.ScreenToWorldPoint(mouseScreenPosition);

        Vector3 aimDirection = mouseWorldPosition - transform.position;
        aimDirection.z = 0;

        // 마우스 위치에 따른 몸통 좌우 반전
        float facingDirection = aimDirection.x > 0 ? 1f : -1f;
        transform.localScale = new Vector3(facingDirection, 1, 1);

        // 팔 회전 계산
        if (gunArm != null)
        {
            // 몸통이 왼쪽(-1)으로 뒤집혔다면, 각도 계산을 위해 마우스의 X 위치도 반전시켜줍니다.
            float localAimX = aimDirection.x * facingDirection;

            // X, Y 거리를 바탕으로 삼각함수(Atan2)를 돌려 각도를 뽑아냅니다.
            float angle = Mathf.Atan2(aimDirection.y, localAimX) * Mathf.Rad2Deg;

            // World 회전이 아닌 '로컬 회전(localRotation)'에 적용해야 몸통의 스케일 반전과 예쁘게 융합됩니다!
            gunArm.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void Shoot()
    {
        nextFireTime = Time.time + fireRate;

        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                // 유니티의 -1 스케일 버그를 보정합니다.
                // 기본적으로는 총구의 오른쪽(right)으로 날아가게 세팅하지만...
                Vector2 shootDir = firePoint.right;

                // 캐릭터의 몸통(Scale X)이 왼쪽(-1)을 향해 뒤집혀 있다면?
                if (transform.localScale.x < 0)
                {
                    // 날아가는 방향을 완전히 정 반대로(-shootDir) 뒤집어줍니다!
                    shootDir = -firePoint.right;
                }

                // 보정된 방향으로 속도를 줍니다.
                rb.linearVelocity = shootDir * bulletSpeed;

                // 덤으로, 총알 이미지의 회전 각도도 날아가는 방향에 맞춰 예쁘게 돌려줍니다.
                float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        Debug.Log("타탕!");
        // TODO: 반동 애니메이션, 이펙트, 사운드 재생
    }

    private void ResetArmRotation()
    {
        if (gunArm != null)
            gunArm.localRotation = Quaternion.Euler(0, 0, 0);
    }

    //
    // 캐릭터 교체 씬이나, 피격으로 기절했을 때의 안전장치
    private void OnDisable()
    {
        if (isAimingMode)
        {
            // 스크립트가 꺼질 때(캐릭터가 교체될 때) 무조건 조준 모드를 강제로 끕니다.
            // 안 그러면 다음 캐릭터로 넘어가도 조작키가 먹통(Lock)이 됩니다!
            ToggleAimMode(false);
        }
    }
}