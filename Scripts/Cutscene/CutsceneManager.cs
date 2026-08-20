using UnityEngine;
using UnityEngine.Playables;
using System;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    private PlayableDirector currentDirector;
    private bool isWaitingForDialogue = false;

    private Action onCutsceneEndCallback;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 트리거에서 호출: 컷신 시작
    public void PlayCutscene(PlayableDirector director, Action onEndCallback = null)
    {
        if (director == null) return;

        currentDirector = director;
        onCutsceneEndCallback = onEndCallback;

        GameStateManager.Instance.ChangeState(GameState.Cutscene);
        GameEvents.OnCutsceneStart?.Invoke();

        currentDirector.stopped += OnDirectorStopped;
        currentDirector.Play();
    }

    // 타임라인 내 Signal에서 호출: 대화창 띄우기 (타임라인 일시정지)
    public void RequestDialogue(DialogueData data)
    {
        if (currentDirector == null) return;

        currentDirector.Pause();
        isWaitingForDialogue = true;

        // UI 매니저에게 데이터 전달
        GameEvents.OnRequestDialogue?.Invoke(data);
    }

    // UI 매니저가 대화가 끝났을 때 호출: 타임라인 재개
    public void ResumeCutscene()
    {
        if (!isWaitingForDialogue || currentDirector == null) return;

        isWaitingForDialogue = false;
        currentDirector.Play();
    }

    // 타임라인 종료 시 호출
    private void OnDirectorStopped(PlayableDirector director)
    {
        director.stopped -= OnDirectorStopped;
        GameStateManager.Instance.ChangeState(GameState.Field);
        GameEvents.OnCutsceneEnd?.Invoke();

        if (onCutsceneEndCallback != null)
        {
            onCutsceneEndCallback.Invoke();
            onCutsceneEndCallback = null; // 실행 후 안전하게 비워줍니다.
        }

        currentDirector = null;
    }

    // 스킵 기능 (타임라인 강제 종료)
    public void SkipCutscene()
    {
        if (currentDirector == null) return;
        currentDirector.time = currentDirector.duration;
        currentDirector.Evaluate();
    }
}