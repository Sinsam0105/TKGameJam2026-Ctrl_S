using System.Collections.Generic;
using Sinsam.SingletonSystem;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoSingleton<UIManager>
{
    [SerializeField] private InputActionAsset inputActionsAsset;

    private readonly List<BaseWindowedUI> windowQueue =
        new List<BaseWindowedUI>();

    private InputAction escapeAction;

    public InputAction Escape => escapeAction;

    protected override void Awake()
    {
        base.Awake();

        if (inputActionsAsset == null)
        {
            Debug.LogError("UIManager: InputActionAsset이 연결되지 않았습니다.");
            return;
        }

        escapeAction = inputActionsAsset.FindAction("Escape", false);

        if (escapeAction == null)
        {
            Debug.LogError(
                "UIManager: 이름이 Escape인 InputAction을 찾을 수 없습니다.");
            return;
        }

        escapeAction.performed += OnEscapePerformed;
        escapeAction.Enable();

        GameStateManager.Instance.OnGameStateChanged +=
            OnGameStateChanged;
    }

    private void OnEscapePerformed(
        InputAction.CallbackContext context)
    {
        OnEscape();
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Computer)
        {
            OnComputer();
        }
        else if (state == GameState.Puzzle)
        {
            OnPuzzle();
        }
    }

    public void RegisterWindow(BaseWindowedUI window)
    {
        if (window == null)
        {
            return;
        }

        if (!windowQueue.Contains(window))
        {
            windowQueue.Add(window);
        }

        if (window.transform.parent != transform)
        {
            window.transform.SetParent(transform, false);
        }

        SetWindowFront(window);
    }

    public void UnregisterWindow(BaseWindowedUI window)
    {
        windowQueue.Remove(window);
    }

    public void OnComputer()
    {
        BaseWindowedUI[] windows = windowQueue.ToArray();

        foreach (BaseWindowedUI window in windows)
        {
            if (window != null &&
                window is not ComputerWindowedUI)
            {
                window.CloseWindow();
            }
        }
    }

    public void OnPuzzle()
    {
        BaseWindowedUI[] windows = windowQueue.ToArray();

        foreach (BaseWindowedUI window in windows)
        {
            if (window != null &&
                window is not PuzzleWindowedUI)
            {
                window.CloseWindow();
            }
        }
    }

    public void SetWindowFront(BaseWindowedUI window)
    {
        if (window == null || !windowQueue.Contains(window))
        {
            return;
        }

        windowQueue.Remove(window);
        windowQueue.Insert(0, window);
        window.transform.SetAsLastSibling();
    }

    public void SetWindowBack(BaseWindowedUI window)
    {
        if (window == null || !windowQueue.Contains(window))
        {
            return;
        }

        windowQueue.Remove(window);
        windowQueue.Add(window);
        window.transform.SetAsFirstSibling();
    }

    public void OnEscape()
    {
        for (int i = 0; i < windowQueue.Count; i++)
        {
            BaseWindowedUI window = windowQueue[i];

            if (window == null)
            {
                windowQueue.RemoveAt(i);
                i--;
                continue;
            }

            if (window.gameObject.activeInHierarchy)
            {
                window.CloseWindow();
                return;
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (escapeAction != null)
        {
            escapeAction.performed -= OnEscapePerformed;
            escapeAction.Disable();
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged -=
                OnGameStateChanged;
        }
    }
}