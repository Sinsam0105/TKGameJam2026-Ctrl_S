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
    [Header("스피치 버블")]
    public List<string> SpeechID;
    [Header("Prompt")]
    public List<string> SystemID;
    [Header("조건 변경")]
    public List<GameCondition> ChangingConditions;
    [Header("컬렉션 추가")]
    public CollectionType CollectCollectionType;
    [Header("컬렉션 시작")]
    public CollectionType StartCollectionType;
    public int NeededCollectionCount;

    public bool OnAction(string uniqueCollectionId = null)
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
        foreach (var narrationID in SpeechID)
        {
            //TODO: ScriptManager가 Play 하게
            if (!string.IsNullOrWhiteSpace(narrationID))
                ScriptManager.Instance.Play(narrationID);
        }
        foreach (var systemID in SystemID)
        {
            //TODO: ScriptManager가 Play 하게
            if (!string.IsNullOrWhiteSpace(systemID))
                ScriptManager.Instance.Play(systemID);
        }
        foreach (var changingCondition in ChangingConditions)
        {
            GameConditionManager.Instance.SetCondition(changingCondition);
        }
        if (CollectCollectionType != CollectionType.None)
        {
            if (!CollectionSystem.Instance.AddCollection(CollectCollectionType, uniqueCollectionId))
                return false;
        }
        if (StartCollectionType != CollectionType.None)
        {
            CollectionSystem.Instance.StartCollection(StartCollectionType, NeededCollectionCount);
        }
        return true;
    }
}
