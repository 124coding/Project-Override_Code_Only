using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public AudioMixer mainMixer;

    [Header("Audio Mixer Groups")]
    public AudioMixerGroup bgmGroup; // AudioMixer의 BGM 그룹
    public AudioMixerGroup vfxGroup; // AudioMixer의 VFX 그룹

    private AudioSource bgmSource;
    private AudioSource vfxSource;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitAudioSources()
    {
        // BGM 전용 AudioSource 생성
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.outputAudioMixerGroup = bgmGroup;
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        // VFX 전용 AudioSource 생성
        vfxSource = gameObject.AddComponent<AudioSource>();
        vfxSource.outputAudioMixerGroup = vfxGroup;
        vfxSource.loop = false;
        vfxSource.playOnAwake = false;
    }

    #region BGM Play & Stop (기존 로직 유지)
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.Play();

        // BGM을 새로 틀 때는 볼륨이 정상(설정된 값)이어야 하므로 믹서 볼륨을 동기화해줍니다.
        SyncMixerVolume();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }
    #endregion

    #region Fade 기능 추가

    public void FadeInBGM(AudioClip clip, float duration = 1.0f)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        // 페이드 시작 전, 볼륨을 최하로 낮추고 음악을 틉니다.
        mainMixer.SetFloat("BGM", Mathf.Log10(0.0001f) * 20f);
        bgmSource.clip = clip;
        bgmSource.Play();

        // 목표 볼륨은 PlayerPrefs에 저장된 유저 세팅값 (없으면 최대치 1)
        float targetVolume = PlayerPrefs.GetFloat("BGM_Vol", 1.0f);

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(StartFade(targetVolume, duration));
    }

    public void FadeOutBGM(float duration = 1.0f)
    {
        if (!bgmSource.isPlaying) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        // 볼륨을 0으로 보냅니다.
        fadeCoroutine = StartCoroutine(StartFade(0f, duration, true));
    }

    private IEnumerator StartFade(float targetVolumePercent, float duration, bool stopAfterFade = false)
    {
        float currentTime = 0;

        // 현재 믹서의 데시벨(dB) 값을 가져와서 퍼센트(0~1)로 변환
        mainMixer.GetFloat("BGM", out float currentDb);
        float startVolumePercent = Mathf.Pow(10, currentDb / 20f);

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;

            // 시작 볼륨부터 목표 볼륨까지 부드럽게 보간
            float newVolPercent = Mathf.Lerp(startVolumePercent, targetVolumePercent, currentTime / duration);

            // 퍼센트 값을 오디오 믹서 전용 데시벨 값으로 변환해서 적용
            // (0이 들어오면 에러가 나므로 매우 작은 값인 0.0001f 사용)
            float newDb = Mathf.Log10(Mathf.Max(newVolPercent, 0.0001f)) * 20f;
            mainMixer.SetFloat("BGM", newDb);

            yield return null;
        }

        // 끝났으면 목표 볼륨에 확실히 고정
        mainMixer.SetFloat("BGM", Mathf.Log10(Mathf.Max(targetVolumePercent, 0.0001f)) * 20f);

        // 페이드 아웃이었다면, 소리가 0이 된 후 노래를 아예 정지시킴
        if (stopAfterFade)
        {
            bgmSource.Stop();
        }
    }

    // 설정 창 등에서 볼륨이 바뀌었을 때 현재 BGM 볼륨을 저장된 설정값으로 맞춤
    private void SyncMixerVolume()
    {
        float savedVol = PlayerPrefs.GetFloat("BGM_Vol", 1.0f);
        mainMixer.SetFloat("BGM", Mathf.Log10(Mathf.Max(savedVol, 0.0001f)) * 20f);
    }
    #endregion

    #region SFX 기능
    /// 효과음 1회 재생
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        vfxSource.PlayOneShot(clip);
    }

    /// 볼륨 배율을 직접 지정하여 효과음 재생 (기본 1.0f)
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;

        vfxSource.PlayOneShot(clip, volumeScale);
    }
    #endregion
}