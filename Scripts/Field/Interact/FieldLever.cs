using System.Collections.Generic;
using UnityEngine;

public class FieldLever : InteractObject
{
    [Header("Target Objects (레버 당길 때)")]
    public GameObject[] turnOnTargets;
    public GameObject[] turnOffTargets;

    private List<IWorkObject> onObjectsList = new List<IWorkObject>();
    private List<IWorkObject> offObjectsList = new List<IWorkObject>();

    [Header("일회용 상호작용 설정")]
    [SerializeField] private bool canInteractAgain = false; // 기본은 false
    private bool isAlreadyInteracted = false;

    public Sprite onSprite;
    public Sprite offSprite;
    private SpriteRenderer sr;

    private bool isPulled = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ExtractWorkObjects(turnOnTargets, onObjectsList);
        ExtractWorkObjects(turnOffTargets, offObjectsList);
    }

    private void ExtractWorkObjects(GameObject[] targets, List<IWorkObject> list)
    {
        foreach (GameObject obj in targets)
        {
            if (obj != null)
            {
                IWorkObject workObj = obj.GetComponent<IWorkObject>();
                if (workObj != null) list.Add(workObj);
            }
        }
    }

    // 부모 클래스의 Interact()를 덮어씌워 레버 작동 로직을 일원화합니다!
    protected override void OnInteraction()
    {
        if (isAlreadyInteracted && !canInteractAgain)
        {
            Debug.Log($"{gameObject.name}은(는) 이미 작동했습니다.");
            return;
        }

        isAlreadyInteracted = true;

        if(!canInteractAgain) GetComponent<Collider2D>().enabled = false;

         // 부모 기본 로직 실행 (필요시)

        isPulled = !isPulled; // 상태 반전

        sr.sprite = isPulled ? onSprite : offSprite;

        if (isPulled)
        {
            DataManager.Instance.SetObjectState(uniqueID, true);
            foreach (var obj in onObjectsList) obj.WorkOn();
            foreach (var obj in offObjectsList) obj.WorkOff();
        }
        else
        {
            foreach (var obj in onObjectsList) obj.WorkOff();
            foreach (var obj in offObjectsList) obj.WorkOn();
        }

        Debug.Log($"{gameObject.name} 레버 상호작용 성공!");
    }

    protected override void LoadState(bool isActivated)
    {
        if (isActivated)
        {
            isPulled = true;
        }
    }

    public void ForceReset()
    {
        isPulled = false;
        sr.sprite = offSprite;
    }
}