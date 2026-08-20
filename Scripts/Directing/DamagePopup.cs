using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamagePopup : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private Vector3 originalScale;

    private void Awake()
    {
        // 텍스트 컴포넌트를 미리 가져옵니다.
        textMesh = GetComponent<TextMeshProUGUI>();
        originalScale = transform.localScale;
    }

    // 외부(매니저)에서 데미지 수치를 전달받아 텍스트를 세팅하고 애니메이션을 실행하는 함수
    public void Setup(int amount)
    {
        // 1. 조건에 따른 텍스트 및 색상 분기 처리
        if (amount > 0)
        {
            // 회복인 경우 (+ 기호 추가, 초록색)
            textMesh.text = "+" + amount.ToString();
            textMesh.color = Color.green;
        }
        else
        {
            // 데미지인 경우 (음수는 자동으로 - 기호가 붙음, 빨간색)
            textMesh.text = amount.ToString();
            textMesh.color = Color.red;
        }

        // 2. 폰트 크기 애니메이션 (DoTween Scale)
        // 팝업 생성 시 크기를 0으로 만들었다가 0.3초 만에 원래 크기(1)로 고무줄처럼 튕기듯 커집니다.
        transform.localScale = Vector3.zero;
        transform.DOScale(originalScale, 0.3f).SetEase(Ease.OutBack);

        // 3. 이동 및 페이드아웃 (기존 로직 유지)
        Vector3 targetPos = transform.position + Camera.main.transform.up * 1.5f;
        transform.DOMove(targetPos, 1f);

        // 0.5초 대기 후 서서히 투명해지다가 파괴됩니다.
        textMesh.DOFade(0f, 1f).SetDelay(0.5f).OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
}
