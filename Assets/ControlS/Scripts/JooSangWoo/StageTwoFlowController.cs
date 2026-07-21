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

    // 정면샷 창의 액션 버튼 id. StageTwoPrefabSetup이 같은 값을 심는다.
    public const string MicrowaveActionId = "clue_microwave";
    public const string PostItActionId = "clue_postit";
    public const string OutsideClockActionId = "clue_outsideclock";

    [Header("Serialized Prefab Parts")]
    [SerializeField] private StageTwoClueInteractable microwaveClue;
    [SerializeField] private StageTwoClueInteractable postItClue;
    [SerializeField] private StageTwoClueInteractable outsideClockClue;
    [SerializeField] private List<FurnitureViewWindow> clueViews = new();
    [SerializeField] private StageTwoTimeInputWindow timeInputWindow;
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
        effectsSource?.PlayOneShot(inputFailedClip);
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
        effectsSource?.PlayOneShot(inputSucceededClip);
        SetHighlights(false);
        timeInputWindow?.ShowSolved();

        if (objectiveText != null)
            objectiveText.text = "2단계 완료  ·  Recovery Progress: 40%";

        PlayScript(scriptIds.TimeSolved);
        if (!completionInvoked)
        {
            completionInvoked = true;
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
        computerInteractable.puzzleAction.NarrationID = new List<string>();
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
