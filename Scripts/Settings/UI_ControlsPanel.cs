using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public struct KeybindingGroup
{
    public string groupName;
    public List<KeybindData> keybinds;
}

[System.Serializable]
public struct KeybindData
{
    public string displayName; // 화면에 보일 이름 (예: "앞으로 이동")
    public string actionPath;  // Input System 경로 (예: "Field/Move")
    public int bindingIndex;   // 바인딩 인덱스 (예: 상=1, 하=2)
}

public class UI_ControlsPanel : MonoBehaviour
{
    public Transform contentParent; // ScrollView의 Content
    public GameObject headerPrefab; // 카테고리 제목 프리팹
    public GameObject itemPrefab;   // 키 설정 슬롯 프리팹 (KeybindSlotUI)

    // 인스펙터에서 설정할 카테고리별 키 데이터
    public List<KeybindingGroup> bindingGroups;

    private void Start()
    {
        Invoke(nameof(InitializeUI), 0.1f);
    }

    private void InitializeUI()
    {
        foreach (Transform child in contentParent) { Destroy(child.gameObject); }

        // 데이터에 맞춰 UI 생성
        foreach (var group in bindingGroups)
        {
            // 헤더 생성
            GameObject headerObj = Instantiate(headerPrefab, contentParent);
            headerObj.GetComponentInChildren<TextMeshProUGUI>().text = group.groupName;

            // 해당 그룹의 키 슬롯들 생성
            foreach (var keyData in group.keybinds)
            {
                GameObject itemObj = Instantiate(itemPrefab, contentParent);
                KeybindSlotUI slotUI = itemObj.GetComponent<KeybindSlotUI>();

                slotUI.Setup(keyData.displayName, keyData.actionPath, keyData.bindingIndex);
            }
        }
    }
}