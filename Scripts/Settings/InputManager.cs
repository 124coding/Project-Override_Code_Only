using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
    // 싱글톤
    public static InputManager Instance { get; private set; }

    public PlayerControls inputActions;

    // 글로벌 키 전용 이벤트 방송
    public Action OnToggleMenuPressed;

    // 현재 진행 중인 키 변경 작업을 추적하는 변수
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            inputActions = new PlayerControls();

            LoadBindingOverrides();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();

        // 글로벌 입력은 언제나 작동
        inputActions.Global.ToggleMenu.performed += OnToggleMenu;

        // 기본 필드 탐험 모드
        if (GameStateManager.Instance != null)
        {
            GameEvents.OnStateChanged += HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        inputActions.Disable();

        inputActions.Global.ToggleMenu.performed -= OnToggleMenu;

        if (GameStateManager.Instance != null)
        {
            GameEvents.OnStateChanged -= HandleStateChanged;
        }
    }

    private void OnToggleMenu(InputAction.CallbackContext context)
    {
        GameStateManager.Instance.TogglePause();
    }

    public void SwitchActionMap(string mapName)
    {
        DisableGameplayInput();

        if (mapName == "Global")
        {
            Debug.Log("[InputManager] 입력 모드 변경: Global (일시정지 중)");
            return;
        }

        // targetMap을 이름으로 찾아서 켬
        InputActionMap targetMap = inputActions.asset.FindActionMap(mapName);

        if (targetMap != null)
        {
            targetMap.Enable();
            Debug.Log($"[InputManager] 입력 모드 변경: {mapName}");
        }
        else
        {
            Debug.LogError($"[InputManager] {mapName} 맵을 찾을 수 없습니다!");
        }
    }

    private void HandleStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Field:
                SwitchActionMap("Field");
                break;
            case GameState.Battle:
                SwitchActionMap("Battle");
                break;
            case GameState.Cutscene:
                SwitchActionMap("Cutscene"); // 컷신 중에는 아무것도 못하게 빈 맵이나 UI 맵으로
                break;
            case GameState.Paused:
                SwitchActionMap("Global"); // 일시정지 중
                break;
                // 퍼즐, 에이밍은 특정 기믹 오브젝트와 상호작용 시 직접 SwitchActionMap 호출
        }
    }

    public void StopPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    public void DisableGameplayInput()
    {
        inputActions.asset.Disable();
        inputActions.Global.Enable();

        Debug.Log("[InputManager] 모든 게임 플레이 입력 차단 (사망 등)");
    }

    public string GetCurrentKeyName(string actionPath, int bindingIndex)
    {
        var action = inputActions.asset.FindAction(actionPath);
        if (action == null) return "None";

        // 현재 할당된 키보드 이름을 예쁘게 문자열로 뽑아줍니다.
        return InputControlPath.ToHumanReadableString(
            action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    /// <summary>
    /// UI에서 호출할 키 변경 함수
    /// 예시: RebindKey("Field/Jump", 0, (newKey) => text.text = newKey);
    /// </summary>
    /// <param name="actionPath">바꿀 액션의 경로 (예: "Field/Jump")</param>
    /// <param name="bindingIndex">바꿀 키의 인덱스 (일반 버튼은 0, WASD 같은 복합키는 Up=1, Down=2 등)</param>
    /// <param name="onRebindComplete">키 변경이 완료되었을 때 UI 텍스트를 바꿔줄 콜백 함수</param>
    public void RebindKey(string actionPath, int bindingIndex, Action<string> onRebindComplete)
    {
        // 바꿀 액션을 찾습니다.
        InputAction actionToRebind = inputActions.asset.FindAction(actionPath);
        if (actionToRebind == null)
        {
            Debug.LogError($"[InputManager] {actionPath} 액션을 찾을 수 없습니다!");
            return;
        }

        // 키를 변경하는 동안에는 해당 액션이 오작동하지 않도록 잠시 끕니다.
        actionToRebind.Disable();

        rebindingOperation = actionToRebind.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse") // 마우스 클릭으로 엉뚱하게 바뀌는 것 방지
            .WithCancelingThrough("<Keyboard>/escape") // ESC를 누르면 변경 취소
            .OnComplete(operation =>
            {
                // [키 변경 성공 시]
                operation.Dispose(); // 메모리 정리
                actionToRebind.Enable(); // 다시 켜기

                // 바뀐 키보드 이름 문자열
                string newKeyName = InputControlPath.ToHumanReadableString(
                    actionToRebind.bindings[bindingIndex].effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);

                onRebindComplete?.Invoke(newKeyName);

                // 기기에 즉시 저장
                SaveBindingOverrides();
            })
            .OnCancel(operation =>
            {
                // [ESC 눌러서 취소 시]
                operation.Dispose();
                actionToRebind.Enable();
            })
            .Start(); // 대기 시작!
    }

    // 바뀐 키보드 세팅을 JSON 문자열로 변환해서 기기에 영구 저장
    private void SaveBindingOverrides()
    {
        string rebinds = inputActions.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("CustomKeyBindings", rebinds);
        PlayerPrefs.Save();
        Debug.Log("[InputManager] 키 바인딩 커스텀 세팅 저장 완료!");
    }

    // 게임 시작 시, 저장된 JSON 문자열이 있다면 불러와서 덮어씌웁니다.
    private void LoadBindingOverrides()
    {
        if (PlayerPrefs.HasKey("CustomKeyBindings"))
        {
            string rebinds = PlayerPrefs.GetString("CustomKeyBindings");
            inputActions.asset.LoadBindingOverridesFromJson(rebinds);
        }
    }

    // TODO: 삭제 필요
    public bool GetFieldReset() => inputActions.Field.TestReset.WasPressedThisFrame();
    public bool GetFieldCheat() => inputActions.Field.TestCheat.WasPressedThisFrame();

}
