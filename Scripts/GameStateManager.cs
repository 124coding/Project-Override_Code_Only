using UnityEngine;
using System;

public enum GameState
{
    Title,       // 타이틀 화면
    Field,     // 일반 플레이 중 (조작 가능)
    Battle,      // 배틀 중
    Cutscene,    // 컷신 진행 중 (플레이어 조작 불가, 적 AI 정지)
    Paused       // 일시정지
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Title;

    private GameState stateBeforePause;

    private void Awake()
    {
        // Bootstrap 씬에서 단 하나만 존재하도록 싱글톤 및 씬 전환 파괴 방지 처리
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"[GameState] 상태 변경 -> {newState}");

        // 상태가 변했다고 사방에 전파
        GameEvents.OnStateChanged?.Invoke(newState);
    }

    public void TogglePause()
    {
        // 이미 일시정지 상태라면 -> 원래 상태로 복구 (Unpause)
        if (CurrentState == GameState.Paused)
        {
            Time.timeScale = 1f; // 유니티 시간 다시 흐르게 하기
            ChangeState(stateBeforePause); // 정지 전 상태(Field 또는 Battle)로 복구
            Debug.Log($"[Pause] 일시정지 해제. 복구된 상태: {stateBeforePause}");
        }
        // 필드나 전투 중에 ESC를 눌렀다면 -> 일시정지 (Pause)
        else if (CurrentState == GameState.Field || CurrentState == GameState.Battle)
        {
            stateBeforePause = CurrentState; // 현재 상태 기억해두기
            Time.timeScale = 0f; // 유니티 시간 전면 정지 (물리, 애니메이션, Update 일부 정지)
            ChangeState(GameState.Paused);
            Debug.Log($"[Pause] 게임 일시정지. (이전 상태: {stateBeforePause})");
        }
        // TODO: 컷신(Cutscene) 중에는 기획에 따라 막거나 허용할 수 있습니다. (우선은 필드/전투만 허용)
    }
}