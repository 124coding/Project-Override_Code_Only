using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class FloorWeightButton : MonoBehaviour
{
    [Header("버튼 설정")]
    [Tooltip("버튼을 누르기 위해 필요한 최소 무게(Mass)")]
    public float requiredMass = 2f;
    [Tooltip("버튼을 누를 수 있는 레이어 (예: Player, PushableBox)")]
    public LayerMask pressableLayer;

    [Header("작동 대상 (IWorkObject)")]
    public GameObject[] targetObjects;

    // 현재 버튼을 누르고 있는 유효한 콜라이더들을 담는 바구니
    private HashSet<Collider2D> pressingObjects = new HashSet<Collider2D>();
    private List<IWorkObject> workObjects = new List<IWorkObject>();

    private bool isPressed = false;

    private void Awake()
    {
        // 1. 트리거 설정 강제 켜기 (실수 방지)
        GetComponent<BoxCollider2D>().isTrigger = true;

        // 2. 작동시킬 타겟들 미리 캐싱 (기존 FieldLever와 동일한 방식)
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null && obj.TryGetComponent(out IWorkObject workObj))
            {
                workObjects.Add(workObj);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (((1 << other.gameObject.layer) & pressableLayer) == 0) return;

        // 수정: GetComponent 대신 attachedRigidbody 사용!
        Rigidbody2D rb = other.attachedRigidbody;

        if (rb != null && rb.mass >= requiredMass)
        {
            Debug.Log("Press 성공");
            pressingObjects.Add(other);
            UpdateButtonState();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 물체가 버튼 밖으로 나가면 목록에서 제거
        if (pressingObjects.Contains(other))
        {
            pressingObjects.Remove(other);
            UpdateButtonState();
        }
    }

    private void UpdateButtonState()
    {
        // 에러 방지: 올려져 있던 물체가 갑자기 파괴되었을 경우(null) 목록에서 자동 정리
        pressingObjects.RemoveWhere(col => col == null || !col.gameObject.activeInHierarchy);

        // 누르고 있는 물체가 1개 이상이면 눌린 상태
        bool shouldBePressed = pressingObjects.Count > 0;

        if (shouldBePressed && !isPressed)
        {
            isPressed = true;
            Debug.Log("딸깍! 버튼이 눌렸습니다.");

            // TODO: 버튼이 바닥으로 쑥 들어가는 스프라이트 변경이나 애니메이션 재생

            foreach (var obj in workObjects) obj.WorkOn();
        }
        else if (!shouldBePressed && isPressed)
        {
            isPressed = false;
            Debug.Log("틱! 버튼이 다시 올라왔습니다.");

            // TODO: 버튼이 다시 올라오는 스프라이트 변경이나 애니메이션 재생

            foreach (var obj in workObjects) obj.WorkOff();
        }
    }
}