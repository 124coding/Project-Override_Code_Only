using UnityEngine;
using UnityEngine.Events;

public class FusePuzzleManager : MonoBehaviour
{
    [Header("Puzzle Settings")]
    [Tooltip("이 구역에서 꽂아야 할 총 퓨즈의 개수")]
    public int totalRequiredFuses = 3;

    private int currentInsertedFuses = 0;

    [Header("Events")]
    [Tooltip("퓨즈를 모두 꽂았을 때 실행될 이벤트 (여기에 보스 소환이나 컷신 함수를 연결)")]
    public UnityEvent OnAllFusesInserted;

    [Tooltip("이미 클리어된 방에 들어왔을 때 실행될 이벤트 (문 열어두기 등)")]
    public UnityEvent OnAlreadyCleared;

    // FuseBox에서 퓨즈가 꽂힐 때마다 이 함수를 호출함
    public void OnFuseInserted()
    {
        currentInsertedFuses++;
        Debug.Log($"현재 퓨즈 상황: {currentInsertedFuses} / {totalRequiredFuses}");

        // 모든 퓨즈가 꽂혔다면?
        if (currentInsertedFuses >= totalRequiredFuses)
        {
            TriggerEvent();
        }
    }

    // 씬 로드 시 '이미 꽂혀있던 퓨즈'를 셌을 때 호출 (이벤트 중복 실행 방지)
    public void OnFuseLoaded()
    {
        currentInsertedFuses++;

        // 로드했는데 이미 다 꽂혀있는 상태(클리어 상태)라면?
        if (currentInsertedFuses >= totalRequiredFuses)
        {
            // 보스를 또 소환하면 안 되니, "문이 이미 열려있는 상태" 등의 이벤트만 조용히 실행
            OnAlreadyCleared?.Invoke();
        }
    }

    private void TriggerEvent()
    {
        Debug.Log("모든 퓨즈 장착 완료! 기믹 발동!");

        // 인스펙터에서 연결해둔 이벤트(보스 소환 등) 실행
        OnAllFusesInserted?.Invoke();

        // 코드 레벨에서 직접 보스를 활성화하려면 아래처럼 작성 가능
        // bossGameObject.SetActive(true);
        // cutsceneManager.PlaySequence();
    }
}