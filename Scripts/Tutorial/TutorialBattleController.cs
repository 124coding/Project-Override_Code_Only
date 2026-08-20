using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum TutorialStep
{
    None,
    TurnOrder_1_1,
    ItemUse_1_2,
    MpSkill_1_3_Fail,  // MP 부족 유도
    MpSkill_1_3_Pass,  // MP 채운 후 스킬 사용
    Break_1_4,
    ForcedDefeat_1_5
}
public class TutorialBattleController : MonoBehaviour
{
    public static TutorialBattleController Instance { get; private set; }

    [Header("상태 추적")]
    public TutorialStep currentStep = TutorialStep.None;
    private CharacterStatus playerStatus;
    private CharacterStatus bossStatus;

    [Header("UI 연결 (튜토리얼용 UI 매니저)")]
    public TutorialUIManager tutorialUI;

    [Header("전투 UI 버튼들 (입력 통제용)")]
    public Button attackButton;
    public Button defendButton;
    public Button skillButton;
    public Button itemButton;

    private void Start()
    {
        // 마스터 코루틴 시작
        StartCoroutine(TutorialFlowRoutine());
    }

    private IEnumerator TutorialFlowRoutine()
    {
        Debug.Log("[Tutorial] 튜토리얼 전투 시퀀스 시작!");
        // LockAllButtons();

        int targetHp = Mathf.RoundToInt(playerStatus.MaxHp * 0.3f);
        playerStatus.ApplyHpChange(-(playerStatus.CurrentHP - targetHp));

        playerStatus.ApplyMpChange(-playerStatus.CurrentMP);

        // 1. 턴 계산기 학습
        currentStep = TutorialStep.TurnOrder_1_1;

        tutorialUI.ShowGuidePopup(new TutorialPopupData
        {
            message = "화면 상단의 [턴 표시]에서 캐릭터와 적의 공격 순서를 확인할 수 있습니다.\n속도가 빠른 캐릭터가 먼저 행동합니다.",
            position = PopupPositionType.Top_TurnOrderUI,
            isMaskingEnabled = true
        });

        yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
        tutorialUI.HideGuidePopup();

        // 2. 아이템 학습

        itemButton.interactable = true; // 아이템 버튼만 활성화

        tutorialUI.ShowGuidePopup(new TutorialPopupData
        {
            message = "기습 공격으로 인해 체력이 얼마 남지 않았습니다!\n[아이템] 탭을 눌러 회복약을 사용하세요.",
            position = PopupPositionType.Bottom_SkillTarget,
            isMaskingEnabled = true
        });

        // 아이템 사용 완료 이벤트 대기
        bool isActionStarted = false;
        System.Action<CharacterStatus, ItemData> onItemUse = (_, _) => isActionStarted = true;

        BattleEvents.OnItemSelected += onItemUse;

        bool isActionCompleted = false;
        System.Action onActionComplete = () => isActionCompleted = true;
        BattleEvents.OnActionCompleted += onActionComplete;

        yield return new WaitUntil(() => isActionStarted);
        BattleEvents.OnItemSelected -= onItemUse;

        tutorialUI.HideGuidePopup();
        // LockAllButtons();

        yield return new WaitUntil(() => isActionCompleted);
        BattleEvents.OnActionCompleted -= onActionComplete;

        Debug.Log("[Tutorial] 1-2 아이템 사용 연출까지 종료");

        // 3. 스킬 및 마나 시스템 학습
        currentStep = TutorialStep.MpSkill_1_3_Fail;

        skillButton.interactable = true; // 스킬 버튼만 켜줌

        tutorialUI.ShowGuidePopup(new TutorialPopupData
        {
            message = "[스킬]을 눌러 적을 공격해보세요.",
            position = PopupPositionType.Bottom_SkillTarget,
            isMaskingEnabled = false
        });

        // 스킬 버튼을 눌렀는지 감지
        bool isSkillTabClicked = false;
        UnityEngine.Events.UnityAction onSkillClick = () => isSkillTabClicked = true;
        skillButton.onClick.AddListener(onSkillClick);
        yield return new WaitUntil(() => isSkillTabClicked);
        skillButton.onClick.RemoveListener(onSkillClick);

        tutorialUI.ShowGuidePopup(new TutorialPopupData
        {
            message = "마나가 부족하여 스킬을 사용할 수 없습니다.\n[돌아가기] 버튼을 눌러 이전 화면으로 돌아가세요.",
            position = PopupPositionType.Center, // 스킬 리스트 옆을 가리키면 좋습니다
            isMaskingEnabled = false
        });

        // 유저가 '돌아가기(Cancel)' 버튼을 누를 때까지 대기!
        bool isCanceled = false;
        System.Action onCancel = () => isCanceled = true;
        BattleEvents.OnMenuCanceled += onCancel; // 위에서 만든 취소 이벤트 구독

        yield return new WaitUntil(() => isCanceled);
        BattleEvents.OnMenuCanceled -= onCancel;
        tutorialUI.HideGuidePopup();

        tutorialUI.ShowGuidePopup(new TutorialPopupData
        {
            message = "마나는 [기본 공격]을 하거나 [방어] 행동을 취할 때 각각 2, 3이 회복됩니다.\n [방어]를 선택하세요.",
            position = PopupPositionType.Center,
            isMaskingEnabled = true
        });

        skillButton.interactable = false;
        defendButton.interactable = true;

        // 행동 완료 대기
        isActionStarted = false;
        isActionCompleted = false;

        System.Action onDefenseSelect = () => isActionStarted = true;

        BattleEvents.OnDefenseSelected += onDefenseSelect;
        BattleEvents.OnActionCompleted += onActionComplete;

        // 시작 기다림
        yield return new WaitUntil(() => isActionStarted);
        BattleEvents.OnDefenseSelected -= onDefenseSelect;

        tutorialUI.HideGuidePopup(); // 팝업 끄기
        // LockAllButtons();

        // 연출 완전히 끝날 때까지 기다림
        yield return new WaitUntil(() => isActionCompleted);
        BattleEvents.OnActionCompleted -= onActionComplete;

       
        currentStep = TutorialStep.MpSkill_1_3_Pass;

        skillButton.interactable = true;

        tutorialUI.ShowGuidePopup(new TutorialPopupData
        {
            message = "[스킬] -> [교차 베기]를 눌러 적을 공격해보세요.",
            position = PopupPositionType.Bottom_SkillTarget,
            isMaskingEnabled = false
        });

        isActionStarted = false;
        isActionCompleted = false;

        System.Action<CharacterStatus, SkillData> onSkillSelect = (_, _) => isActionStarted = true;

        BattleEvents.OnSkillSelected += onSkillSelect;
        BattleEvents.OnActionCompleted += onActionComplete;

        yield return new WaitUntil(() => isActionStarted);
        BattleEvents.OnSkillSelected -= onSkillSelect;

        tutorialUI.HideGuidePopup(); // 연출 가리지 않게 팝업 끄기
        // LockAllButtons();

        // 스킬 화려한 연출 끝날 때까지 대기
        yield return new WaitUntil(() => isActionCompleted);
        BattleEvents.OnActionCompleted -= onActionComplete;

        Debug.Log("[Tutorial] 타겟팅 및 스킬 연출 종료");

        // Break 학습
        currentStep = TutorialStep.Break_1_4;

        // 연출이 다 끝나고 보스가 BREAK 상태로 굳어있을 때 설명 팝업을 띄웁니다!
        tutorialUI.ShowGuidePopup(new TutorialPopupData
        {
            message = "적의 약점 속성으로 공격하여 [BREAK]를 이끌었습니다! [BREAK]는 추가 데미지와 플레이어의 턴을 한 턴 더 가져갈 수 있게 합니다.",
            position = PopupPositionType.Center,
            isMaskingEnabled = true // 강조를 위해 어두운 배경 마스크 적용
        });

        yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
        tutorialUI.HideGuidePopup();

        // LockAllButtons();

        skillButton.interactable = false;
        attackButton.interactable = true;

        tutorialUI.ShowGuidePopup(new TutorialPopupData
        {
            message = "지금은 공격을 한번 더 실행해보겠습니다.",
            position = PopupPositionType.Center,
            isMaskingEnabled = true // 강조를 위해 어두운 배경 마스크 적용
        });

        // 행동 완료 대기
        isActionStarted = false;
        isActionCompleted = false;

        System.Action<CharacterStatus> onAttackSelect = (_) => isActionStarted = true;

        BattleEvents.OnNormalAttackSelected += onAttackSelect;
        BattleEvents.OnActionCompleted += onActionComplete;

        // 시작 기다림
        yield return new WaitUntil(() => isActionStarted);
        BattleEvents.OnNormalAttackSelected -= onAttackSelect;

        tutorialUI.HideGuidePopup(); // 팝업 끄기
        // LockAllButtons();

        // 연출 완전히 끝날 때까지 기다림
        yield return new WaitUntil(() => isActionCompleted);
        BattleEvents.OnActionCompleted -= onActionComplete;

        // 5. 강제 패배 및 전환
        currentStep = TutorialStep.ForcedDefeat_1_5;
        //  LockAllButtons();

        BattleEvents.OnTurnOverrideRequested?.Invoke(bossStatus);

        tutorialUI.ShowGuidePopup(new TutorialPopupData
        {
            message = "위험합니다!\n보스가 턴을 강제로 가져갔습니다!",
            position = PopupPositionType.Center,
            isMaskingEnabled = true
        });

        Debug.Log("[Tutorial] 보스의 즉사기 발동!");
        // TODO: 보스 AI가 강제로 강한 스킬 발동

        // 플레이어 사망 이벤트 대기
        bool isPlayerDead = false;
        System.Action<CharacterStatus> onDeath = (status) =>
        {
            if (status.IsPlayer) isPlayerDead = true;
        };
        BattleEvents.OnCharacterDied += onDeath;

        yield return new WaitUntil(() => isPlayerDead);
        BattleEvents.OnCharacterDied -= onDeath;

        // 패배 컷신 및 로딩 연출 시작
        yield return StartCoroutine(ProcessDefeatAndStoryRoutine());
    }

    private IEnumerator ProcessDefeatAndStoryRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        //// 1. 화면 검게 페이드 아웃
        //// yield return ScreenFader.Instance.FadeOut(1.5f);

        //// 2. 세계관 텍스트 출력
        //tutorialUI.ShowStoryText("과거 인류는 '에테르'라는 신기원의 에너지로 번영을 누렸다...\n\n멸망의 위기 속, 살아남은 사람들은 지상에 '오르도'라는 생존 세력을 결성해 저항하기 시작했다...");

        //yield return new WaitForSeconds(2.0f); // 읽을 시간 확보

        //tutorialUI.ShowPressAnyKey();
        //yield return new WaitUntil(() => Input.anyKeyDown);

        //// 3. 세이브 데이터에 튜토리얼 완료 기록!
        //DataManager.Instance.SetTutorialCleared(true);

        //// 4. 메인 씬으로 이동
        //SceneManager.LoadScene("MainFieldScene");
    }

    // --- 헬퍼 함수들 ---
    private void LockAllButtons()
    {
        attackButton.interactable = false;
        defendButton.interactable = false;
        skillButton.interactable = false;
        itemButton.interactable = false;
    }

    private void UnlockAllButtons()
    {
        attackButton.interactable = true;
        defendButton.interactable = true;
        skillButton.interactable = true;
        itemButton.interactable = true;
    }
}
