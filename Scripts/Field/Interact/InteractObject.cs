using UnityEngine;

public abstract class InteractObject : MonoBehaviour
{
    public string uniqueID;

    public bool canInteractByBullet = false;

    public bool isItem = false;

    [Header("Floating Settings")]
    public float floatSpeed = 3f;       // 위아래로 움직이는 속도 (클수록 빠름)
    public float floatAmplitude = 0.15f; // 위아래로 움직이는 폭 (단위: 미터/격자칸)

    private Vector3 startPos;
    private float timeOffset;

    protected virtual void Start()
    {
        startPos = transform.position;
        timeOffset = Random.Range(0f, 100f);

        // 씬 로드 시 DataManager에게 내 상태 물어보기
        bool savedState = DataManager.Instance.GetObjectState(uniqueID);
        LoadState(savedState);
    }

    protected virtual void Update()
    {
        if (isItem)
        {
            // Mathf.Sin은 -1.0f ~ 1.0f 사이를 부드럽게 오갑니다.
            float newY = startPos.y + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmplitude;

            // X, Z 좌표는 고정하고 Y 좌표만 실시간 계산된 값으로 변경
            transform.position = new Vector3(startPos.x, newY, startPos.z);
        }
    }

    // 플레이어나 총알이 호출하면 실행되는 '공통 창구'
    public void ExecuteInteraction()
    {
        // 자식 클래스에서 이 함수를 재정의(override)하여 내용을 채움
        OnInteraction();
    }

    // 총알 상호작용용
    public void InteractByBullet()
    {
        if (canInteractByBullet) ExecuteInteraction();
    }

    // 자식들이 구현할 핵심 로직
    protected abstract void OnInteraction();

    protected abstract void LoadState(bool isActivated);
}