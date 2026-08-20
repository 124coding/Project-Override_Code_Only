using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerSource : MonoBehaviour
{
    [Header("Settings")]
    public float timeToActivate = 1.5f; // 완전히 켜지기까지 필요한 시간
    private float currentCharge = 0f;    // 현재 충전량

    [Header("Target Objects")]
    public GameObject[] targetObjects;
    private List<IWorkObject> workObjects = new List<IWorkObject>();

    [Header("Visuals")]
    private SpriteRenderer sr;
    public SpriteRenderer glowSr;

    private Coroutine fadeCoroutine;

    private bool isActivated = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                IWorkObject workObj = obj.GetComponent<IWorkObject>();
                if (workObj != null)
                {
                    workObjects.Add(workObj);
                }
                else
                {
                    Debug.LogWarning($"{obj.name}에는 IWorkObject 인터페이스가 없습니다!");
                }
            }
        }

        if (glowSr != null)
        {
            glowSr.gameObject.SetActive(false);

            Color c = glowSr.color;
            c.a = 0f;
            glowSr.color = c;
        }
    }

    public void ReceiveLaser()
    {
        if (isActivated) return; // 이미 켜져 있으면 무시

        currentCharge += Time.deltaTime;

        float progress = Mathf.Clamp01(currentCharge / timeToActivate);
        if (sr != null) sr.color = Color.Lerp(Color.white, Color.blue, progress);

        if (currentCharge >= timeToActivate)
        {
            SetActivated(true);
        }
    }

    private void Update()
    {
        if (!isActivated && currentCharge > 0)
        {
            currentCharge -= Time.deltaTime * 0.5f; 
            currentCharge = Mathf.Max(0, currentCharge);
            if (sr != null) sr.color = Color.Lerp(Color.white, Color.blue, Mathf.Clamp01(currentCharge / timeToActivate));
        }
    }

    private void SetActivated(bool status)
    {
        isActivated = status;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeGlow(isActivated));

        foreach (IWorkObject workObj in workObjects)
        {
            if (isActivated) workObj.WorkOn();
            else workObj.WorkOff();
        }
    }

    private IEnumerator FadeGlow(bool fadeIn)
    {
        if (glowSr == null) yield break;

        float startAlpha = glowSr.color.a;
        float targetAlpha = fadeIn ? 1f : 0f;
        float elapsed = 0f;

        // 켜질 때만 미리 활성화
        if (fadeIn) glowSr.gameObject.SetActive(true);

        while (elapsed < timeToActivate)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / timeToActivate);

            Color color = glowSr.color;
            color.a = newAlpha;
            glowSr.color = color;

            yield return null;
        }

        // 꺼질 때만 최종적으로 비활성화
        if (!fadeIn) glowSr.gameObject.SetActive(false);
    }
}