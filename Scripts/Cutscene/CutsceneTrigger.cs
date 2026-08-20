using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(BoxCollider2D))]
public class CutsceneTrigger : MonoBehaviour
{
    [Header("연동할 타임라인")]
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private bool playOnlyOnce = true;

    private bool hasPlayed = false;

    private void Awake()
    {
        // 횡스크롤 게임의 트리거는 충돌하지 않고 통과해야 하므로 IsTrigger 강제 활성화
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasPlayed) return;

        // 플레이어 태그 확인
        if (collision.CompareTag("Player"))
        {
            if (playOnlyOnce) hasPlayed = true;

            // 컷신 매니저에게 타임라인 재생 명령 하달
            CutsceneManager.Instance.PlayCutscene(playableDirector);
        }
    }
}