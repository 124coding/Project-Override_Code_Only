using UnityEngine;
using UnityEngine.Audio;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("연결된 시스템")]
    public AudioMixer mainAudioMixer; // 에디터에서 MainMixer를 끌어다 넣으세요.

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 설정은 유지되어야 함
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임이 켜지자마자 기기(PC)에 저장된 세팅값을 불러와서 적용합니다.
        LoadSettings();
    }

    // 오디오 설정 (UI의 슬라이더 값 0.0001 ~ 1.0 을 받습니다)
    public void SetMasterVolume(float volume)
    {
        // 유니티 AudioMixer는 데시벨(dB) 단위이므로 로그 계산
        float decibel = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        mainAudioMixer.SetFloat("Master", decibel);
        PlayerPrefs.SetFloat("Master_Vol", volume);
    }

    public void SetBGMVolume(float volume)
    {
        float decibel = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        mainAudioMixer.SetFloat("BGM", decibel);
        PlayerPrefs.SetFloat("BGM_Vol", volume);
    }

    public void SetSFXVolume(float volume)
    {
        float decibel = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        mainAudioMixer.SetFloat("SFX", decibel);
        PlayerPrefs.SetFloat("SFX_Vol", volume);
    }

    // 그래픽/디스플레이 설정
    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        PlayerPrefs.SetInt("FullScreen", isFullScreen ? 1 : 0);
    }

    // 해상도 변경 (예: width 1920, height 1080)
    public void SetResolution(int width, int height)
    {
        Screen.SetResolution(width, height, Screen.fullScreen);
        PlayerPrefs.SetInt("Res_Width", width);
        PlayerPrefs.SetInt("Res_Height", height);
    }

    // 키 바인딩 설정
    public void StartRebinding(string actionPath, int bindingIndex, Action<string> onRebindComplete)
    {
        InputManager.Instance.RebindKey(actionPath, bindingIndex, onRebindComplete);
    }

    // 기기에서 세팅값 불러오기
    public void LoadSettings()
    {
        // 오디오 로드 (기본값 1.0f = 100% 소리)
        SetMasterVolume(PlayerPrefs.GetFloat("Master_Vol", 1.0f));
        SetBGMVolume(PlayerPrefs.GetFloat("BGM_Vol", 1.0f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFX_Vol", 1.0f));

        // 창모드/전체화면 로드 (기본값 1 = 전체화면)
        bool isFull = PlayerPrefs.GetInt("FullScreen", 1) == 1;
        SetFullScreen(isFull);

        // 해상도 로드 (기본값 1920x1080)
        int width = PlayerPrefs.GetInt("Res_Width", 1920);
        int height = PlayerPrefs.GetInt("Res_Height", 1080);
        SetResolution(width, height);
    }
}