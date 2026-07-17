using Sinsam.SingletonSystem;
using System;
using UnityEngine;
[Flags]
public enum GameCondition
{
    None = 0,
    Test = 1 << 0,





    All
}
public class GameConditionManager : MonoSingleton<GameConditionManager>
{
    public float GameProgress => (float)Condition / (float)GameCondition.All;
    public GameCondition Condition { get; private set; }
    public void SetCondition(GameCondition condition)
    {
        Condition |= condition;
    }
}
