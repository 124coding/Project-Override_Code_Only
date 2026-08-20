using System.Collections.Generic;
using UnityEngine;

public class CharacterClickable : MonoBehaviour
{
    private CharacterStatus myStatus;
    public GameObject targetIndicator;
    public GameObject targetSelectedIndicator;

    private void Awake() { myStatus = GetComponent<CharacterStatus>(); }
    private void Start()
    {
        targetIndicator.SetActive(false);
        targetSelectedIndicator.SetActive(false);
    }

    private void OnEnable() { 
        BattleEvents.OnTargetingStateChanged += ToggleIndicator;
        BattleEvents.OnCharacterHovered += IndicatorOff;
    }
    private void OnDisable() { 
        BattleEvents.OnTargetingStateChanged -= ToggleIndicator;
        BattleEvents.OnCharacterHovered -= IndicatorOff;
    }

    private void ToggleIndicator(bool isOn, TargetType targetType, List<CharacterStatus> validTargets)
    {
        // 끄라는 방송이거나, 내가 죽어있거나, 전달받은 명단이 비어있으면 무조건 끕니다.
        if (!isOn || myStatus.Speed <= 0 || validTargets == null)
        {
            targetIndicator.SetActive(false);
            targetSelectedIndicator.SetActive(false);
            return;
        }

        bool isMatch = validTargets.Contains(myStatus);
         
        // 명단에 내 이름이 있으면 화살표를 켭니다!
        targetIndicator.SetActive(isMatch);
    }
    private void IndicatorOff(CharacterStatus status)
    {
        if (status != myStatus)
        {
            targetSelectedIndicator.SetActive(false);
        }
    }


    public void OnMouseDown()
    {
        // [핵심 방어코드] 내 머리 위에 화살표가 켜져 있을 때만 클릭이 먹히게 합니다!
        // 이렇게 하면 공격할 땐 아군이 안 눌러지고, 힐 할 땐 적군이 안 눌러집니다.
        if (targetIndicator.activeSelf)
        {
            Debug.Log($"[{gameObject.name}] 클릭됨!");
            BattleEvents.OnCharacterClicked?.Invoke(myStatus);
            BattleEvents.OnTargetingStateChanged?.Invoke(false, TargetType.All, null);
            targetSelectedIndicator.SetActive(false);
        }
    }
    public void OnMouseEnter()
    {
        if (targetIndicator.activeSelf)
        {
            Debug.Log($"[{gameObject.name}] 호버링!");
            targetSelectedIndicator.SetActive(true);
            BattleEvents.OnCharacterHovered?.Invoke(myStatus);
        }
    }
    public void OnMouseExit()
    {
        if (targetIndicator.activeSelf)
        {
            Debug.Log($"[{gameObject.name}] 호버링 나감!");
            targetSelectedIndicator.SetActive(false);
            BattleEvents.OnCharacterHoveredOut?.Invoke();
        }
    }
}
