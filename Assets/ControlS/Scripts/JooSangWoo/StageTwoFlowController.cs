using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public sealed class StageTwoScriptIds
{
    public string MicrowaveFirst = "Stage2MicrowaveFirst";
    public string MicrowaveRepeat = "Stage2MicrowaveRepeat";
    public string PostItFirst = "Stage2PostItFirst";
    public string PostItRepeat = "Stage2PostItRepeat";
    public string OutsideClockFirst = "Stage2OutsideClockFirst";
    public string OutsideClockRepeat = "Stage2OutsideClockRepeat";
    public string Calculate = "Stage2Calculate";
    public string TimeSolved = "Stage2TimeSolved";
}

[DisallowMultipleComponent]
public sealed class StageTwoFlowController : MonoBehaviour
{
    private const string CorrectHour = "03";
    private const string CorrectMinute = "05";

    // 정면샷 창의 액션 버튼 id. 프리팹에 저장된 버튼 id와 같은 값을 쓴다.
    public const string MicrowaveActionId = "clue_microwave";
    public const string PostItActionId = "clue_postit";
    public const string OutsideClockActionId = "clue_outsideclock";
    // 정민 선배 납량 연출(MicrowaveDirection / WashingMachineDirection) 연결용
    public const string MicrowaveDoorActionId = "microwave_door";
    public const string WasherDoorCloseActionId = "washer_door_close";

    [Header("Serialized Prefab Parts")]
    [SerializeField] private StageTwoClueInteractable microwaveClue;
    [SerializeField] private StageTwoClueInteractable postItClue;
    [SerializeField] private StageTwoClueInteractable outsideClockClue;
    [SerializeField] private List<FurnitureViewWindow> clueViews = new();
    [SerializeField] private StageTwoTimeInputWindow timeInputWindow;

    [Header("납량 연출 (정민)")]
    [SerializeField] private MicrowaveDirection microwaveDirection;
    [SerializeField] private WashingMachineDirection washingMachineDirection;
    [SerializeField] private RoomInteractable washerSpot;
    [SerializeField] private List<GameObject> clueHighlights = new();

    [Header("State")]
    [SerializeField] private bool stageActive;
    [SerializeField, Min(0)] private int failedAttemptCount;
    [SerializeField] private bool calculationDialoguePlayed;
    [SerializeField] private bool completionInvoked;

    [Header("Dialogue")]
    [SerializeField] private StageTwoScriptIds scriptIds = new();

    [Header("Sound")]
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioClip inputFailedClip;
    [SerializeField] private AudioClip inputSucceededClip;
    [SerializeField] private AudioClip stageCompleteClip;   // Recovery 40% 알림음
    [SerializeField] private AudioClip stageStartClip;      // 단계 진입 알림음
    [SerializeField] private AudioClip washerDoorClip;      // 세탁기 문 개폐음
    [SerializeField] private AudioClip microwaveDoorClip;   // 전자레인지 문 개폐음

    [Header("Stage 3 Link")]
    [SerializeField] private StageThreeFlowController stageThreeFlow;

    [Header("Events")]
    [SerializeField] private UnityEvent onStageTwoCompleted = new();

    private RoomInteractable computerInteractable;
    private Text objectiveText;

    public bool IsStageActive => stageActive;
    public int FailedAttemptCount => failedAttemptCount;
    public bool MicrowaveInvestigated => HasCondition(GameCondition.Stage2MicrowaveInvestigated);
    public bool PostItInvestigated => HasCondition(GameCondition.Stage2PostItInvestigated);
    public bool OutsideClockInvestigated => HasCondition(GameCondition.Stage2OutsideClockInvestigated);
    public bool IsSolved => HasCondition(GameCondition.Stage2TimeSolved);

    private void Awake()
    {
        // 새 대본(D0xx) 적용: 시간 정답 대사를 새 ID로 덮어쓴다. (단서 구조가 달라 단서 대사는 유지)
        scriptIds.TimeSolved = "D017Stage2TimeCorrect";

        SetCluesEnabled(false);
        SetHighlights(false);

        foreach (FurnitureViewWindow view in clueViews)
        {
            if (view != null)
                view.ActionClicked += OnClueAction;
        }
    }

