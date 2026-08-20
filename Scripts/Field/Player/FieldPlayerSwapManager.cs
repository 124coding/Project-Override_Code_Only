//using System.Collections.Generic;
//using UnityEngine;

//public class FieldPlayerSwapManager : MonoBehaviour
//{
//    [Header("캐릭터 비주얼 데이터베이스")]
//    [SerializeField] private List<CharacterVisualData> visualDatabase;

//    public CharacterVisualData CurrentVisualData { get; private set; }

//    [Header("Current Character")]
//    private SpriteRenderer spriteRenderer;
//    private Animator animator;

//    [Header("Character Unlocked or not")]
//    public bool isTankerUnlocked = false;
//    public bool isMageUnlocked = false;
//    public bool isGunnerUnlocked = false;

//    // 플레이어에 붙어있는 모든 레고 블록(컴포넌트)들 캐싱
//    private FieldPlayerMovement movement;
//    private FieldPlayerJump jump;
//    private FieldPlayerDash dash;
//    private FieldPlayerWallJump wallJump;
//    private FieldPlayerDoubleJump doubleJump;
//    private FieldPlayerPushPull pushPull;
//    private FieldGunShooter gunShooter;
//    private FieldPlayerMeleeAttack meleeAttack;

//    private void OnEnable()
//    {
//        // InputManager가 준비되었는지 확인 
//        if (InputManager.Instance != null)
//        {
//            InputManager.Instance.inputActions.Field.Swap1.performed += ctx => SwapCharacter(PlayerCharacterType.Default);
//            InputManager.Instance.inputActions.Field.Swap2.performed += ctx => SwapCharacter(PlayerCharacterType.Tanker);
//            InputManager.Instance.inputActions.Field.Swap3.performed += ctx => SwapCharacter(PlayerCharacterType.Mage);
//            InputManager.Instance.inputActions.Field.Swap4.performed += ctx => SwapCharacter(PlayerCharacterType.Gunner);
//        }
//    }

//    private void OnDisable()
//    {
//        if (InputManager.Instance != null)
//        {
//            // 스크립트가 꺼질 땐 안전하게 구독 해제
//            InputManager.Instance.inputActions.Field.Swap1.performed -= ctx => SwapCharacter(PlayerCharacterType.Default);
//            InputManager.Instance.inputActions.Field.Swap2.performed -= ctx => SwapCharacter(PlayerCharacterType.Tanker);
//            InputManager.Instance.inputActions.Field.Swap3.performed -= ctx => SwapCharacter(PlayerCharacterType.Mage);
//            InputManager.Instance.inputActions.Field.Swap4.performed -= ctx => SwapCharacter(PlayerCharacterType.Gunner);
//        }
//    }

//    private void Awake()
//    {
//        // 컴포넌트들 싹 다 긁어오기
//        movement = GetComponent<FieldPlayerMovement>();
//        jump = GetComponent<FieldPlayerJump>();
//        dash = GetComponent<FieldPlayerDash>();
//        wallJump = GetComponent<FieldPlayerWallJump>();
//        doubleJump = GetComponent<FieldPlayerDoubleJump>();
//        pushPull = GetComponent<FieldPlayerPushPull>();
//        gunShooter = GetComponent<FieldGunShooter>();
//        meleeAttack = GetComponent<FieldPlayerMeleeAttack>();
//        spriteRenderer = GetComponent<SpriteRenderer>();
//        animator = GetComponent<Animator>();
//    }

//    private void Start()
//    {
//        // 게임 시작 시 초기 캐릭터 세팅
//        ApplyCharacterAbilities();
//    }

//    // 캐릭터 교체 시도 함수
//    public void SwapCharacter(PlayerCharacterType targetCharacter)
//    {
//        if (currentCharacter == targetCharacter) return;

