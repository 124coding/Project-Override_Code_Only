using UnityEngine;

[RequireComponent(typeof(AreaEffector2D))]
public class WindDraft : MonoBehaviour, IWorkObject
{
    [Header("Signal Settings")]
    [Tooltip("체크하면 레버/동력원의 신호를 받아야만 바람이 붑니다.\n체크 해제하면 항상 바람이 붑니다.")]
    public bool useSignal = false;

    private AreaEffector2D effector;

    // 바람 파티클 이펙트
    private ParticleSystem windParticles;

    private void Awake()
    {
        effector = GetComponent<AreaEffector2D>();
        windParticles = GetComponentInChildren<ParticleSystem>();

        // 신호를 받아야 하는 기류라면 처음엔 꺼둡니다.
        if (useSignal)
        {
            TurnOffWind();
        }
    }
    public void WorkOn()
    {
        if (useSignal) TurnOnWind();
    }

    public void WorkOff()
    {
        if (useSignal) TurnOffWind();
    }

    private void TurnOnWind()
    {
        effector.enabled = true;
        if (windParticles != null) windParticles.Play();
    }

    private void TurnOffWind()
    {
        effector.enabled = false;
        if (windParticles != null) windParticles.Stop();
    }
}