using Sinsam.SingletonSystem;
using System;
using System.Collections.Generic;
using UnityEngine;
[Flags]
public enum GameCondition
{
    None = 0,
    Test = 1 << 0,
    PrologueEnded = 1 << 1,
    AllImageFound = 1 << 2,
    ImagePuzzleCompleted = 1 << 3,
    Stage2MicrowaveInvestigated = 1 << 4,
    Stage2PostItInvestigated = 1 << 5,
    Stage2OutsideClockInvestigated = 1 << 6,
    Stage2TimeSolved = 1 << 7,
    Stage3VersionSolved = 1 << 8,
    Stage3BoxOpened = 1 << 9,
    Stage3UsbConnected = 1 << 10,
    Stage3Version07Seen = 1 << 11,
    Stage3KnockHeard = 1 << 12,
    Stage4WorkspaceSolved = 1 << 13,
    Stage5CodeRevealed = 1 << 14,
    Stage5Verified = 1 << 15,
    All = Test | PrologueEnded | AllImageFound | ImagePuzzleCompleted
        | Stage2MicrowaveInvestigated | Stage2PostItInvestigated
        | Stage2OutsideClockInvestigated | Stage2TimeSolved
        | Stage3VersionSolved | Stage3BoxOpened | Stage3UsbConnected
        | Stage3Version07Seen | Stage3KnockHeard
        | Stage4WorkspaceSolved | Stage5CodeRevealed | Stage5Verified,
}

public enum RecoveryStage
{
    None,
    ImageRecovery,
    ActivityLogRecovery,
    VersionHistoryRecovery,
    WorkspaceRecovery,
    UserVerification,
    Complete,
}

[Serializable]
public sealed class ConditionObject
{
    [SerializeField] private GameCondition condition;
    [SerializeField] private GameObject target;

    public GameCondition Condition => condition;
    public GameObject Target => target;
}

[DefaultExecutionOrder(-300)]
public class GameConditionManager : MonoSingleton<GameConditionManager>
{
    [SerializeField] private List<ConditionObject> conditionObjects = new List<ConditionObject>();
    [SerializeField] private RecoveryStage recoveryStage;

    public RecoveryStage CurrentRecoveryStage => recoveryStage;
    public float GameProgress => GetProgressPercentage(recoveryStage);
    public float NormalizedGameProgress => GameProgress / 100f;
    public int RecoveryProgress => Mathf.RoundToInt(GameProgress);
    public GameCondition Condition { get; private set; }
    public event Action<GameCondition> OnConditionChanged;
    public event Action<int> OnRecoveryProgressChanged;

    protected override bool ShouldPersist() => false;

    public void SetCondition(GameCondition condition)
    {
        if (condition == GameCondition.None)
        {
            return;
        }

        GameCondition previous = Condition;
        Condition |= condition;

        if (previous == Condition)
            return;

        foreach (ConditionObject conditionObject in conditionObjects)
        {
            if (conditionObject == null || conditionObject.Target == null)
            {
                continue;
            }

            if ((Condition & conditionObject.Condition) == conditionObject.Condition)
            {
                conditionObject.Target.SetActive(true);
            }
        }

        OnConditionChanged?.Invoke(Condition);
    }

    public void SetRecoveryStage(RecoveryStage stage)
    {
        if (recoveryStage == stage)
            return;

        recoveryStage = stage;
        OnRecoveryProgressChanged?.Invoke(RecoveryProgress);
    }

    public static float GetProgressPercentage(RecoveryStage stage)
    {
        return stage switch
        {
            RecoveryStage.None => 0f,
            RecoveryStage.ImageRecovery => 20f,
            RecoveryStage.ActivityLogRecovery => 40f,
            RecoveryStage.VersionHistoryRecovery => 60f,
            RecoveryStage.WorkspaceRecovery => 80f,
            RecoveryStage.UserVerification => 95f,
            RecoveryStage.Complete => 100f,
            _ => 0f,
        };
    }

    public void ResetProgress()
    {
        Condition = GameCondition.None;
        recoveryStage = RecoveryStage.None;

        foreach (ConditionObject conditionObject in conditionObjects)
        {
            if (conditionObject?.Target != null)
                conditionObject.Target.SetActive(false);
        }

        OnConditionChanged?.Invoke(Condition);
        OnRecoveryProgressChanged?.Invoke(RecoveryProgress);
    }
}
