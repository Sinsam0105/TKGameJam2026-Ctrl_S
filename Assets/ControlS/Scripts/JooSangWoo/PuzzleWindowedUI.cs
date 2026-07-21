using UnityEngine.InputSystem;

public class PuzzleWindowedUI : BaseWindowedUI
{
    protected override void Awake()
    {
        base.Awake();
        if (WindowManager != null && WindowManager.Escape != null)
            WindowManager.Escape.performed += OnEscapePerformed;
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        GetComponentInChildren<PictureCollector>(true)?.ResetPuzzle();
        GameStateManager.Instance?.SetPuzzle();
    }

    public override void CloseWindow()
    {
        base.CloseWindow();
        GameStateManager.Instance?.SetRoom();
    }

    private void OnEscapePerformed(InputAction.CallbackContext context)
    {
        if (gameObject.activeInHierarchy)
        {
            CloseWindow();
        }
    }

    protected override void OnDestroy()
    {
        if (WindowManager != null && WindowManager.Escape != null)
        {
            WindowManager.Escape.performed -= OnEscapePerformed;
        }

        base.OnDestroy();
    }
}
