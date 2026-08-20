using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class TutorialInitializer : MonoBehaviour
{
    [Header("Cutscene & Tutorial Settings")]
    [SerializeField] private PlayableDirector introCutsceneDirector;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 인트로 컷신 실행!
        Debug.Log("[SceneInit] 신규 유저 진입! 인트로 컷신을 시작합니다.");

        // 컷신이 다 끝나면 'OnIntroCutsceneFinished' 함수를 실행하라고 넘겨줌
        CutsceneManager.Instance.PlayCutscene(introCutsceneDirector, OnIntroCutsceneFinished);
    }

    private void OnIntroCutsceneFinished()
    {
        Debug.Log("[SceneInit] 인트로 컷신 종료. 튜토리얼 전투 진행!");

        // 전투 상태로 변경 및 튜토리얼 전투 컨트롤러 가동
        GameStateManager.Instance.ChangeState(GameState.Battle);
        SceneManager.LoadScene("TutorialBattle");
    }
}
