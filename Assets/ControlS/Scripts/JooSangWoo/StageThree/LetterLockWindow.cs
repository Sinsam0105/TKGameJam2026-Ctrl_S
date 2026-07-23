using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 백업 상자의 알파벳 문자 잠금. 정답 코드(기본 ABCD)를 순서대로 누르면 상자가 열린다.
/// 코드는 3단계 정렬 퍼즐이 도출한 버전 순서 그대로이며(체크섬 없음),
/// StageThreeFlowController가 SetExpectedCode로 넘겨준다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LetterLockWindow : PuzzleWindowedUI
{
    [Header("Letter Lock")]
    [Tooltip("정답 코드. 3단계 정렬 퍼즐 결과(예: ABCD)를 그대로 쓴다. 체크섬 없음.")]
    [SerializeField] private string expectedCode = "ABCD";
    [Tooltip("버튼 순서에 대응하는 문자 라벨. 버튼 수와 개수를 맞춘다.")]
    [SerializeField] private List<string> letterLabels = new() { "A", "B", "C", "D" };
    [SerializeField] private List<Button> letterButtons = new();  // letterLabels 순서
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

    private readonly StringBuilder entry = new();

    /// <summary>3단계 정렬 퍼즐이 도출한 코드를 정답으로 설정한다. (체크섬 없이 그대로)</summary>
    public void SetExpectedCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(code))
            expectedCode = code.ToUpperInvariant();
    }

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < letterButtons.Count && i < letterLabels.Count; i++)
        {
            string letter = letterLabels[i];
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
            SetText(entryText, expectedCode);
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
        if (IsOpened || entry.Length >= expectedCode.Length)
            return;

        entry.Append(letter);
        PlayClip(keyClip);
        SetText(entryText, entry.ToString());

        if (entry.Length < expectedCode.Length)
            return;

        if (entry.ToString() == expectedCode)
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
        if (clip == null)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(clip);
        else if (sfxSource != null)
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
