using UnityEngine.InputSystem;

public class PuzzleWindowedUI : BaseWindowedUI
{
    protected override void Awake()
    {
        base.Awake();
        UIManager.Instance.Escape.performed += OnEscapePerformed;
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
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Escape.performed -= OnEscapePerformed;
        }

        base.OnDestroy();
    }
}