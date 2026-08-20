using UnityEngine;
using System.Collections;

public class DisappearWall : MonoBehaviour
{
    // 공격을 받았을 때 사라지게 하는 함수
    public void HitByAttack()
    {
        StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator FadeOutAndDisable()
    {
        // TODO: 여기서 투명도(Alpha)를 서서히 줄이는 연출을 넣으세요.
        // 예를 들어 SpriteRenderer.color의 alpha 값을 조절

        yield return new WaitForSeconds(0.5f); // 0.5초 뒤에 사라짐

        gameObject.SetActive(false); // 벽 비활성화
    }
}