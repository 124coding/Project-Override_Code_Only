using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterVisual", menuName = "Player/Character Visual Data")]
public class CharacterVisualData : ScriptableObject
{
    public Sprite defaultSprite;              // 기본 필드 스프라이트
    public RuntimeAnimatorController animatorController; // 해당 캐릭터의 애니메이터 컨트롤러

    [Header("Character Unique Effects")]
    public GameObject attackEffectPrefab;
    [Tooltip("이펙트 기본 리소스 크기 보정 비율 (기본값 1)")]
    public float effectScaleMultiplier = 1f;

    // 추후 필요하다면 여기에 초상화 UI용 Sprite나 사운드 등을 추가 확장 가능!
}