using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [Header("파괴될 시간 (초)")]
    public float fallbackDestroyTime = 1.0f; // 기본값 1초

    private void Start()
    {
        Animator anim = GetComponent<Animator>();

        if (anim != null)
        {
            // 현재 재생 중인 애니메이션의 첫 번째 레이어(0)의 길이를 초(second) 단위로 가져옵니다.
            float animLength = anim.GetCurrentAnimatorStateInfo(0).length;

            // 애니메이션 길이만큼 기다렸다가 파괴합니다.
            Destroy(gameObject, animLength);
        }
        else
        {
            // 만약 Animator가 없는 단순 파티클이라면 기본 시간 뒤에 파괴합니다.
            Destroy(gameObject, fallbackDestroyTime);
        }
    }
}