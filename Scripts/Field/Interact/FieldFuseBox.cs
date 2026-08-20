using UnityEngine;

public class FieldFuseBox : InteractObject
{
    [Header("Settings")]
    public FusePuzzleManager puzzleManager; // 이 구역의 퍼즐 매니저
    
    private SpriteRenderer sr;
    private bool isInserted = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    protected override void LoadState(bool isActivated)
    {
        if (isActivated)
        {
            isInserted = true;
            sr.color = Color.white;

            if (puzzleManager != null)
            {
                puzzleManager.OnFuseLoaded();
            }
        }
    }

    protected override void OnInteraction()
    {
        if (isInserted) return;

        if (!DataManager.Instance.HasFuse()) return;

        DataManager.Instance.ConsumeFuse();
        isInserted = true;

        sr.color = Color.white;

        DataManager.Instance.SetObjectState(uniqueID, true);

        if (puzzleManager != null)
        {
            puzzleManager.OnFuseInserted();
        }
    }
}
