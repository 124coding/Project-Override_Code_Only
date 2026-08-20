using UnityEngine;
using DG.Tweening; // DOTween 필수

public class PuzzleDoor : MonoBehaviour, IWorkObject
{
    [Header("Settings")]
    public float openDuration = 0.5f;
    public Vector3 openOffset = new Vector3(0, 3f, 0);
    public Ease moveEase = Ease.InOutQuad; // 더 자연스러운 곡선

    private Vector3 closedPos;
    private Vector3 openPos;
    private Tween currentTween; // 현재 진행 중인 애니메이션을 담을 변수

    private void Awake()
    {
        closedPos = transform.position;
        openPos = closedPos + openOffset;
    }

    // 신호를 받았을 때 (열림)
    public void WorkOn()
    {
        MoveDoor(openPos);
    }

    // 신호가 끊겼을 때 (닫힘)
    public void WorkOff()
    {
        MoveDoor(closedPos);
    }

    private void MoveDoor(Vector3 targetPos)
    {
        // 1. 기존에 움직이던 트윈이 있다면 즉시 멈추고 덮어씌움 (충돌 방지)
        currentTween?.Kill();

        // 2. 새로운 트윈 생성 및 실행
        currentTween = transform.DOMove(targetPos, openDuration)
                                .SetEase(moveEase)
                                .SetLink(gameObject);
    }
}