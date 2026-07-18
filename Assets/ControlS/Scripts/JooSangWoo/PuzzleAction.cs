using System.Collections.Generic;
using System;
using UnityEngine;
[Serializable]
public class PuzzleAction 
{
    [Header("조건")]
    public List<GameCondition> Conditions;
    [Header("실행")]
    public BaseWindowedUI OpeningUI;
    public List<string> NarrationID;
    public List<GameCondition> ChagingConditions;
    public CollectionType CollectCollectionType;
    public CollectionType StartCollectionType;
    public int NeededCollectionCount;

    public bool OnAction()
    {
        foreach (var condition in Conditions)
        {
            if (!GameConditionManager.Instance.Condition.HasFlag(condition))
            {
                return false;
            }
        }
        if (OpeningUI != null)
        {
            OpeningUI.OpenWindow();
        }
        foreach (var narrationID in NarrationID)
        {
            //TODO: NarrationManager가  Play 하게
        }
        foreach (var changingCondition in ChagingConditions)
        {
            GameConditionManager.Instance.SetCondition(changingCondition);
        }
        if (CollectCollectionType != CollectionType.None)
        {
            CollectionSystem.Instance.AddCollection(CollectCollectionType);
        }
        if (StartCollectionType != CollectionType.None)
        {
            CollectionSystem.Instance.StartCollection(StartCollectionType, NeededCollectionCount);
        }
        return true;
    }
}
