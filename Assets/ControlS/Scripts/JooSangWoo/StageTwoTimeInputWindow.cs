using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StageTwoTimeInputWindow : PuzzleWindowedUI
{
    [Header("Serialized Input")]
    [SerializeField] private InputField hourInput;
    [SerializeField] private InputField minuteInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Text feedbackText;
    [SerializeField] private Text hintText;
    [SerializeField] private StageTwoFlowController flowController;

    protected override void Awake()
    {
        base.Awake();
        ConfigureInput(hourInput);
        ConfigureInput(minuteInput);

        if (submitButton != null)
            submitButton.onClick.AddListener(Submit);
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        flowController?.RefreshTimeInputView();

        if (hourInput != null && hourInput.interactable)
            hourInput.ActivateInputField();
    }

    public void ResetView()
    {
        if (hourInput != null)
        {
            hourInput.text = string.Empty;
            hourInput.interactable = true;
        }

        if (minuteInput != null)
        {
            minuteInput.text = string.Empty;
            minuteInput.interactable = true;
        }

        if (submitButton != null)
            submitButton.interactable = true;
        if (feedbackText != null)
            feedbackText.text = string.Empty;
        if (hintText != null)
            hintText.text = string.Empty;

        gameObject.SetActive(false);
    }

    public void ShowAttemptState(int failedAttempts, bool cluesHighlighted)
    {
        if (feedbackText != null)
            feedbackText.text = failedAttempts > 0
                ? $"입력 시간이 일치하지 않습니다.  ({failedAttempts}회 실패)"
                : string.Empty;

        if (hintText == null)
            return;

        if (cluesHighlighted)
            hintText.text = "단서 위치가 강조되었습니다. 전자레인지, 포스트잇, 외부 시계를 다시 확인하세요.";
        else if (failedAttempts >= 2)
            hintText.text = "힌트: 포스트잇에 적힌 시간 차이를 전자레인지 시각에서 빼 보세요.";
        else
            hintText.text = string.Empty;
    }

    public void ShowSolved()
    {
        if (hourInput != null)
        {
            hourInput.text = "03";
            hourInput.interactable = false;
        }

        if (minuteInput != null)
        {
            minuteInput.text = "05";
            minuteInput.interactable = false;
        }

        if (submitButton != null)
            submitButton.interactable = false;
        if (feedbackText != null)
            feedbackText.text = "ACTIVITY LOG RESTORED";
        if (hintText != null)
            hintText.text = "Recovery Progress: 40%";
    }

    private void Submit()
    {
        flowController?.SubmitTime(hourInput != null ? hourInput.text : string.Empty,
            minuteInput != null ? minuteInput.text : string.Empty);
    }

    private static void ConfigureInput(InputField input)
    {
        if (input == null)
            return;

        input.contentType = InputField.ContentType.IntegerNumber;
        input.characterLimit = 2;
        input.onValidateInput = ValidateDigit;
    }

    private static char ValidateDigit(string text, int characterIndex, char addedChar)
    {
        return char.IsDigit(addedChar) ? addedChar : '\0';
    }

    protected override void OnDestroy()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveListener(Submit);

        base.OnDestroy();
    }
}
