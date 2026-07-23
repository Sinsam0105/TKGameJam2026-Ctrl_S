using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum StageThreePhase
{
    Idle,
    Sorting,        // 버전 카드 정렬
    CodeAcquired,   // DBFAEC 확보
    BoxOpened,      // 백업 상자 개방
    UsbConnected,   // USB 연결
    Complete,       // 60% 도달
}

[Serializable]
public sealed class StageThreeScriptIds
{
    public string CardConfirmed = "Stage3CardConfirmed";
}

/// <summary>
/// 3단계 Version History Recovery 흐름.
/// 정렬 퍼즐 → 접근 코드 → 백업 상자 문자 잠금 → USB 연결 → Recovery 60% → 현관 노크.
/// Version 07·현관 노크 공포 연출은 정민 선배의 Awake 실행형 프리팹이라, 여기서는 활성화만 한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class StageThreeFlowController : MonoBehaviour
{
    private const string UsbConnectActionId = "usb_connect";

    [Header("Systems")]
    [SerializeField] private VersionChainPuzzle versionPuzzle;
    [SerializeField] private LetterLockWindow letterLock;
    [SerializeField] private FurnitureViewWindow usbView;

    [Header("Room Interactables")]
    [SerializeField] private RoomInteractable computerInteractable;   // Version History 열기
    [SerializeField] private RoomInteractable backupBoxInteractable;  // 문자 잠금 열기
    [SerializeField] private RoomInteractable usbInteractable;        // USB+메모 조사
    [SerializeField] private RoomInteractable frontDoorInteractable;  // 60% 노크 후 현관 조사

    [Header("공포 연출 프리팹 (정민, Awake 실행형)")]
    [SerializeField] private GameObject version07Direction;  // 상자 개방 시 등장
    [SerializeField] private GameObject knockDirection;      // 60% 현관 노크

    [Header("UI")]
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text recoveryProgressText;
    [SerializeField] private GameObject recoveryWindow;
    [SerializeField] private Text recoveryBodyText;

    [Header("Dialogue")]
    [SerializeField] private StageThreeScriptIds scriptIds = new();

    [Header("Stage 4 Link")]
    [SerializeField] private StageFourFlowController stageFourFlow;

    [Header("Events")]
    [SerializeField] private UnityEvent onStageThreeCompleted = new();

    public StageThreePhase CurrentPhase { get; private set; } = StageThreePhase.Idle;
    public event Action<StageThreePhase> PhaseChanged;

    private void Awake()
    {
        if (versionPuzzle != null)
            versionPuzzle.OnSolved.AddListener(OnVersionSolved);
        if (letterLock != null)
            letterLock.OnBoxOpened.AddListener(OnBoxOpened);
        if (usbView != null)
            usbView.ActionClicked += OnUsbAction;
        if (frontDoorInteractable != null)
            frontDoorInteractable.OnInteracted.AddListener(OnFrontDoorInteracted);
    }

    private void OnDestroy()
    {
        if (versionPuzzle != null)
            versionPuzzle.OnSolved.RemoveListener(OnVersionSolved);
        if (letterLock != null)
            letterLock.OnBoxOpened.RemoveListener(OnBoxOpened);
        if (usbView != null)
            usbView.ActionClicked -= OnUsbAction;
        if (frontDoorInteractable != null)
            frontDoorInteractable.OnInteracted.RemoveListener(OnFrontDoorInteracted);
    }

    // 노크 후 현관을 조사하면 문이 열리고 빈 복도가 드러난다. (기획 29.6)
    private void OnFrontDoorInteracted()
    {
        knockDirection?.GetComponent<FrontDoorDirection>()?.OnDoorOpened();
    }

    public void ResetStage()
    {
        SetPhase(StageThreePhase.Idle);
        versionPuzzle?.ResetPuzzle();
        letterLock?.ResetView();

        SetInteractable(computerInteractable, false);
        SetInteractable(backupBoxInteractable, false);
        SetInteractable(usbInteractable, false);
        SetInteractable(frontDoorInteractable, false);

        SetActive(version07Direction, false);
        SetActive(knockDirection, false);
        usbView?.ResetView();
    }

    /// <summary>2단계 완료 후 StageTwo가 호출한다.</summary>
    public void BeginStage()
    {
        SetPhase(StageThreePhase.Sorting);
        SetInteractable(computerInteractable, true);
        SetInteractable(backupBoxInteractable, true);   // 상자는 게임 시작부터 보이되, 코드 전엔 못 연다
        SetInteractable(usbInteractable, false);

        SetText(objectiveText, "컴퓨터에서 버전 기록을 정렬한다");
        SetText(recoveryBodyText,
            "STEP 3\n\nVERSION HISTORY RECOVERY\n\nReconstruct the edit order\nof the recovered document.");
    }

    private void OnVersionSolved()
    {
        SetPhase(StageThreePhase.CodeAcquired);
        // 정렬 결과(예: ABCD)를 그대로 문자 잠금의 정답으로 넘긴다. 체크섬 없음.
        letterLock?.SetExpectedCode(versionPuzzle.AccessCode);
        SetText(objectiveText, $"백업 상자에 코드 입력: {versionPuzzle.AccessCode}");
        StartCoroutine(PlayScript(scriptIds.CardConfirmed));
    }

    private void OnBoxOpened()
    {
        SetPhase(StageThreePhase.BoxOpened);
        SetInteractable(usbInteractable, true);
        SetText(objectiveText, "상자 안의 USB를 연결한다");
    }

    // USB+메모 정면샷 안에서 "USB 연결"을 눌렀을 때
    private void OnUsbAction(string actionId)
    {
        if (!string.Equals(actionId, UsbConnectActionId, StringComparison.OrdinalIgnoreCase))
            return;
        if (CurrentPhase != StageThreePhase.BoxOpened)
            return;

        SetPhase(StageThreePhase.UsbConnected);
        GameConditionManager.Instance?.SetCondition(GameCondition.Stage3UsbConnected);
        SetInteractable(usbInteractable, false);
        usbView?.CloseWindow();

        // Version 07 공포 연출 (선배 프리팹). 활성화 후 명시적으로 재생 메서드를 부른다.
        SetActive(version07Direction, true);
        version07Direction?.GetComponent<Version07Direction>()?.OnUSBConnected();
        GameConditionManager.Instance?.SetCondition(GameCondition.Stage3Version07Seen);

        StartCoroutine(CompleteSequence());
    }

    private IEnumerator CompleteSequence()
    {
        GameConditionManager.Instance?.SetRecoveryStage(RecoveryStage.VersionHistoryRecovery);
        SetText(recoveryProgressText, "Recovery Progress: 60%");
        SetText(objectiveText, "Project Structure Restored");
        SetActive(recoveryWindow, true);

        yield return new WaitForSecondsRealtime(2f);

        // 60% 현관 노크 공포 연출 (선배 프리팹). 활성화 후 노크 재생 메서드를 부른다.
        SetActive(knockDirection, true);
        knockDirection?.GetComponent<FrontDoorDirection>()?.OnProjectStructureRestoredMessageFinished();
        GameConditionManager.Instance?.SetCondition(GameCondition.Stage3KnockHeard);

        // 노크 후 현관을 조사해 문을 열 수 있게 한다.
        SetInteractable(frontDoorInteractable, true);
        SetText(objectiveText, "현관을 확인한다");

        SetPhase(StageThreePhase.Complete);
        onStageThreeCompleted?.Invoke();

        // 4단계(Workspace Recovery)로 이어진다.
        stageFourFlow?.BeginStage();
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

    private void SetPhase(StageThreePhase phase)
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
