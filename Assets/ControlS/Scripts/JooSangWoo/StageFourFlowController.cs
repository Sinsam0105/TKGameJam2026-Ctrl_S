using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum StageFourPhase
{
    Idle,
    Reconstructing, // 워크스페이스 재조립(사진 퍼즐과 동일)
    Complete,       // Recovery 80% 도달
}

/// <summary>
/// 4단계 Workspace Recovery.
/// 기획서(0718)의 Stage4는 1단계 이미지 복원과 동일한 조각 맞추기로 구현한다.
/// 컴퓨터에서 워크스페이스 퍼즐(PictureCollector)을 열어 조각을 다 맞추면
/// Recovery 80%에 도달하고 5단계(User Verification)로 넘어간다.
/// </summary>
[DisallowMultipleComponent]
public sealed class StageFourFlowController : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private PictureCollector workspaceCollector;   // 1단계와 동일한 조각 맞추기
    [SerializeField] private PuzzleWindowedUI workspaceWindow;      // 퍼즐이 들어 있는 창

    [Header("Room Interactables")]
    [SerializeField] private RoomInteractable computerInteractable; // 워크스페이스 복원 열기

    [Header("UI")]
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text recoveryProgressText;
    [SerializeField] private GameObject recoveryWindow;
    [SerializeField] private Text recoveryBodyText;

    [Header("Audio")]
    [SerializeField] private AudioClip workspaceRecoveryClip;       // "컴퓨터 Workspace Recovery 알림음"

    [Header("Dialogue")]
    [SerializeField] private string reconstructScriptId = "D038Stage4Start";
    [SerializeField] private string completeScriptId = "D045Stage4ToStage5";

    [Header("Stage 5 Link")]
    [SerializeField] private StageFiveFlowController stageFiveFlow;

    [Header("Events")]
    [SerializeField] private UnityEvent onStageFourCompleted = new();

    public StageFourPhase CurrentPhase { get; private set; } = StageFourPhase.Idle;
    public event Action<StageFourPhase> PhaseChanged;

    private void Awake()
    {
        if (workspaceCollector != null)
            workspaceCollector.OnPuzzleCompleted.AddListener(OnWorkspaceCompleted);
    }

    private void OnDestroy()
    {
        if (workspaceCollector != null)
            workspaceCollector.OnPuzzleCompleted.RemoveListener(OnWorkspaceCompleted);
    }

    public void ResetStage()
    {
        SetPhase(StageFourPhase.Idle);
        workspaceCollector?.ResetPuzzle();
        SetInteractable(computerInteractable, false);
        SetActive(recoveryWindow, false);
    }

    /// <summary>3단계 완료 후 StageThree(또는 씬 흐름)가 호출한다.</summary>
    public void BeginStage()
    {
        SetPhase(StageFourPhase.Reconstructing);
        ConfigureComputer();
        SetInteractable(computerInteractable, true);

        SetText(objectiveText, "컴퓨터에서 작업 공간을 복원한다");
        SetText(recoveryBodyText,
            "STEP 4\n\nWORKSPACE RECOVERY\n\nReassemble the corrupted\nworkspace layout.");

        if (!string.IsNullOrWhiteSpace(reconstructScriptId))
            ScriptManager.Instance?.Play(reconstructScriptId);
    }

    private void ConfigureComputer()
    {
        if (computerInteractable == null || computerInteractable.puzzleAction == null)
            return;

        computerInteractable.Configure("computer_stage4_workspace", "[E] 작업 공간 복원", false);
        computerInteractable.puzzleAction.Conditions =
            new System.Collections.Generic.List<GameCondition> { GameCondition.PrologueEnded };
        computerInteractable.puzzleAction.OpeningUI = workspaceWindow;
        computerInteractable.puzzleAction.NarrationID = new System.Collections.Generic.List<string>();
        computerInteractable.enabled = true;
    }

    private void OnWorkspaceCompleted()
    {
        if (CurrentPhase != StageFourPhase.Reconstructing)
            return;

        StartCoroutine(CompleteSequence());
    }

    private IEnumerator CompleteSequence()
    {
        GameStateManager.Instance?.SetState(GameState.UI);
        PlaySfx(workspaceRecoveryClip);

        GameConditionManager.Instance?.SetCondition(GameCondition.Stage4WorkspaceSolved);
        GameConditionManager.Instance?.SetRecoveryStage(RecoveryStage.WorkspaceRecovery);
        SetText(recoveryProgressText, "Recovery Progress: 80%");
        SetText(objectiveText, "Workspace Restored");

        SetActive(recoveryWindow, true);
        yield return new WaitForSecondsRealtime(1.5f);

        workspaceWindow?.CloseWindow();
        SetInteractable(computerInteractable, false);

        if (!string.IsNullOrWhiteSpace(completeScriptId))
        {
            yield return PlayScript(completeScriptId);
        }

        SetPhase(StageFourPhase.Complete);
        SetActive(recoveryWindow, false);
        onStageFourCompleted?.Invoke();

        // 5단계(전신거울 인증)로 넘어간다.
        stageFiveFlow?.BeginStage();
        GameStateManager.Instance?.SetRoom();
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

    private static void PlaySfx(AudioClip clip)
    {
        if (clip != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(clip);
    }

    private void SetPhase(StageFourPhase phase)
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
