using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaseWindowedUI : MonoBehaviour
{
    [SerializeField] private Button closeButton;

    public UnityEvent OnOpen = new UnityEvent();
    public UnityEvent OnClose = new UnityEvent();
    private UIManager windowManager;

    protected UIManager WindowManager => windowManager;

    protected virtual void Awake()
    {
        windowManager = UIManager.Instance;
        if (windowManager != null)
        {
            windowManager.RegisterWindow(this);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }
    }

    public virtual void OpenWindow()
    {
        if (windowManager == null)
            windowManager = UIManager.Instance;
        if (windowManager != null)
        {
            windowManager.RegisterWindow(this);
        }

        gameObject.SetActive(true);
        if (windowManager != null)
        {
            windowManager.SetWindowFront(this);
        }

        OnOpen?.Invoke();
    }

    public virtual void CloseWindow()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        OnClose?.Invoke();

        if (windowManager != null)
        {
            windowManager.SetWindowBack(this);
        }

        gameObject.SetActive(false);
    }

    protected virtual void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseWindow);
        }

        if (windowManager != null)
            windowManager.UnregisterWindow(this);
    }
}
