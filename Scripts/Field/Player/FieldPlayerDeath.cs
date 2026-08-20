using UnityEngine;
using System.Collections;

public class FieldPlayerDeath : MonoBehaviour
{
    private bool isDead = false;

    // 외부(데들리 레이저 등)에서 직접 죽이고 싶을 때 호출하는 퍼블릭 함수
    public void Die()
    {
        if (!isDead)
        {
            StartCoroutine(HandleDeathRoutine());
        }
    }

    private IEnumerator HandleDeathRoutine()
    {
        isDead = true;
        Debug.Log("플레이어가 필드에서 사망했습니다!");

        // 조작 및 물리력 차단
        InputManager.Instance.StopPlayer();
        InputManager.Instance.DisableGameplayInput();
        GetComponent<Rigidbody2D>().simulated = false;

        // 사망 애니메이션 및 이펙트 대기
        // anim.SetTrigger("Die");
        yield return new WaitForSeconds(2.0f); // 연출이 끝날 때까지 대기

        // DataManager에서 마지막 휴식처 정보 가져오기
        string targetScene = DataManager.Instance.lastFieldSceneName;
        Vector3 targetPos = DataManager.Instance.lastPlayerPosition;


        // 파티 전원 체력 회복 및 필드 몬스터 부활 처리
        DataManager.Instance.FullHealParty();             // 작성해두신 파티 회복 함수 호출[cite: 1]
        DataManager.Instance.ResetRespawnableMonsters();  // 일반 몬스터 데스노트 초기화[cite: 1]

        TeleportManager.Instance.TeleportPlayer(targetScene, targetPos);

        // 조작 원상 복구
        Revive();
    }

    public void Revive()
    {
        isDead = false;
        GetComponent<Rigidbody2D>().simulated = true; // 물리 간섭 복구

        // 다시 필드 입력 모드로 원상 복구
        InputManager.Instance.SwitchActionMap("Field");

        Debug.Log("마지막 휴식처에서 부활 완료!");
    }
}