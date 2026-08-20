using Unity.Cinemachine;
using UnityEngine;

// 상자(밀고 당길 수 있는 오브젝트)에 부착할 스크립트
public class PushableBox : MonoBehaviour
{
    [Header("Box Weight Feel (이 상자의 무게감)")]
    public float boxPushSpeed = 3f;         // 밀리는 최고 속도
    public float boxPushAcceleration = 2f;  // 밀기 시작할 때의 낑낑댐 (낮을수록 무거움)
    public float boxPushDeceleration = 200f; // 밀기 위해 필요한 힘 (나중에 능력치와 연동 가능)
    public bool isHeavyBox = false;     // 특정 아이템/스킬이 있어야만 밀 수 있는지?

    public CinemachineCamera boxCam;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
    }

    // 플레이어가 상자를 잡았을 때 호출될 함수
    public void OnGrabbed()
    {
        // rb.drag = 0f; 
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (boxCam != null) boxCam.Priority = 100;

        Debug.Log(gameObject.name + "을(를) 잡았습니다!");
    }

    // 플레이어가 상자를 놓았을 때 호출될 함수
    public void OnReleased()
    {
        // rb.drag = 10f;

        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (boxCam != null) boxCam.Priority = 0;
        Debug.Log(gameObject.name + "을(를) 놓았습니다.");
    }
}