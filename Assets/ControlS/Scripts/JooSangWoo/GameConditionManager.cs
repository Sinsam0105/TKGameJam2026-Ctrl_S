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
    All = Test | PrologueEnded | AllImageFound | ImagePuzzleCompleted,
}

[Serializable]
public sealed class ConditionObject
{
    [SerializeField] private GameCondition condition;
    [SerializeField] private GameObject target;

    public GameCondition Condition => condition;
    public GameObject Target => target;
}

public class GameConditionManager : MonoSingleton<GameConditionManager>
{
    [SerializeField] private List<ConditionObject> conditionObjects = new List<ConditionObject>();
    public float GameProgress => (float)Condition / (float)GameCondition.All;
    public GameCondition Condition { get; private set; }
    public void SetCondition(GameCondition condition)
    {
        if (condition == GameCondition.None)
        {
            return;
        }

        Condition |= condition;

        foreach (ConditionObject conditionObject in conditionObjects)
        {
            if (conditionObject == null || conditionObject.Target == null)
            {
                continue;
            }

            if ((conditionObject.Condition & condition) != 0)
            {
                conditionObject.Target.SetActive(true);
            }
        }
    }
}
