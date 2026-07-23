using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum StageFivePhase
{
    Idle,
    Mirror,      // 전신거울에서 인증코드 확인
    Verifying,   // 컴퓨터에 코드 입력
    Complete,    // Recovery 100%
}

/// <summary>
/// 5단계 User Verification.
/// 기획 변경: 웹캠 얼굴 인증을 삭제하고, 전신거울에서 인증코드가 나타나면 그 코드를
/// 컴퓨터에 바로 입력하는 방식으로 바꾼다.
/// 전신거울 조사 → 코드 노출 → 컴퓨터 입력 → Recovery 95% → 100% 완료.
/// </summary>
[DisallowMultipleComponent]
public sealed class StageFiveFlowController : MonoBehaviour
{
    [Header("Verification")]
    [Tooltip("전신거울에 나타나고, 컴퓨터에 입력해야 하는 인증코드.")]
    [SerializeField] private string authCode = "7290";

    [Header("Room Interactables")]
    [SerializeField] private RoomInteractable mirrorInteractable;   // 전신거울 조사 → 코드 노출
    [SerializeField] private RoomInteractable computerInteractable; // 코드 입력 창 열기

    [Header("Systems")]
    [SerializeField] private StageFiveCodeInputWindow codeInputWindow;
    [SerializeField] private Text mirrorCodeText;                   // 거울에 코드를 띄운다(아트 있으면 그 위에)

    [Header("UI")]
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text recoveryProgressText;
    [SerializeField] private GameObject recoveryWindow;
    [SerializeField] private Text recoveryBodyText;

    [Header("Audio")]
    [SerializeField] private AudioClip codeRevealClip;
    [SerializeField] private AudioClip verifyFailClip;
    [SerializeField] private AudioClip verifySuccessClip;

    [Header("Dialogue")]
    [SerializeField] private string mirrorScriptId = "D049Stage5Start";
    [SerializeField] private string verifiedScriptId = "D052Stage5OwnerVerified";

    [Header("Ending Link")]
    [SerializeField] private FinalSequenceController finalSequence;

    [Header("Events")]
    [SerializeField] private UnityEvent onStageFiveCompleted = new();

    public StageFivePhase CurrentPhase { get; private set; } = StageFivePhase.Idle;
    public bool IsVerified => HasCondition(GameCondition.Stage5Verified);
    public event Action<StageFivePhase> PhaseChanged;

    private int failedAttempts;

    private void Awake()
    {
        if (mirrorInteractable != null)
            mirrorInteractable.OnInteracted.AddListener(OnMirrorInvestigated);
    }

    private void OnDestroy()
    {
        if (mirrorInteractable != null)
            mirrorInteractable.OnInteracted.RemoveListener(OnMirrorInvestigated);
    }

    public void ResetStage()
    {
        SetPhase(StageFivePhase.Idle);
        failedAttempts = 0;
        // 전신거울은 5단계 전까지 숨겨 둔다.
        if (mirrorInteractable != null)
            mirrorInteractable.gameObject.SetActive(false);
        SetInteractable(mirrorInteractable, false);
        SetInteractable(computerInteractable, false);
        codeInputWindow?.ResetView();
        SetText(mirrorCodeText, string.Empty);
        SetActive(recoveryWindow, false);
    }

    /// <summary>4단계 완료 후 StageFour가 호출한다.</summary>
    public void BeginStage()
    {
        SetPhase(StageFivePhase.Mirror);
        // 5단계 진입 시 전신거울을 등장시킨다.
        if (mirrorInteractable != null)
            mirrorInteractable.gameObject.SetActive(true);
        SetInteractable(mirrorInteractable, true);
        SetInteractable(computerInteractable, false);

        SetText(objectiveText, "전신거울을 확인한다");
        SetText(recoveryBodyText,
            "STEP 5\n\nUSER VERIFICATION\n\nRetrieve the verification code\nand authenticate.");
    }

