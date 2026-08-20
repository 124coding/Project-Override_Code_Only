using UnityEngine;
using TMPro;
using DG.Tweening;

public class SpecialEffectPopup : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
    }

    // 외부에서 텍스트 내용과 색상을 받아 연출
    public void Setup(string message, Color textColor)
    {
        textMesh.text = message;
        textMesh.color = textColor;

        // 시작 크기를 0으로 만듭니다.
        transform.localScale = Vector3.zero;

        // 여러 애니메이션을 순서대로 실행해주는 큐
        Sequence seq = DOTween.Sequence();

        // 1. 화면 중앙에 박혀서 1.3배 -> 1배 로 크기가 바뀜
        seq.Append(transform.DOScale(1.3f, 0.15f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(1.0f, 0.05f));

        // 2. 0.5초 대기
        seq.AppendInterval(0.5f);

        // 3. 화면 밖으로 확 커지면서 서서히 투명해지며 소멸
        seq.Append(transform.DOScale(1.8f, 0.25f).SetEase(Ease.InQuad));
        seq.Join(textMesh.DOFade(0f, 0.25f));

        // 4. 모든 시퀀스 애니메이션이 끝나면 스스로 오브젝트 파괴
        seq.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
}
