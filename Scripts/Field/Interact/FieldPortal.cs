using UnityEngine;
using System.Collections;

public class FieldPortal : InteractObject, IWorkObject
{
    [Header("Portal Settings")]
    public string targetSceneName;
    public Vector3 targetPosition;

    [Header("Power Settings")]
    [Tooltip("체크 해제하면 기본적으로 꺼진 상태(작동 불가)로 시작합니다.")]
    public bool isPoweredOn = false;

    private bool isOnCooldown = false;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (isPoweredOn) anim.Play("Portal_On");
    }

    public void WorkOn()
    {
        isPoweredOn = true;
        Debug.Log($"[{gameObject.name}] 포탈 전원 ON!");

        anim.Play("Portal_On");
    }

    public void WorkOff()
    {
        isPoweredOn = false;
        Debug.Log($"[{gameObject.name}] 포탈 전원 OFF!");

        anim.Play("Portal_Off");

    }

    protected override void OnInteraction()
    {
        if (!isPoweredOn)
        {
            Debug.Log("포탈에 전력이 공급되지 않아 작동하지 않습니다.");
            return;
        }

        if (isOnCooldown)
        {
            Debug.Log("포탈이 아직 충전 중입니다. 잠시 후 다시 시도하세요.");
            return;
        }

        // DataManager 저장
        DataManager.Instance.SetObjectState(uniqueID, true);
        isOnCooldown = true;

        if (targetPosition != null && targetSceneName != null)
        {
            // 포탈의 역할은 여기서 끝! 코루틴 없이 매니저에게 모든 걸 토스합니다.
            TeleportManager.Instance.TeleportPlayer(targetSceneName, targetPosition, () =>
            {
                isOnCooldown = false; // 텔레포트가 끝났을 때 쿨타임 OFF
                Debug.Log($"[{gameObject.name}] 포탈 재사용 대기시간 종료!");
            });
        }
    }

    protected override void LoadState(bool isActivated)
    {
        // 세이브 데이터 복구 시 처리
        if (isActivated)
        {
            // 이전에 퍼즐을 풀어서 포탈을 켜둔 상태로 저장했다면, 로드할 때 다시 켜줍니다.
            WorkOn();
        }
        else
        {
            // 꺼져있는 상태라면 시각적으로 꺼진 연출을 적용해 줍니다.
            if (!isPoweredOn) WorkOff();
        }
    }
}