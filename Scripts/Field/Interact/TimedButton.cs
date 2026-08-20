using System.Collections.Generic;
using UnityEngine;

public class TimedButton : InteractObject
{
    [Header("Target Objects")]
    public GameObject[] targetObjects;

    [Header("Timer Settings")]
    public float activeDuration = 3f;

    private List<IWorkObject> workObjects = new List<IWorkObject>();
    private bool isActivated = false;
    private float currentTimer = 0f;

    public Sprite onSprite;
    public Sprite offSprite;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                IWorkObject workObj = obj.GetComponent<IWorkObject>();
                if (workObj != null) workObjects.Add(workObj);
            }
        }
    }

    private void Update()
    { 

        if (activeDuration == -1f) return;

        if (isActivated)
        {
            currentTimer -= Time.deltaTime;
            if (currentTimer <= 0f) DeactivateButton();
        }
    }

    // 시간 제한 버튼의 통합 상호작용 함수 오버라이드
    protected override void OnInteraction()
    {
        if (isActivated && activeDuration != -1f) return;

        ActivateButton();
    }

    protected override void LoadState(bool isActivated)
    {
        if (activeDuration == -1f && isActivated)
        {
            ActivateButton();
        }
    }

    private void ActivateButton()
    {
        isActivated = true;

        // -1일 때는 타이머를 돌리지 않음
        if (activeDuration != -1f)
        {
            currentTimer = activeDuration;
        }
        else
        {
            DataManager.Instance.SetObjectState(uniqueID, true);
        }

        if (onSprite != null) sr.sprite = onSprite;

        // 대상을 켜는 명령만 보냄
        foreach (var obj in workObjects) obj.WorkOn();

        Debug.Log(activeDuration == -1f ? "영구 활성화 스위치 ON" : "시간 제한 버튼 작동!");
    }

    private void DeactivateButton()
    {
        isActivated = false;
        if (offSprite != null) sr.sprite = offSprite;

        foreach (var obj in workObjects) obj.WorkOff();
        Debug.Log("시간 초과 버튼 리셋");
    }
}