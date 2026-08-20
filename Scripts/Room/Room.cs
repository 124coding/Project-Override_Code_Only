using UnityEngine;

public class Room : MonoBehaviour
{
    public GameObject contents;

    [Header("카메라 설정 (Cinemachine용)")]
    public Collider2D cameraConfiner;

    [Header("진입 판정 (룸 활성화용)")]
    public Collider2D roomTrigger;

    private void Awake()
    {
        // 씬 시작 시 컨텐츠 비활성화
        if (contents != null) contents.SetActive(false);
    }

    // RoomManager가 호출할 '플레이어가 방 안에 있는가?' 체크 함수
    public bool IsPlayerInRoom(Vector2 playerPos)
    {
        if (roomTrigger != null)
        {
            return roomTrigger.OverlapPoint(playerPos);
        }
        // 트리거가 없다면 기존처럼 컨파이너를 사용 (백업)
        return cameraConfiner != null && cameraConfiner.OverlapPoint(playerPos);
    }

    public Vector2 GetCenter()
    {
        // 판정 기준을 트리거 중심점으로 잡는 것이 더 정확합니다.
        if (roomTrigger != null) return roomTrigger.bounds.center;
        if (cameraConfiner != null) return cameraConfiner.bounds.center;
        return transform.position;
    }
}