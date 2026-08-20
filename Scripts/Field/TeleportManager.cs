using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportManager : MonoBehaviour
{
    public static TeleportManager Instance;

    [Header("Encounter Settings")]
    public float fadeDuration = 1f;

    private void Awake()
    {
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

    public void TeleportPlayer(string targetSceneName, Vector3 targetPosition, Action onComplete = null)
    {
        InputManager.Instance.StopPlayer();
        InputManager.Instance.DisableGameplayInput();

        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == targetSceneName)
        {
            ExecuteLocalTeleport(targetPosition, onComplete);
        }
        else
        {
            StartCoroutine(LoadSceneAndTeleportRoutine(targetSceneName, targetPosition, onComplete));
        }
    }

    private void CoreTeleportAndSnap(Vector3 targetPosition)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 deltaPosition = targetPosition - player.transform.position;

            // 플레이어 즉시 이동 및 물리 잔상 제거
            player.transform.position = targetPosition;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = targetPosition;
                rb.linearVelocity = Vector2.zero;
            }

            // 활성화된 카메라 찾아서 즉시 스냅
            CinemachineCamera activeVcam = GetActiveCinemachineCamera();
            if (activeVcam != null)
            {
                activeVcam.PreviousStateIsValid = false;
                activeVcam.OnTargetObjectWarped(player.transform, deltaPosition);
            }
        }
    }

    private void ExecuteLocalTeleport(Vector3 targetPosition, Action onComplete)
    {
        FadeManager.Instance.FadeOut(fadeDuration, () =>
        {
            CoreTeleportAndSnap(targetPosition);

            FadeManager.Instance.FadeIn(fadeDuration, () =>
            {
                InputManager.Instance.SwitchActionMap("Field");
                onComplete?.Invoke();
            });
        });
    }

    // 다른 씬을 로드하고 플레이어를 배치하는 코루틴
    private IEnumerator LoadSceneAndTeleportRoutine(string targetSceneName, Vector3 targetPosition, Action onComplete)
    {
        bool isFadeOutDone = false;
        FadeManager.Instance.FadeOut(fadeDuration, () => isFadeOutDone = true);
        yield return new WaitUntil(() => isFadeOutDone);

        // 비동기 씬 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 새 씬의 카메라가 플레이어를 타겟팅할 시간
        yield return null;

        // 순수 텔레포트 뼈대 함수 실행
        CoreTeleportAndSnap(targetPosition);

        // 렌더링 정착 대기
        yield return null;

        bool isFadeInDone = false;
        FadeManager.Instance.FadeIn(fadeDuration, () =>
        {
            isFadeInDone = true;
            InputManager.Instance.SwitchActionMap("Field"); // 조작 복구
            onComplete?.Invoke(); // 포탈 쿨타임 해제 등 콜백 실행
        });

        yield return new WaitUntil(() => isFadeInDone);

        Debug.Log($"{targetSceneName} 씬으로 전환 및 순간이동 완료!");
    }

    private CinemachineCamera GetActiveCinemachineCamera()
    {
        // CinemachineBrain이 현재 제어 중인 Active Virtual Camera 가져오기
        CinemachineBrain brain = UnityEngine.Object.FindFirstObjectByType<CinemachineBrain>();
        if (brain != null && brain.ActiveVirtualCamera != null)
        {
            CinemachineCamera vcam = brain.ActiveVirtualCamera as CinemachineCamera;
            if (vcam != null) return vcam;
        }

        // 씬에 켜져 있는(isActiveAndEnabled) CinemachineCamera 검색
        CinemachineCamera[] vcams = UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (var v in vcams)
        {
            if (v.isActiveAndEnabled)
            {
                return v;
            }
        }

        return null;
    }
}