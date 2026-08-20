using UnityEngine;

public class FieldRelic : InteractObject
{
    public string relicId;
    public string relicName;
    public string relicDescription;

    protected override void LoadState(bool isActivated)
    {
        if(isActivated) gameObject.SetActive(false);
    }

    protected override void OnInteraction()
    {
        // DataManager에 카드 ID 전달
        DataManager.Instance.AddRelic(relicId);

        DataManager.Instance.SetObjectState(uniqueID, true);

        // 획득했으니 필드에서 삭제
        gameObject.SetActive(false);
    }
}