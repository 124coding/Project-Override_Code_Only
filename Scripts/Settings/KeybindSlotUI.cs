using UnityEngine;
using TMPro; // TextMeshPro 사용 시
using UnityEngine.UI;

public class KeybindSlotUI : MonoBehaviour
{
    [Header("바인딩 설정")]
    [Tooltip("예: Field/Jump 또는 Field/Move")]
    public string actionPath;

    [Tooltip("일반 버튼은 0, WASD(상하좌우)는 1, 2, 3, 4")]
    public int bindingIndex = 0;

    [Header("연결할 UI 컴포넌트")]
    public TextMeshProUGUI actionNameText;
    public TextMeshProUGUI keyNameText; // 현재 세팅된 키(예: "Space")를 보여줄 텍스트
    public Button rebindButton;         // 누르면 키 변경이 시작되는 버튼

    public void Setup(string displayName, string path, int index)
    {
        actionNameText.text = displayName;
        actionPath = path;
        bindingIndex = index;

        // 현재 설정된 키 이름 가져오기
        string currentKey = InputManager.Instance.GetCurrentKeyName(actionPath, bindingIndex);
        UpdateKeyText(currentKey);

        rebindButton.onClick.RemoveAllListeners();
        rebindButton.onClick.AddListener(OnRebindButtonClicked);
    }

    private void OnRebindButtonClicked()
    {
        keyNameText.text = "Wait..";

        rebindButton.interactable = false;

        SettingsManager.Instance.StartRebinding(actionPath, bindingIndex, (newKeyName) => 
        {
            UpdateKeyText(newKeyName);
            
            rebindButton.interactable = true; 
        });
    }

    // SettingsManager가 키 변경을 완료하면, 바뀐 키 이름을 여기에 넣어줍니다.
    private void UpdateKeyText(string newKeyName)
    {
        keyNameText.text = newKeyName;
    }
}