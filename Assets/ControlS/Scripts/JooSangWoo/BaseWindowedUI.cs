using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaseWindowedUI : MonoBehaviour
{
    [SerializeField] private Button closeButton;

    public UnityEvent OnOpen = new UnityEvent();
    public UnityEvent OnClose = new UnityEvent();

    protected virtual void Awake()
    {
        OpenWindow();
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }
    }

    public virtual void OpenWindow()
    {
        UIManager.Instance.RegisterWindow(this);

        gameObject.SetActive(true);
        UIManager.Instance.SetWindowFront(this);

        OnOpen?.Invoke();
    }

    public virtual void CloseWindow()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        OnClose?.Invoke();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetWindowBack(this);
        }

        gameObject.SetActive(false);
    }

    protected virtual void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseWindow);
        }
    }
}