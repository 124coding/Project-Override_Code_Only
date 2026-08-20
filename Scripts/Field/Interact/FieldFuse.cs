using UnityEngine;

public class FieldFuse : InteractObject
{
    protected override void LoadState(bool isActivated)
    {
        if (isActivated)
        {
            gameObject.SetActive(false);
        }
    }

    protected override void OnInteraction()
    {
        DataManager.Instance.AddFuse();

        DataManager.Instance.SetObjectState(uniqueID, true);

        // 획득했으니 필드에서 삭제
        gameObject.SetActive(false);
    }
}