    // 전신거울을 조사하면 인증코드가 나타난다.
    private void OnMirrorInvestigated()
    {
        if (CurrentPhase != StageFivePhase.Mirror)
            return;

        SetPhase(StageFivePhase.Verifying);
        GameConditionManager.Instance?.SetCondition(GameCondition.Stage5CodeRevealed);
        PlaySfx(codeRevealClip);

        SetText(mirrorCodeText, authCode);
        if (!string.IsNullOrWhiteSpace(mirrorScriptId))
            ScriptManager.Instance?.Play(mirrorScriptId);

        ConfigureComputer();
        SetInteractable(mirrorInteractable, false);
        SetInteractable(computerInteractable, true);
        SetText(objectiveText, "컴퓨터에 인증코드를 입력한다");
    }

    private void ConfigureComputer()
    {
        if (computerInteractable == null || computerInteractable.puzzleAction == null)
            return;

        computerInteractable.Configure("computer_stage5_verify", "[E] 인증코드 입력", false);
        computerInteractable.puzzleAction.Conditions = new List<GameCondition> { GameCondition.PrologueEnded };
        computerInteractable.puzzleAction.OpeningUI = codeInputWindow;
        computerInteractable.puzzleAction.NarrationID = new List<string>();
        computerInteractable.enabled = true;
    }

    public void RefreshCodeInputView()
    {
        if (IsVerified)
            codeInputWindow?.ShowSolved(authCode);
        else
            codeInputWindow?.ShowAttemptState(failedAttempts);
    }

    public void SubmitCode(string code)
    {
        if (CurrentPhase != StageFivePhase.Verifying)
            return;

        if (IsVerified)
        {
            codeInputWindow?.ShowSolved(authCode);
            return;
        }

        if (string.Equals((code ?? string.Empty).Trim(), authCode, StringComparison.OrdinalIgnoreCase))
        {
            StartCoroutine(CompleteSequence());
            return;
        }

        failedAttempts++;
        PlaySfx(verifyFailClip);
        codeInputWindow?.ShowAttemptState(failedAttempts);
    }

    private IEnumerator CompleteSequence()
    {
        GameConditionManager.Instance?.SetCondition(GameCondition.Stage5Verified);
        PlaySfx(verifySuccessClip);
        codeInputWindow?.ShowSolved(authCode);

        GameConditionManager.Instance?.SetRecoveryStage(RecoveryStage.UserVerification);
        SetText(recoveryProgressText, "Recovery Progress: 95%");
        SetInteractable(computerInteractable, false);

        yield return new WaitForSecondsRealtime(1.5f);

        codeInputWindow?.CloseWindow();
        GameStateManager.Instance?.SetState(GameState.UI);

        GameConditionManager.Instance?.SetRecoveryStage(RecoveryStage.Complete);
        SetText(recoveryProgressText, "Recovery Progress: 100%");
        SetText(objectiveText, "Recovery Complete");
        SetText(recoveryBodyText,
            "RECOVERY COMPLETE\n\nAll data restored.\n\n100%");
        SetActive(recoveryWindow, true);

        if (!string.IsNullOrWhiteSpace(verifiedScriptId))
            yield return PlayScript(verifiedScriptId);

        SetPhase(StageFivePhase.Complete);
        onStageFiveCompleted?.Invoke();

        // 엔딩(최종 저장 시퀀스)으로 이어진다.
        finalSequence?.BeginEnding();
    }

    private IEnumerator PlayScript(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || ScriptManager.Instance == null)
            yield break;

        ScriptManager.Instance.Play(id);
        yield return null;
        while (ScriptManager.Instance != null && ScriptManager.Instance.IsPlaying)
            yield return null;
    }

    private static bool HasCondition(GameCondition condition)
    {
        GameConditionManager manager = GameConditionManager.Instance;
        return manager != null && (manager.Condition & condition) == condition;
    }

    private static void PlaySfx(AudioClip clip)
    {
        if (clip != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(clip);
    }

    private void SetPhase(StageFivePhase phase)
    {
        CurrentPhase = phase;
        PhaseChanged?.Invoke(phase);
    }

    private static void SetInteractable(RoomInteractable interactable, bool value)
    {
        if (interactable == null)
            return;
        interactable.enabled = value;
        foreach (Collider2D collider in interactable.GetComponents<Collider2D>())
            collider.enabled = value;
    }

    private static void SetActive(GameObject target, bool value)
    {
        if (target != null && target.activeSelf != value)
            target.SetActive(value);
    }

    private static void SetText(Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
