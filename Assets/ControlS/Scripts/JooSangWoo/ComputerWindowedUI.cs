using System.Collections;
using System.Text;
using ScriptData;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    [SerializeField] private GameObject desktopDialogueRoot;
    [SerializeField] private Text desktopDialogueText;

    private RectTransform rectTransform;
    private Canvas canvas;
    private ScriptManager scriptManager;
    private Coroutine desktopDialogueRoutine;
    private string desktopDialogueFullText = string.Empty;
    private bool desktopDialogueTyping;
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
        scriptManager = ScriptManager.Instance;
        if (scriptManager != null)
        {
            scriptManager.BubbleStarted += OnBubbleStarted;
            scriptManager.BubbleFinished += OnBubbleFinished;
            scriptManager.ScriptFinished += OnScriptFinished;
        }

        SetSpeakerVisible(true);
        HideDesktopDialogue();

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

    private void Update()
    {
        if (!desktopDialogueTyping)
            return;

        bool complete = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        complete |= Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
        if (complete)
            CompleteDesktopDialogue();
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

    private void OnBubbleStarted(string scriptName, int index, SpeechBubbleData data)
    {
        if (!gameObject.activeInHierarchy || data == null || data.ObjectType != EObject.Player)
            return;

        SetSpeakerVisible(true);
        if (desktopDialogueRoot != null)
            desktopDialogueRoot.SetActive(true);

        if (desktopDialogueText == null)
            return;

        StringBuilder line = new StringBuilder();
        if (data.Bubble != null)
        {
            foreach (SpeechBubbleInfo info in data.Bubble)
                line.Append(info.Text);
        }

        desktopDialogueFullText = line.ToString();
        if (desktopDialogueRoutine != null)
            StopCoroutine(desktopDialogueRoutine);
        desktopDialogueRoutine = StartCoroutine(TypeDesktopDialogue(data));
    }

    private IEnumerator TypeDesktopDialogue(SpeechBubbleData data)
    {
        desktopDialogueTyping = true;
        desktopDialogueText.text = string.Empty;

        if (data.Bubble != null)
        {
            foreach (SpeechBubbleInfo info in data.Bubble)
            {
                desktopDialogueText.fontSize = info.FontSize;
                desktopDialogueText.color = info.FontColor;

                if (info.Speed == ESpeechBubbleSpeed.None)
                {
                    desktopDialogueText.text += info.Text;
                    continue;
                }

                float interval = 1f / Mathf.Max((float)info.Speed, 1f);
                for (int index = 0; index < info.Text.Length; index++)
                {
                    desktopDialogueText.text += info.Text[index];
                    if (index < info.Text.Length - 1)
                        yield return new WaitForSeconds(interval);
                }
            }
        }

        desktopDialogueTyping = false;
        desktopDialogueRoutine = null;
    }

    private void CompleteDesktopDialogue()
    {
        if (desktopDialogueRoutine != null)
        {
            StopCoroutine(desktopDialogueRoutine);
            desktopDialogueRoutine = null;
        }

        desktopDialogueTyping = false;
        if (desktopDialogueText != null)
            desktopDialogueText.text = desktopDialogueFullText;
    }

    private void OnBubbleFinished(string scriptName, int index)
    {
        HideDesktopDialogue();
    }

    private void OnScriptFinished(string scriptName)
    {
        HideDesktopDialogue();
    }

    private void SetSpeakerVisible(bool visible)
    {
        if (desktopSpeaker != null)
            desktopSpeaker.SetActive(visible);
    }

    private void HideDesktopDialogue()
    {
        if (desktopDialogueRoutine != null)
        {
            StopCoroutine(desktopDialogueRoutine);
            desktopDialogueRoutine = null;
        }

        desktopDialogueTyping = false;
        desktopDialogueFullText = string.Empty;
        if (desktopDialogueRoot != null)
            desktopDialogueRoot.SetActive(false);
        if (desktopDialogueText != null)
            desktopDialogueText.text = string.Empty;
    }

    private void OnDisable()
    {
        HideDesktopDialogue();
    }

    protected override void OnDestroy()
    {
        if (scriptManager != null)
        {
            scriptManager.BubbleStarted -= OnBubbleStarted;
            scriptManager.BubbleFinished -= OnBubbleFinished;
            scriptManager.ScriptFinished -= OnScriptFinished;
        }

        if (leaveComputerButton != null)
            leaveComputerButton.onClick.RemoveListener(OnLeaveComputer);

        base.OnDestroy();
    }
}
