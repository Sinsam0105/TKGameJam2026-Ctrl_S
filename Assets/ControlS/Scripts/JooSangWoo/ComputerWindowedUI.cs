using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ComputerWindowedUI : BaseWindowedUI,
    IDragHandler,
    IBeginDragHandler
{
    [Header("Desktop Setup")]
    [SerializeField] private bool openOnStart;
    [SerializeField] private bool draggable = true;
    [SerializeField] private Button leaveComputerButton;

    [Header("Desktop Speaker")]
    [SerializeField] private GameObject desktopSpeaker;

    private RectTransform rectTransform;
    private Canvas canvas;
    private bool allowUserExit;
    private bool systemVisibilityChange;

    public bool OpenOnStart => openOnStart;
    public bool IsOpen => gameObject.activeInHierarchy;
    public bool CanUserExit => allowUserExit;

    protected override void Awake()
    {
        base.Awake();

        rectTransform = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();

        if (leaveComputerButton != null)
            leaveComputerButton.onClick.AddListener(OnLeaveComputer);
    }

    private void Start()
    {
        SetSpeakerVisible(true);

        if (openOnStart)
            OpenForStory();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!draggable)
            return;

        UIManager.Instance?.SetWindowFront(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!draggable || rectTransform == null || canvas == null)
            return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void SetDesktopActive(bool active, bool allowUserExit)
    {
        if (active)
            OpenInternal(allowUserExit);
        else
            CloseForStory();
    }

    public void OpenDesktop()
    {
        OpenInternal(true);
    }

    public void CloseDesktop()
    {
        CloseWindow();
    }

    public void ToggleDesktop()
    {
        if (IsOpen)
            CloseDesktop();
        else
            OpenDesktop();
    }

    public void OpenForStory()
    {
        OpenInternal(false);
    }

    public void CloseForStory()
    {
        systemVisibilityChange = true;
        CloseWindow();
        systemVisibilityChange = false;
        SetUserExitEnabled(false);
    }

    public void SetUserExitEnabled(bool enabled)
    {
        allowUserExit = enabled;
        if (leaveComputerButton != null)
            leaveComputerButton.interactable = enabled;
    }

    public override void OpenWindow()
    {
        OpenDesktop();
    }

    private void OpenInternal(bool userCanExit)
    {
        SetUserExitEnabled(userCanExit);
        base.OpenWindow();
        SetSpeakerVisible(true);
        GameStateManager.Instance?.SetComputer();
    }

    public override void CloseWindow()
    {
        if (!allowUserExit && !systemVisibilityChange)
            return;

        base.CloseWindow();
        GameStateManager.Instance?.SetRoom();
    }

    private void OnLeaveComputer()
    {
        CloseDesktop();
    }

    private void SetSpeakerVisible(bool visible)
    {
        if (desktopSpeaker != null)
            desktopSpeaker.SetActive(visible);
    }

    protected override void OnDestroy()
    {
        if (leaveComputerButton != null)
            leaveComputerButton.onClick.RemoveListener(OnLeaveComputer);

        base.OnDestroy();
    }
}
