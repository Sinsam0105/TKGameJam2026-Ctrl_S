using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 백업 상자의 A~F 문자 잠금. 정답 코드(DBFAEC)를 순서대로 누르면 상자가 열린다.
/// 코드 자체는 3단계 정렬 퍼즐이 도출하지만, 판정용 정답은 여기에 고정으로 둔다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LetterLockWindow : PuzzleWindowedUI
{
    private const string CorrectCode = "DBFAEC";

    [Header("Letter Lock")]
    [SerializeField] private List<Button> letterButtons = new();  // A~F 순서
    [SerializeField] private Text entryText;                      // 입력 중인 문자열
    [SerializeField] private Text feedbackText;
    [SerializeField] private Button clearButton;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip keyClip;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip errorClip;

    [Header("Events")]
    [SerializeField] private UnityEvent onBoxOpened = new();

    public UnityEvent OnBoxOpened => onBoxOpened;
    public bool IsOpened { get; private set; }

    private static readonly string[] Letters = { "A", "B", "C", "D", "E", "F" };
    private readonly StringBuilder entry = new();

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < letterButtons.Count && i < Letters.Length; i++)
        {
            string letter = Letters[i];
            Button button = letterButtons[i];
            if (button != null)
                button.onClick.AddListener(() => Press(letter));
        }

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearEntry);
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        if (IsOpened)
        {
            SetText(entryText, CorrectCode);
            SetText(feedbackText, "UNLOCKED");
            SetButtonsInteractable(false);
        }
        else
        {
            ClearEntry();
            SetButtonsInteractable(true);
        }
    }

    public void ResetView()
    {
        IsOpened = false;
        ClearEntry();
        gameObject.SetActive(false);
    }

    private void Press(string letter)
    {
        if (IsOpened || entry.Length >= CorrectCode.Length)
            return;

        entry.Append(letter);
        PlayClip(keyClip);
        SetText(entryText, entry.ToString());

        if (entry.Length < CorrectCode.Length)
            return;

        if (entry.ToString() == CorrectCode)
            Open();
        else
            Fail();
    }

    private void Open()
    {
        IsOpened = true;
        SetText(feedbackText, "UNLOCKED");
        SetButtonsInteractable(false);
        PlayClip(openClip);
        GameConditionManager.Instance?.SetCondition(GameCondition.Stage3BoxOpened);
        onBoxOpened?.Invoke();
    }

    private void Fail()
    {
        SetText(feedbackText, "코드가 일치하지 않습니다.");
        PlayClip(errorClip);
        entry.Clear();
        SetText(entryText, string.Empty);
    }

    private void ClearEntry()
    {
        entry.Clear();
        SetText(entryText, string.Empty);
        if (!IsOpened)
            SetText(feedbackText, string.Empty);
    }

    private void SetButtonsInteractable(bool value)
    {
        foreach (Button button in letterButtons)
        {
            if (button != null)
                button.interactable = value;
        }
        if (clearButton != null)
            clearButton.interactable = value;
    }

    private void PlayClip(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    private static void SetText(Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    protected override void OnDestroy()
    {
        foreach (Button button in letterButtons)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
        if (clearButton != null)
            clearButton.onClick.RemoveListener(ClearEntry);

        base.OnDestroy();
    }
}