    private void OnDestroy()
    {
        foreach (FurnitureViewWindow view in clueViews)
        {
            if (view != null)
                view.ActionClicked -= OnClueAction;
        }
    }

    // 정면샷 안의 단서를 클릭했을 때. 창은 이미 열려 있으므로 조사 처리만 한다.
    private void OnClueAction(string actionId)
    {
        switch (actionId)
        {
            case MicrowaveActionId:
                Investigate(StageTwoClueType.Microwave);
                break;
            case PostItActionId:
                Investigate(StageTwoClueType.PostIt);
                break;
            case OutsideClockActionId:
                Investigate(StageTwoClueType.OutsideClock);
                break;

            // 전자레인지 문을 열면 회전판이 멈추고, 베란다 쪽에서 세탁기 완료음이 울린다.
            case MicrowaveDoorActionId:
                PlayEffect(microwaveDoorClip);
                microwaveDirection?.OnOpened();
                washingMachineDirection?.OnMicrowaveOpened();
                break;

            // 세탁기 문을 닫으면 디스플레이에 03:05가 0.5초 표시된다.
            case WasherDoorCloseActionId:
                PlayEffect(washerDoorClip);
                washingMachineDirection?.OnClosed();
                break;
        }
    }

    public void BeginStage(RoomInteractable computer, Text sharedObjectiveText)
    {
        computerInteractable = computer;
        objectiveText = sharedObjectiveText;
        stageActive = true;
        SetCluesEnabled(true);
        SetHighlights(failedAttemptCount >= 3 && !IsSolved);
        ConfigureComputer();
        RefreshObjective();
        PlayEffect(stageStartClip);
        PlayScript("D013Stage2Start");
    }

    public void ResetStage()
    {
        stageActive = false;
        failedAttemptCount = 0;
        calculationDialoguePlayed = false;
        completionInvoked = false;
        computerInteractable = null;
        objectiveText = null;

        SetCluesEnabled(false);
        SetHighlights(false);
        foreach (FurnitureViewWindow view in clueViews)
            view?.ResetView();
        timeInputWindow?.ResetView();

        if (microwaveDirection != null)
            microwaveDirection.gameObject.SetActive(false);
        if (washingMachineDirection != null)
            washingMachineDirection.gameObject.SetActive(false);
    }

    public void Investigate(StageTwoClueType clueType)
    {
        if (!stageActive)
            return;

        bool repeated = IsInvestigated(clueType);
        if (!repeated)
            GameConditionManager.Instance?.SetCondition(GetCondition(clueType));

        PlayScript(GetScriptId(clueType, repeated));
        RefreshObjective();

        if (!calculationDialoguePlayed && AllCluesInvestigated)
        {
            calculationDialoguePlayed = true;
            PlayScript(scriptIds.Calculate);
        }
    }

    public void SubmitTime(string hour, string minute)
    {
        if (!stageActive)
            return;

        if (IsSolved)
        {
            timeInputWindow?.ShowSolved();
            return;
        }

        if (hour == CorrectHour && minute == CorrectMinute)
        {
            CompleteStage();
            return;
        }

        failedAttemptCount++;
        PlayEffect(inputFailedClip);
        bool highlightClues = failedAttemptCount >= 3;
        SetHighlights(highlightClues);
        timeInputWindow?.ShowAttemptState(failedAttemptCount, highlightClues);
    }

    public void RefreshTimeInputView()
    {
        if (IsSolved)
            timeInputWindow?.ShowSolved();
        else
            timeInputWindow?.ShowAttemptState(failedAttemptCount, failedAttemptCount >= 3);
    }

