using Sinsam.SingletonSystem;
using UnityEngine;
using System;
public enum GameState
{
    UI,
    Puzzle,
    Computer,
    Room,
}
public class GameStateManager: MonoSingleton<GameStateManager>
{
    public GameState State { get; private set; } = GameState.Room;
    public event Action<GameState> OnGameStateChanged;
    [SerializeField] private GameState initialState = GameState.Room;
    void Start()
    {
        SetState(initialState);
    }
    public void SetState(GameState newState)
    {
        State = newState;
        OnGameStateChanged?.Invoke(State);
    }
    [ContextMenu("Set Room State")]
    public void SetRoom()
    {
        SetState(GameState.Room);
    }
    [ContextMenu("Set Puzzle State")]
    public void SetPuzzle()
    {
        SetState(GameState.Puzzle);
    }
    [ContextMenu("Set Computer State")]
    public void SetComputer()
    {
        SetState(GameState.Computer);
    }
}
