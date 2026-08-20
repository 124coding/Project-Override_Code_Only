using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class BattleDirector : MonoBehaviour
{
    [Header("PopupPrefab")]
    public GameObject damagePopupPrefab;
    public GameObject specialEffectPrefab;

    [Header("Popup Canvas")]
    public Transform popupCanvas;
    public Transform screenCanvas;

    [Header("Battle End Directing")]
    public CanvasGroup dimPanel;
    public RectTransform resultTextRect;
    public TextMeshProUGUI resultText;

    [Header("Damage Popup Settings")]
    public float popupInterval = 0.15f;
    public float scatterRadius = 0.5f;

    private Dictionary<CharacterStatus, Queue<int>> damageQueues = new Dictionary<CharacterStatus, Queue<int>>();
    private Dictionary<CharacterStatus, Coroutine> popupCoroutines = new Dictionary<CharacterStatus, Coroutine>();

    // TODO: Test용 삭제 필요
    private bool isWaitingForAnyKey = false;
    private bool wasBattleWon = false;

    private void Start()
    {
        BattleEvents.OnHealthChanged += HandleHealthChanged;
        BattleEvents.OnBreakOccurred += HandleBreakOccurred;
        BattleEvents.OnBattleEnded += HandleBattleEnded;
    }

    //private void Update()
    //{
    //    if (isWaitingForAnyKey && Input.anyKeyDown)
    //    {
    //        isWaitingForAnyKey = false;
    //        BattleEvents.OnReturnToField?.Invoke(wasBattleWon);
    //    }

    //    // TODO: 테스트용
    //    if (Keyboard.current.wKey.wasPressedThisFrame) BattleEvents.OnBattleEnded?.Invoke(true);
    //    if (Keyboard.current.lKey.wasPressedThisFrame) BattleEvents.OnBattleEnded?.Invoke(false);
    //}

    private void HandleBreakOccurred(CharacterStatus characterStatus)
    {
        // 연출의 연속성(BREAK가 끝날 때쯤 1MORE 등장)을 위해 코루틴으로 제어합니다.
        StartCoroutine(BreakAndOneMoreRoutine(characterStatus));
    }

    private IEnumerator BreakAndOneMoreRoutine(CharacterStatus characterStatus)
    {
        // 화면에 Break 프리팹 찍기
        // TODO: 적 위치에 찍기
        GameObject breakObj = Instantiate(specialEffectPrefab, screenCanvas);
        breakObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        SpecialEffectPopup breakScript = breakObj.GetComponent<SpecialEffectPopup>();
        breakScript.Setup("BREAK!", Color.red);

        // 0.6초 대기
        yield return new WaitForSeconds(0.6f);

        GameObject oneMoreObj = Instantiate(specialEffectPrefab, screenCanvas);
        oneMoreObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    
        SpecialEffectPopup oneMoreScript = oneMoreObj.GetComponent<SpecialEffectPopup>();
        oneMoreScript.Setup("1MORE!", new Color(1f, 0.7f, 0f));
    }

    private void HandleHealthChanged(CharacterStatus targetStatus, int changeAmount)
    {
        // 데미지가 아니면 무시 (혹은 힐 팝업도 처리 가능)
        if (changeAmount >= 0) return;

        // 1. 해당 타겟의 큐가 없으면 만들어줍니다.
        if (!damageQueues.ContainsKey(targetStatus))
        {
            damageQueues[targetStatus] = new Queue<int>();
        }

        // 2. 데미지 수치를 큐에 넣습니다. (본 데미지 -> 추가 데미지 순으로 쌓임)
        damageQueues[targetStatus].Enqueue(changeAmount);

        // 3. 만약 팝업을 꺼내주는 코루틴이 쉬고 있다면 작동시킵니다.
        if (!popupCoroutines.ContainsKey(targetStatus) || popupCoroutines[targetStatus] == null)
        {
            popupCoroutines[targetStatus] = StartCoroutine(ProcessDamageQueue(targetStatus));
        }
    }

    // 큐에 들어있는 데미지를 0.15초 간격으로 하나씩 꺼내서 띄워주는 코루틴
    private IEnumerator ProcessDamageQueue(CharacterStatus targetStatus)
    {
        while (damageQueues.ContainsKey(targetStatus) && damageQueues[targetStatus].Count > 0)
        {
            // 큐에서 데미지 하나 꺼내기
            int currentDamage = damageQueues[targetStatus].Dequeue();

            // 위치에 살짝 랜덤 오프셋 주기 (글자가 겹치지 않게 흩뿌림)
            Vector3 randomOffset = new Vector3(
                Random.Range(-scatterRadius, scatterRadius),
                Random.Range(-scatterRadius, scatterRadius),
                0f
            );
            Vector3 spawnPos = targetStatus.EntityTransform.position + randomOffset;

            // 프리팹 생성 및 세팅
            GameObject popupObj = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity, popupCanvas);
            DamagePopup popupScript = popupObj.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(currentDamage);

            Debug.Log($"Damage Text Spawn: {currentDamage}");

            // 다음 데미지가 뜰 때까지 잠깐 대기 (타다닥! 연출)
            yield return new WaitForSeconds(popupInterval);
        }

        // 큐가 다 비었으면 코루틴 상태를 비워줌
        popupCoroutines[targetStatus] = null;
    }

    private void HandleBattleEnded(bool isWin)
    {
        // 기존 결과 애니메이션 삭제
        resultTextRect.DOKill();

        // 공통: 텍스트 오브젝트 켜기 및 배경 어둡게
        //resultTextRect.gameObject.SetActive(true);
        //dimPanel.alpha = 0f;
        //dimPanel.DOFade(0.7f, 1.5f);

        //if (isWin)
        //{
        //    resultText.text = "WIN!";
        //    resultText.color = new Color(0.2f, 0.6f, 1f);

        //    // 1. 위치를 처음부터 '정중앙(0, 0)'으로 고정
        //    resultTextRect.anchoredPosition = Vector2.zero;

        //    // 2. 크기를 0으로 만들어서 아예 안 보이게
        //    resultTextRect.localScale = Vector3.zero;

        //    // 3. 0.5초 만에 크기 1로 '빡!' 하고 튕겨 나오듯 커지게
        //    resultTextRect.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        //}
        //else
        //{
        //    resultText.text = "LOSE...";
        //    resultText.color = Color.red;

        //    // 1. 크기는 원래 크기(1)로 고정
        //    resultTextRect.localScale = Vector3.one;

        //    // 2. 위치를 화면 밖 하늘 위(Y: 1000)로 올리기
        //    resultTextRect.anchoredPosition = new Vector2(0, 1000f);

        //    // 3. 1.5초 동안 중앙으로 고무공처럼 무겁게 쿵! 쿵! 떨어짐
        //    resultTextRect.DOAnchorPosY(0, 1.5f).SetEase(Ease.OutBounce);
        //}

        wasBattleWon = isWin;

        // TODO: Test용 삭제 필요
        // StartCoroutine(EnableInputAfterDelay(1.5f));
    }

    // TODO: Test용 삭제 필요
    //private IEnumerator EnableInputAfterDelay(float delay)
    //{
    //    yield return new WaitForSeconds(delay);
    //    isWaitingForAnyKey = true;
    //}

    private void OnDestroy()
    {
        BattleEvents.OnHealthChanged -= HandleHealthChanged;
        BattleEvents.OnBreakOccurred -= HandleBreakOccurred;
        BattleEvents.OnBattleEnded -= HandleBattleEnded;
    }
}
