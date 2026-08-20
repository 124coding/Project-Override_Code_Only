using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class Dialogue
{
    [TextArea(3, 5)]
    public string text;           // 출력할 대사/문장
    public float typingSpeed = 0.05f; // 글자 출력 속도 (타자기 연출용)
    public float autoNextDelay = 2.0f; // 출력이 끝난 후 다음 문장으로 넘어가기 전 대기 시간
}

public enum DialogueEndAction
{
    LoadNextScene,  // 프롤로그용: 끝나면 다음 씬으로 이동
    CloseUI         // 인게임용: 끝나면 UI만 끄고 게임 계속 진행
}

public class DialogueManager : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI guideText;

    [Header("Intro Data")]
    [SerializeField] private List<Dialogue> sentences = new List<Dialogue>();

    [Header("Settings")]
    [SerializeField] private bool useTypewriterEffect = true; // true: 타자기, false: 페이드 연출
    [SerializeField] private bool autoAdvance = false;

    [Header("Pacing Settings")]
    [Tooltip("UI가 켜지고 첫 대사 타이핑이 시작되기 전 대기하는 시간")]
    [SerializeField] private float initialDelay = 0.8f;

    [Tooltip("true일 경우, 유저가 첫 Z키를 눌러야 대사가 시작됩니다.")]
    [SerializeField] private bool waitForFirstInput = false;

    [Header("End Action Setting")]
    public DialogueEndAction endAction = DialogueEndAction.CloseUI;
    public string nextSceneName = "FieldScene"; // LoadNextScene일 때만 사용

    private Coroutine currentRoutine;
    private bool isTyping = false;
    private bool isWaitingForClick = false;
    private string currentFullText = "";

    // 오브젝트가 켜질 때 (대사 시작 시) 이벤트 연결
    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.inputActions.Cutscene.Skip.performed += OnInteractInput;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.inputActions.Cutscene.Skip.performed -= OnInteractInput;
        }
    }

    private void Start()
    {
        dialogueText.text = "";
        if (guideText != null) guideText.text = "";

        if (endAction == DialogueEndAction.LoadNextScene)
        {
            StartDialogueSequence(sentences);
        }
    }

    private void OnInteractInput(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (isTyping)
        {
            isTyping = false;
        }
        else if (isWaitingForClick)
        {
            // 글자가 다 나온 상태에서 대기 중이라면 -> 다음 대사로 넘기기
            isWaitingForClick = false;
        }
    }

    public void StartDialogueSequence(List<Dialogue> newSentences)
    {
        // 화면 켜기 (검은 배경 보이게)
        gameObject.SetActive(true);

        // 대사 덮어씌우기
        sentences = newSentences;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.StopPlayer(); // 플레이어 급정거
            GameStateManager.Instance.ChangeState(GameState.Cutscene);
        }

        // 코루틴 시작
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        if (waitForFirstInput)
        {
            if (guideText != null) guideText.text = "[Z] 시작";
            isWaitingForClick = true;
            while (isWaitingForClick)
            {
                yield return null;
            }
            if (guideText != null) guideText.text = "";
        }
        else
        {
            //시작 전 0.8초간의 숨 고르기 대기
            yield return new WaitForSeconds(initialDelay);
        }

        for (int i = 0; i < sentences.Count; i++)
        {
            var sentence = sentences[i];
            currentFullText = sentence.text;

            // 현재 문장이 마지막 문장인지 확인
            bool isLastSentence = (i == sentences.Count - 1);

            // 타이핑 또는 페이드 연출 실행
            if (useTypewriterEffect)
            {
                yield return StartCoroutine(TypewriterRoutine(sentence));
            }
            else
            {
                yield return StartCoroutine(FadeRoutine(sentence));
            }

            // 출력이 끝난 후 안내 텍스트(guideText) 상태 변경
            if (guideText != null)
            {
                if (isLastSentence)
                {
                    guideText.text = (endAction == DialogueEndAction.LoadNextScene) ? "[Z] 시작" : "[Z] 닫기";
                }
                else
                {
                    guideText.text = "[Z] 다음";
                }
            }

            // Z키 입력 대기 처리
            isWaitingForClick = true;

            if (autoAdvance)
            {
                // 시간제한 대기
                float timer = 0f;
                while (timer < sentence.autoNextDelay && isWaitingForClick)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                while (isWaitingForClick)
                {
                    yield return null;
                }
            }

            isWaitingForClick = false;
            dialogueText.text = "";
            if (guideText != null) guideText.text = "";

            yield return new WaitForSeconds(0.15f); // 문장 사이의 아주 짧은 텀
        }

        // 모든 문장이 끝나고 마지막 Z키까지 눌렸을 때 실행
        EndIntro();
    }

    // 타자기 연출 코루틴
    private IEnumerator TypewriterRoutine(Dialogue sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in sentence.text.ToCharArray())
        {
            if (!isTyping) break;

            dialogueText.text += letter;
            yield return new WaitForSeconds(sentence.typingSpeed);
        }

        dialogueText.text = sentence.text;
        isTyping = false;
    }

    private IEnumerator FadeRoutine(Dialogue sentence)
    {
        dialogueText.text = sentence.text;

        float elapsed = 0f;
        float fadeTime = 1.0f;

        while (elapsed < fadeTime && isTyping)
        {
            elapsed += Time.deltaTime;
            dialogueText.color = new Color(1, 1, 1, Mathf.Clamp01(elapsed / fadeTime));
            yield return null;
        }

        dialogueText.color = new Color(1, 1, 1, 1f);
        isTyping = false;
    }

    private void EndIntro()
    {
        if (InputManager.Instance != null && endAction == DialogueEndAction.CloseUI)
        {
            GameStateManager.Instance.ChangeState(GameState.Field);
        }

        if (endAction == DialogueEndAction.LoadNextScene)
        {
            Debug.Log($"[{nextSceneName}] 씬으로 이동합니다");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("대사 종료");
            // UI 전체를 꺼버려서 다시 원래 필드 화면이 보이게 함
            gameObject.SetActive(false);
        }
    }
}