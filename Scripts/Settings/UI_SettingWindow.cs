using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_SettingWindow : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button btn_AudioTab;
    public Button btn_GraphicsTab;
    public Button btn_ControlsTab;

    [Header("Tabs")]
    public GameObject audioPanel;
    public GameObject graphicsPanel;
    public GameObject controlsPanel;

    [Header("Audio Settings UI")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider vfxVolumeSlider;

    [Header("Audio Settings UI (Input Fields)")]
    public TMP_InputField masterVolumeInput;
    public TMP_InputField bgmVolumeInput;
    public TMP_InputField vfxVolumeInput;

    [Header("Graphics Settings UI")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    [Header("Bottom Buttons")]
    public Button saveButton;
    public Button closeButton;

    // 지원할 해상도 목록
    private List<Vector2Int> supportedResolutions = new List<Vector2Int>()
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1600, 900),
        new Vector2Int(1280, 720)
    };

    private void Start()
    {
        btn_AudioTab.onClick.AddListener(() => OpenTab(audioPanel));
        btn_GraphicsTab.onClick.AddListener(() => OpenTab(graphicsPanel));
        btn_ControlsTab.onClick.AddListener(() => OpenTab(controlsPanel));

        // 하단 버튼 이벤트 연결
        saveButton.onClick.AddListener(SaveSettings);
        closeButton.onClick.AddListener(CloseWindow);

        // 오디오 슬라이더 실시간 이벤트 연
        // 슬라이더를 드래그할 때마다 즉각적으로 매니저에 값을 보내서 소리 크기가 바로 바뀌게 합니다.
        BindVolumeUI(masterVolumeSlider, masterVolumeInput, SettingsManager.Instance.SetMasterVolume);
        BindVolumeUI(bgmVolumeSlider, bgmVolumeInput, SettingsManager.Instance.SetBGMVolume);
        BindVolumeUI(vfxVolumeSlider, vfxVolumeInput, SettingsManager.Instance.SetSFXVolume);
    }

    private void OnEnable()
    {
        // 창이 열릴 때, SettingManager가 들고 있는 현재 세팅값을 UI에 덮어씌움
        LoadCurrentSettingsToUI();

        // 기본 탭 열기 (예: 오디오 탭)
        OpenTab(audioPanel);
    }

    private void BindVolumeUI(Slider slider, TMP_InputField inputField, Action<float> onVolumeChange)
    {
        // 슬라이더 드래그 중 -> 실시간으로 숫자만 바뀜 (예: 50)
        slider.onValueChanged.AddListener(val =>
        {
            onVolumeChange(val);

            // 유저가 입력창을 직접 클릭해서 타이핑 중이 아닐 때만 슬라이더 값 반영
            if (!inputField.isFocused)
            {
                inputField.SetTextWithoutNotify(Mathf.RoundToInt(val * 100f).ToString());
            }
        });

        // 슬라이더 손을 뗐을 때 -> 자동으로 % 붙이기
        EventTrigger trigger = slider.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = slider.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entry.callback.AddListener((data) =>
        {
            if (!inputField.isFocused)
            {
                int percent = Mathf.RoundToInt(slider.value * 100f);
                inputField.SetTextWithoutNotify($"{percent}%");
            }
        });
        trigger.triggers.Add(entry);

        // 입력창을 직접 클릭(포커스)했을 때 -> 지우거나 수정하기 편하게 % 제거
        inputField.onSelect.AddListener(text =>
        {
            string cleanText = text.Replace("%", "").Trim();
            inputField.SetTextWithoutNotify(cleanText);
        });

        // 입력창 수정 완료 (엔터 / 다른 곳 클릭) -> 값 적용 후 '%' 붙이기
        inputField.onEndEdit.AddListener(text =>
        {
            string cleanText = text.Replace("%", "").Trim();

            if (float.TryParse(cleanText, out float percentage))
            {
                float val = Mathf.Clamp(percentage, 0f, 100f) / 100f;

                slider.SetValueWithoutNotify(val);
                onVolumeChange(val);

                int percent = Mathf.RoundToInt(val * 100f);
                inputField.SetTextWithoutNotify($"{percent}%");
            }
            else
            {
                // 글자를 잘못 입력한 경우 기존 슬라이더 값으로 원상복구 후 '%' 추가
                int percent = Mathf.RoundToInt(slider.value * 100f);
                inputField.SetTextWithoutNotify($"{percent}%");
            }
        });
    }

    private void LoadCurrentSettingsToUI()
    {
        // 오디오 UI 갱신
        float masterVol = PlayerPrefs.GetFloat("Master_Vol", 1.0f);
        masterVolumeSlider.SetValueWithoutNotify(masterVol);
        masterVolumeInput.SetTextWithoutNotify($"{Mathf.RoundToInt(masterVol * 100f)}%");

        float bgmVol = PlayerPrefs.GetFloat("BGM_Vol", 1.0f);
        bgmVolumeSlider.SetValueWithoutNotify(bgmVol);
        bgmVolumeInput.SetTextWithoutNotify($"{Mathf.RoundToInt(bgmVol * 100f)}%");

        float sfxVol = PlayerPrefs.GetFloat("SFX_Vol", 1.0f);
        vfxVolumeSlider.SetValueWithoutNotify(sfxVol);
        vfxVolumeInput.SetTextWithoutNotify($"{Mathf.RoundToInt(sfxVol * 100f)}%");

        // 그래픽 UI 갱신
        fullscreenToggle.isOn = PlayerPrefs.GetInt("FullScreen", 1) == 1;

        int savedWidth = PlayerPrefs.GetInt("Res_Width", 1920);
        int savedHeight = PlayerPrefs.GetInt("Res_Height", 1080);

        // 드롭다운 메뉴 초기화 및 세팅
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < supportedResolutions.Count; i++)
        {
            options.Add($"{supportedResolutions[i].x} x {supportedResolutions[i].y}");

            // 저장된 해상도와 일치하는 인덱스 찾기
            if (supportedResolutions[i].x == savedWidth && supportedResolutions[i].y == savedHeight)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void SaveSettings()
    {
        SettingsManager.Instance.SetFullScreen(fullscreenToggle.isOn);

        Vector2Int selectedRes = supportedResolutions[resolutionDropdown.value];
        SettingsManager.Instance.SetResolution(selectedRes.x, selectedRes.y);

        CloseWindow();
    }

    public void CloseWindow()
    {
        gameObject.SetActive(false);
    }

    // 탭 전환 버튼에서 OnClick() 이벤트로 호출할 함수
    public void OpenTab(GameObject tabToOpen)
    {
        audioPanel.SetActive(false);
        graphicsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        tabToOpen.SetActive(true);
    }
}