//        // 기획 조건 체크: 해금되지 않은 캐릭터로는 스왑 불가능!
//        if (targetCharacter == PlayerCharacterType.Tanker && !isTankerUnlocked) { Debug.Log("전사가 아직 해금되지 않았습니다."); return; }
//        if (targetCharacter == PlayerCharacterType.Mage && !isMageUnlocked) { Debug.Log("도적이 아직 해금되지 않았습니다."); return; }
//        if (targetCharacter == PlayerCharacterType.Gunner && !isGunnerUnlocked) { Debug.Log("총잡이가 아직 해금되지 않았습니다."); return; }

//        // 상태 변경
//        currentCharacter = targetCharacter;

//        // 외형(Sprite & 애니메이션) 변경
//        UpdateCharacterVisuals(targetCharacter);

//        // 능력치 및 스킬 세팅 변경
//        ApplyCharacterAbilities();

//        // 데이터 매니저 캐시 동기화 (세이브용)
//        if (DataManager.Instance != null)
//        {
//            DataManager.Instance.currentCharacterType = targetCharacter.ToString();
//        }

//        Debug.Log($"캐릭터 스왑 완료! 현재 캐릭터: {currentCharacter}");
//    }

//    // 핵심: 현재 캐릭터에 맞게 스위치를 ON / OFF 해주는 로직
//    private void ApplyCharacterAbilities()
//    {
//        CurrentVisualData = visualDatabase.Find(data => data.characterType == currentCharacter);

//        // 고유 능력들 일단 초기화
//        if (wallJump != null) wallJump.isActiveCharacter = false;
//        if (doubleJump != null) doubleJump.isActiveCharacter = false;
//        // if (pushPull != null) pushPull.isActiveCharacter = false;
//        if (gunShooter != null) gunShooter.isActiveCharacter = false;

//        // 현재 선택된 캐릭터의 고유 능력만 콕 집어서 켜주기!
//        switch (currentCharacter)
//        {
//            case PlayerCharacterType.Default:
//                if (wallJump != null) wallJump.isActiveCharacter = true;
//                break;

//            case PlayerCharacterType.Tanker:
//                // if (pushPull != null) pushPull.isActiveCharacter = true;       // 상자 밀기 ON
//                break;

//            case PlayerCharacterType.Mage:
//                if (doubleJump != null) doubleJump.isActiveCharacter = true;   // 이단 점프 ON
//                break;

//            case PlayerCharacterType.Gunner:
//                if (gunShooter != null) gunShooter.isActiveCharacter = true;   // 원거리 사격 ON
//                break;
//        }

//        UpdateCharacterVisuals(currentCharacter);
//    }

//    private void UpdateCharacterVisuals(PlayerCharacterType targetCharacter)
//    {
//        // 데이터베이스에서 해당 캐릭터 타입에 맞는 비주얼 데이터 검색
//        CharacterVisualData visualData = visualDatabase.Find(data => data.characterType == targetCharacter);

//        if (visualData == null)
//        {
//            Debug.LogError($"{targetCharacter}에 해당하는 비주얼 데이터가 데이터베이스에 없습니다!");
//            return;
//        }

//        // 스프라이트 교체
//        if (spriteRenderer != null)
//        {
//            spriteRenderer.sprite = visualData.defaultSprite;
//        }

//        // 애니메이터 컨트롤러 교
//        if (animator != null && visualData.animatorController != null)
//        {
//            animator.runtimeAnimatorController = visualData.animatorController;

//            // 컨트롤러가 바뀌면 애니메이터가 순간 꼬일 수 있으므로 
//            // 바뀐 컨트롤러의 기본 상태(ex: Idle)를 즉시 강제 재생해주는 것이 안전합니다.
//            animator.Play("Idle", 0, 0f);
//        }
//    }

//    // 새 캐릭터(능력)를 획득했을 때 호출할 함수
//    public void UnlockCharacter(PlayerCharacterType characterToUnlock)
//    {
//        switch (characterToUnlock)
//        {
//            case PlayerCharacterType.Tanker: isTankerUnlocked = true; break;
//            case PlayerCharacterType.Mage: isMageUnlocked = true; break;
//            case PlayerCharacterType.Gunner: isGunnerUnlocked = true; break;
//        }
//        Debug.Log($"{characterToUnlock} 능력이 해금되었습니다! 이제 스왑이 가능합니다.");
//    }
//}