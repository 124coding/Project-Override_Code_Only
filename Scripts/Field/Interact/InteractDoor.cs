using DG.Tweening; // DOTween 네임스페이스
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class InteractDoor : InteractObject
{
    [Header("Base Settings")]
    public List<string> requestCardIDs = new List<string>();

    [Header("Door Settings")]
    public float openDuration = 0.5f;
    public Ease moveEase = Ease.OutQuart;
    public Vector3 openOffset = new Vector3(0, 3f, 0);

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;

    // 1. 초기화 (부모의 Start보다 먼저 실행되거나 Awake에서 처리)
    private void Awake()
    {
        closedPos = transform.position;
        openPos = closedPos + openOffset;
    }

    // 2. 부모(InteractObject)의 Start에서 호출됨 (상태 로드용)
    //protected override void ApplyStoredState()
    //{
    //    // DataManager에서 불러온 상태가 true(열림)라면
    //    isOpen = true;
    //    transform.position = openPos; // 애니메이션 없이 바로 위치 고정
    //}

    // 3. 실제 상호작용 로직
    protected override void OnInteraction()
    {
        if (isOpen) return; // 이미 열려있으면 동작 안 함

        // 카드 확인
        foreach (var id in requestCardIDs)
        {
            if (!DataManager.Instance.HasCard(id))
            {
                Debug.Log($"필요 카드 부족: {id}");
                return;
            }
        }

        OpenDoor();
    }

    protected override void LoadState(bool isActivated)
    {
        if(isActivated)
        {
            isOpen = true;
            transform.position = openPos;
        }
    }

    private void OpenDoor()
    {
        isOpen = true;

        // 연출 실행
        transform.DOMove(openPos, openDuration)
                 .SetEase(moveEase)
                 .SetLink(gameObject);

        DataManager.Instance.SetObjectState(uniqueID, true);
    }
}