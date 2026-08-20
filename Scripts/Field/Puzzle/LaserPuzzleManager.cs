using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class LaserPuzzleManager : MonoBehaviour, IWorkObject
{
    public List<LaserMirror> mirrors;
    public CinemachineCamera puzzleRoomCam;
    public FieldLever linkedLever;

    private int currentIndex = 0;
    private bool isPuzzleActive = false;

    private InputAction interactAction;
    private InputAction previousAction;
    private InputAction nextAction;

    private float puzzleStartTime; // 퍼즐 시작 시간 저장용
    private const float INPUT_BUFFER_TIME = 0.2f; // 시작 후 0.2초간 입력 무시

    private void Start()
    {
        interactAction = InputManager.Instance.inputActions.Puzzle.Interact;
        previousAction = InputManager.Instance.inputActions.Puzzle.PreviousObject;
        nextAction = InputManager.Instance.inputActions.Puzzle.NextObject;
        
        foreach(var m in mirrors)
        {
            m.myManager = this;
        }
    }

    public void StartPuzzle(LaserMirror initialMirror)
    {
        Debug.Log("StartPuzzle");
        isPuzzleActive = true;
        InputManager.Instance.SwitchActionMap("Puzzle"); // 퍼즐 모드 전환
        puzzleRoomCam.Priority = 100;
        puzzleStartTime = Time.time; // 시작 시간 기록

        currentIndex = mirrors.IndexOf(initialMirror);
        UpdateSelection();
    }
    public void WorkOn()
    {
        if (mirrors.Count > 0) StartPuzzle(mirrors[0]);
    }

    public void WorkOff()
    {
        EndPuzzle();
    }

    private void Update()
    {
        if (!isPuzzleActive) return;

        if (Time.time - puzzleStartTime < INPUT_BUFFER_TIME) return;

        // 퍼즐 종료
        if (interactAction.WasPressedThisFrame()) EndPuzzle();

        // 거울 선택 전환 (좌우 화살표/이동키)
        if (nextAction.WasPressedThisFrame())
        {
            SwitchMirror(1);
        }
        else if (previousAction.WasPressedThisFrame())
        {
            SwitchMirror(-1);
        }

        // 거울 회전 (Q/E 키)
        // 직접적인 키 입력 체크 (InputManager에 해당 함수가 없다면 KeyCode 사용)
        float rotationInput = 0f;
        if (InputManager.Instance.inputActions.Puzzle.RotateCounterClockwise.IsPressed()) rotationInput = 1f;  // 반시계
        if (InputManager.Instance.inputActions.Puzzle.RotateClockwise.IsPressed()) rotationInput = -1f; // 시계

        if (rotationInput != 0f)
        {
            mirrors[currentIndex].Rotate(rotationInput);
        }
    }

    private void SwitchMirror(int direction)
    {
        currentIndex = (currentIndex + direction + mirrors.Count) % mirrors.Count;
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < mirrors.Count; i++)
        {
            mirrors[i].SetSelected(i == currentIndex);
        }
    }

    public void EndPuzzle()
    {
        Debug.Log("EndPuzzle");
        isPuzzleActive = false;
        InputManager.Instance.SwitchActionMap("Field");
        puzzleRoomCam.Priority = 0;
        foreach (var m in mirrors)
        {
            m.SetMirror();
            m.SetSelected(false);
        }

        if (linkedLever != null) linkedLever.ForceReset();
    }
}