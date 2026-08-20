using UnityEngine;

public class LaserMirror : InteractObject
{
    [Header("Settings")]
    public float rotationSpeed = 30f;

    public Sprite basicSprite;
    public Sprite highrightSprite;

    public LaserPuzzleManager myManager;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Rotate(float direction)
    {
        // direction: 1 또는 -1
        transform.Rotate(0, 0, direction * rotationSpeed * Time.deltaTime);
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            if (highrightSprite != null) sr.sprite = highrightSprite;
        }
        else
        {
            if (basicSprite != null) sr.sprite = basicSprite;
        }
    }

    protected override void OnInteraction()
    {
        // 거울이 직접 퍼즐 모드를 켜지 않고 매니저를 찾아서 실행
        if (myManager != null) myManager.StartPuzzle(this);
    }

    public void SetMirror()
    {
        DataManager.Instance.SetObjectState(uniqueID, true);
        DataManager.Instance.SetObjectRotation(uniqueID, transform.eulerAngles.z);
    }

    protected override void LoadState(bool isActivated)
    {
        if(isActivated)
        {
            float savedZ = DataManager.Instance.GetObjectRotation(uniqueID);
            transform.eulerAngles = new Vector3(0, 0, savedZ);
        }
    }
}