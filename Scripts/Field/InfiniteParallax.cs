using UnityEngine;

public class SeamlessInfiniteParallax : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("X, Y 축 각각의 패럴랙스 배율을 따로 설정합니다.\n(보통 Y축은 X축보다 작게 주거나 0으로 고정)")]
    public Vector2 parallaxMultiplier; // float에서 Vector2로 변경!

    public float startOffsetY = 0f;

    private Transform cam;
    private Vector3 lastCameraPosition;
    private float textureUnitSizeX;

    void Start()
    {
        cam = Camera.main.transform;
        lastCameraPosition = cam.position;

        // 원본 스프라이트 1장만의 길이에 스케일을 곱해 정확한 1칸 크기를 구함
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            textureUnitSizeX = spriteRenderer.sprite.bounds.size.x * transform.localScale.x;
        }
        else
        {
            Debug.LogError($"{gameObject.name}의 자식에 SpriteRenderer가 없습니다!");
        }

        // 게임 시작 시, 배경 부모의 X 위치를 카메라 X 위치에 맞춤
        transform.position = new Vector3(cam.position.x, cam.position.y + startOffsetY, transform.position.z);
    }

    void LateUpdate()
    {
        Vector3 deltaMovement = cam.position - lastCameraPosition;

        // 1. 패럴랙스 이동 (X, Y 각각 따로 배율을 곱해줌!)
        transform.position += new Vector3(
            deltaMovement.x * parallaxMultiplier.x,
            deltaMovement.y * parallaxMultiplier.y,
            0
        );
        lastCameraPosition = cam.position;

        // 2. 무한 스크롤 스왑 처리 (X축 기준)
        if (Mathf.Abs(cam.position.x - transform.position.x) >= textureUnitSizeX)
        {
            float offsetPositionX = (cam.position.x - transform.position.x) % textureUnitSizeX;
            transform.position = new Vector3(cam.position.x + offsetPositionX, transform.position.y, transform.position.z);
        }
    }

    public void ResumeParallax()
    {
        cam = Camera.main.transform;
        lastCameraPosition = cam.position; // 카메라 추적 박자를 현재 위치로 리셋해서 순간이동 방지!
    }
}