using UnityEngine;

public class FieldCard : InteractObject
{
    public string cardId; // 인스펙터에서 "RedCard"처럼 입력

    protected override void LoadState(bool isActivated)
    {
        if (isActivated)
        {
            gameObject.SetActive(false);
        }
    }

    protected override void OnInteraction()
    {
        // DataManager에 카드 ID 전달
        DataManager.Instance.AddCard(uniqueID);

        DataManager.Instance.SetObjectState(uniqueID, true);

        // 획득했으니 필드에서 삭제
        gameObject.SetActive(false);
    }
}