using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    [Header("UI Reference")]
    public Image fadeImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 부모 캔버스 최상단에 있다면 gameObject 전체 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FadeOut(float duration, Action onComplete = null)
    {
        StartCoroutine(FadeOutRoutine(duration, onComplete));
    }

    private IEnumerator FadeOutRoutine(float duration, Action onComplete)
    {
        fadeImage.transform.parent.gameObject.SetActive(true);
        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        yield return fadeImage.DOFade(1f, duration)
                              .SetEase(Ease.OutQuad)
                              .SetUpdate(true)
                              .WaitForCompletion();

        onComplete?.Invoke();
    }

    // 어디서든 부를 수 있는 만능 페이드 인
    public void FadeIn(float duration, Action onComplete = null)
    {
        StartCoroutine(FadeInRoutine(duration, onComplete));
    }

    private IEnumerator FadeInRoutine(float duration, Action onComplete)
    {
        fadeImage.transform.parent.gameObject.SetActive(true);
        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;

        // DOTween 연출
        yield return fadeImage.DOFade(0f, duration)
                              .SetEase(Ease.OutQuad)
                              .SetUpdate(true)
                              .WaitForCompletion();

        fadeImage.gameObject.SetActive(false);

        // 연출이 다 끝난 후 실행할 행동이 있다면 실행
        onComplete?.Invoke();
    }
}