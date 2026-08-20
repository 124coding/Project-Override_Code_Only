using UnityEngine;
public enum PopupPositionType
{
    // TODO: 위치 추가 각 튜토리얼 스텝에 맞게
    Center,             // 화면 중앙
    Top_TurnOrderUI,    // 턴 계산기 위치
    Bottom_SkillTarget, // 스킬/아이템 버튼 위
    Right_BossHP        // 보스 체력바 옆
}

[System.Serializable]
public struct TutorialPopupData
{
    [TextArea]
    public string message;              // 출력할 가이드 텍스트
    public PopupPositionType position;  // 어디에 띄울지
    public bool isMaskingEnabled;       // 주변 화면을 어둡게 할지 여부
}

public class TutorialUIManager : MonoBehaviour
{

    public void ShowGuidePopup(TutorialPopupData popupData)
    {
        // UI 개발자 작성 영역: 팝업 애니메이션 재생, 텍스트 할당 등
    }

    public void HideGuidePopup()
    {
        // UI 개발자 작성 영역: 팝업 닫기
    }

    public void FocusUIElement(RectTransform targetUI)
    {
        // UI 개발자 작성 영역: 나머지 화면 어둡게 하고 타겟 UI만 밝게 강조
    }

    public void ResetFocus()
    {
        // UI 개발자 작성 영역: 화면 어두운 효과 끄기
    }
}