    private void CompleteStage()
    {
        if (IsSolved)
            return;

        GameConditionManager.Instance?.SetCondition(GameCondition.Stage2TimeSolved);
        GameConditionManager.Instance?.SetRecoveryStage(RecoveryStage.ActivityLogRecovery);
        PlayEffect(inputSucceededClip);
        PlayEffect(stageCompleteClip);
        SetHighlights(false);
        timeInputWindow?.ShowSolved();

        if (objectiveText != null)
            objectiveText.text = "2단계 완료  ·  Recovery Progress: 40%";

        // 연출 오브젝트를 켠 뒤(Awake→Init) 전자레인지 연출을 명시적으로 시작한다.
        if (microwaveDirection != null)
        {
            microwaveDirection.gameObject.SetActive(true);
            microwaveDirection.OnAnswerValidated();
        }
        if (washingMachineDirection != null)
            washingMachineDirection.gameObject.SetActive(true);

        PlayScript(scriptIds.TimeSolved);
        if (!completionInvoked)
        {
            completionInvoked = true;
            // 3단계 조사 지점(컴퓨터의 버전 기록, 백업 상자)을 연다.
            stageThreeFlow?.BeginStage();
            onStageTwoCompleted?.Invoke();
        }
    }

    private void ConfigureComputer()
    {
        if (computerInteractable == null || computerInteractable.puzzleAction == null)
            return;

        computerInteractable.Configure("computer_stage2_time", "[E] 시간 입력", false);
        computerInteractable.puzzleAction.Conditions = new List<GameCondition> { GameCondition.PrologueEnded };
        computerInteractable.puzzleAction.OpeningUI = timeInputWindow;
        computerInteractable.puzzleAction.SpeechID = new List<string>();
        computerInteractable.enabled = true;
    }

    private bool AllCluesInvestigated => MicrowaveInvestigated && PostItInvestigated && OutsideClockInvestigated;

    private bool IsInvestigated(StageTwoClueType clueType)
    {
        return HasCondition(GetCondition(clueType));
    }

    private static bool HasCondition(GameCondition condition)
    {
        GameConditionManager manager = GameConditionManager.Instance;
        return manager != null && (manager.Condition & condition) == condition;
    }

    private static GameCondition GetCondition(StageTwoClueType clueType)
    {
        return clueType switch
        {
            StageTwoClueType.Microwave => GameCondition.Stage2MicrowaveInvestigated,
            StageTwoClueType.PostIt => GameCondition.Stage2PostItInvestigated,
            StageTwoClueType.OutsideClock => GameCondition.Stage2OutsideClockInvestigated,
            _ => GameCondition.None,
        };
    }

    private string GetScriptId(StageTwoClueType clueType, bool repeated)
    {
        return clueType switch
        {
            StageTwoClueType.Microwave => repeated ? scriptIds.MicrowaveRepeat : scriptIds.MicrowaveFirst,
            StageTwoClueType.PostIt => repeated ? scriptIds.PostItRepeat : scriptIds.PostItFirst,
            StageTwoClueType.OutsideClock => repeated ? scriptIds.OutsideClockRepeat : scriptIds.OutsideClockFirst,
            _ => string.Empty,
        };
    }

    // 클립이 비어 있으면 Unity가 경고를 뱉으므로 여기서 걸러낸다.
    // 효과음은 일괄 SoundManager로 보내고, 없으면 로컬 소스로 대체한다.
    private void PlayEffect(AudioClip clip)
    {
        if (clip == null)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(clip);
        else if (effectsSource != null)
            effectsSource.PlayOneShot(clip);
    }

    private static void PlayScript(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
            ScriptManager.Instance?.Play(id);
    }

    private void SetCluesEnabled(bool value)
    {
        microwaveClue?.SetStageEnabled(value);
        postItClue?.SetStageEnabled(value);
        outsideClockClue?.SetStageEnabled(value);

        // 세탁기는 단서가 아니라 납량 연출용이라 별도로 켠다.
        if (washerSpot != null)
        {
            washerSpot.ResetInteraction();
            washerSpot.enabled = value;
            foreach (Collider2D collider in washerSpot.GetComponents<Collider2D>())
                collider.enabled = value;
        }
    }

    private void SetHighlights(bool value)
    {
        foreach (GameObject highlight in clueHighlights)
        {
            if (highlight != null)
                highlight.SetActive(value);
        }
    }

    private void RefreshObjective()
    {
        if (objectiveText == null || IsSolved)
            return;

        int count = 0;
        if (MicrowaveInvestigated) count++;
        if (PostItInvestigated) count++;
        if (OutsideClockInvestigated) count++;

        objectiveText.text = count < 3
            ? $"2단계 단서 조사  {count}/3"
            : "컴퓨터에서 실제 시간을 입력한다";
    }
}
