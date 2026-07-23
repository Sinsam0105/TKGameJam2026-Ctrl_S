using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 5단계 인증코드 입력 창. 전신거울에서 확인한 코드를 그대로 입력한다.
/// (웹캠 얼굴 인증을 대체한다.) 판정은 StageFiveFlowController가 맡는다.
/// StageTwoTimeInputWindow와 같은 골격이되, 코드 필드 하나만 둔다.
/// </summary>
[DisallowMultipleComponent]
public sealed class StageFiveCodeInputWindow : PuzzleWindowedUI
{
    [Header("Serialized Input")]
    [SerializeField] private InputField codeInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Text feedbackText;
    [SerializeField] private Text hintText;
    [SerializeField] private StageFiveFlowController flowController;

    protected override void Awake()
    {
        base.Awake();
        if (submitButton != null)
            submitButton.onClick.AddListener(Submit);
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        flowController?.RefreshCodeInputView();

        if (codeInput != null && codeInput.interactable)
            codeInput.ActivateInputField();
    }

    public void ResetView()
    {
        if (codeInput != null)
        {
            codeInput.text = string.Empty;
            codeInput.interactable = true;
        }

        if (submitButton != null)
            submitButton.interactable = true;
        if (feedbackText != null)
            feedbackText.text = string.Empty;
        if (hintText != null)
            hintText.text = string.Empty;

        gameObject.SetActive(false);
    }

    public void ShowAttemptState(int failedAttempts)
    {
        if (feedbackText != null)
            feedbackText.text = failedAttempts > 0
                ? $"인증코드가 일치하지 않습니다.  ({failedAttempts}회 실패)"
                : string.Empty;

        if (hintText != null)
            hintText.text = failedAttempts >= 2
                ? "전신거울에 나타난 코드를 다시 확인하세요."
                : string.Empty;
    }

    public void ShowSolved(string code)
    {
        if (codeInput != null)
        {
            codeInput.text = code;
            codeInput.interactable = false;
        }

        if (submitButton != null)
            submitButton.interactable = false;
        if (feedbackText != null)
            feedbackText.text = "USER VERIFIED";
        if (hintText != null)
            hintText.text = "Recovery Progress: 100%";
    }

    private void Submit()
    {
        flowController?.SubmitCode(codeInput != null ? codeInput.text : string.Empty);
    }

    protected override void OnDestroy()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveListener(Submit);

        base.OnDestroy();
    }
}
