using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 3단계 버전 정렬 퍼즐. 카드를 슬롯에 정답 순서(A→B→C→D)로 놓으면
/// 접근 코드 ABCD가 도출된다(체크섬 없음). Compare로 두 카드의 편집 관계를 확인할 수 있다.
/// 슬롯/카드 개수는 직렬화 데이터로 정하므로 코드는 개수에 의존하지 않는다.
///
/// Compare 판정은 실제 이미지 비교가 아니라 정답 인덱스 산술로 처리한다.
///   diff = 나중에 고른 카드 - 먼저 고른 카드 (정답 인덱스 차)
///     +1  : 정방향 1칸 (정상 편집)
///     >=2 : 변경점 2개 이상
///     <=-1: 역방향 편집
/// </summary>
[DisallowMultipleComponent]
public sealed class VersionChainPuzzle : MonoBehaviour
{
    [Header("Slots & Cards")]
    [SerializeField] private List<RectTransform> slots = new();   // 정렬 타임라인 6칸
    [SerializeField] private List<VersionCard> cards = new();     // 카드 6장
    [SerializeField, Min(1f)] private float snapRadius = 90f;     // 슬롯 중심 스냅 반경(px)

    [Header("Compare UI")]
    [SerializeField] private Button compareButton;
    [SerializeField] private Button submitButton;
    [SerializeField] private Text compareResultText;
    [SerializeField] private Text hintText;
    [SerializeField] private Text codeText;                       // 도출된 접근 코드 표시

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip compareOkClip;
    [SerializeField] private AudioClip compareErrorClip;
    [SerializeField] private AudioClip solvedClip;

    [Header("Events")]
    [SerializeField] private UnityEvent onSolved = new();

    public UnityEvent OnSolved => onSolved;
    public bool IsSolved { get; private set; }
    public string AccessCode { get; private set; } = string.Empty;

    private readonly Dictionary<VersionCard, RectTransform> cardSlot = new();
    private readonly List<VersionCard> compareSelection = new();
    private int failedAttempts;

    private void Awake()
    {
        if (compareButton != null)
            compareButton.onClick.AddListener(RunCompare);
        if (submitButton != null)
            submitButton.onClick.AddListener(Validate);
    }

    private void OnDestroy()
    {
        if (compareButton != null)
            compareButton.onClick.RemoveListener(RunCompare);
        if (submitButton != null)
            submitButton.onClick.RemoveListener(Validate);
    }

    private void OnEnable()
    {
        ResetPuzzle();
    }

    public void ResetPuzzle()
    {
        IsSolved = false;
        AccessCode = string.Empty;
        failedAttempts = 0;
        cardSlot.Clear();
        compareSelection.Clear();

        // 카드를 슬롯에 섞어 배치한다. 슬롯 인덱스 = 초기 배치, 정답과 다르게 순환시킨다.
        for (int i = 0; i < cards.Count && i < slots.Count; i++)
        {
            VersionCard card = cards[i];
            if (card == null)
                continue;

            RectTransform slot = slots[(i + 3) % slots.Count]; // 정답과 어긋난 초기 배치
            PlaceInSlot(card, slot);
            card.SetSelected(false);
            card.SetHighlighted(false);
        }

        SetText(compareResultText, string.Empty);
        SetText(hintText, string.Empty);
        SetText(codeText, string.Empty);
        SetInteractable(compareButton, false);
        SetInteractable(submitButton, true);
    }

    // ── 드래그 배치 ────────────────────────────────────────────────
    public void OnCardDropped(VersionCard card, Vector2 screenPosition, Camera eventCamera)
    {
        if (IsSolved || card == null)
            return;

        RectTransform target = FindNearestSlot(screenPosition, eventCamera);
        if (target == null)
        {
            // 슬롯을 못 찾으면 원래 자리로 되돌린다.
            if (cardSlot.TryGetValue(card, out RectTransform home))
                card.Rect.position = home.position;
            return;
        }

        VersionCard occupant = FindCardInSlot(target);
        RectTransform previous = cardSlot.TryGetValue(card, out RectTransform p) ? p : null;

        if (occupant != null && occupant != card && previous != null)
            PlaceInSlot(occupant, previous); // 교환

        PlaceInSlot(card, target);
    }

    private void PlaceInSlot(VersionCard card, RectTransform slot)
    {
        cardSlot[card] = slot;
        card.Rect.SetParent(slot, false);
        card.Rect.anchorMin = card.Rect.anchorMax = new Vector2(0.5f, 0.5f);
        card.Rect.pivot = new Vector2(0.5f, 0.5f);
        card.Rect.anchoredPosition = Vector2.zero;
    }

    private RectTransform FindNearestSlot(Vector2 screenPosition, Camera cam)
    {
        RectTransform best = null;
        float bestDist = snapRadius;
        foreach (RectTransform slot in slots)
        {
            if (slot == null)
                continue;
            Vector2 slotScreen = RectTransformUtility.WorldToScreenPoint(cam, slot.position);
            float dist = Vector2.Distance(slotScreen, screenPosition);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = slot;
            }
        }
        return best;
    }

    private VersionCard FindCardInSlot(RectTransform slot)
    {
        foreach (KeyValuePair<VersionCard, RectTransform> pair in cardSlot)
        {
            if (pair.Value == slot)
                return pair.Key;
        }
        return null;
    }

    // ── Compare ────────────────────────────────────────────────────
    public void OnCardClicked(VersionCard card)
    {
        if (IsSolved || card == null)
            return;

        if (compareSelection.Contains(card))
        {
            compareSelection.Remove(card);
            card.SetSelected(false);
        }
        else
        {
            // 최대 2장. 넘치면 가장 오래된 선택을 뺀다.
            if (compareSelection.Count >= 2)
            {
                VersionCard oldest = compareSelection[0];
                compareSelection.RemoveAt(0);
                oldest.SetSelected(false);
            }
            compareSelection.Add(card);
            card.SetSelected(true);
        }

        SetInteractable(compareButton, compareSelection.Count == 2);
    }

    private void RunCompare()
    {
        if (compareSelection.Count != 2)
            return;

        int diff = compareSelection[1].AnswerIndex - compareSelection[0].AnswerIndex;

        if (diff == 1)
        {
            SetText(compareResultText, "1 Sequential Change Detected");
            PlayClip(compareOkClip);
        }
        else if (diff >= 2)
        {
            SetText(compareResultText, "Unresolved Changes");
            PlayClip(compareErrorClip);
        }
        else // diff <= 0 (0은 같은 카드지만 선택 구조상 나오지 않음)
        {
            SetText(compareResultText, "Reverse Edit Detected");
            PlayClip(compareErrorClip);
        }
    }

    // ── 검증 ───────────────────────────────────────────────────────
    private void Validate()
    {
        if (IsSolved)
            return;

        // 모든 슬롯이 채워졌는지 + 슬롯 순서대로 정답 인덱스가 1..6인지 확인한다.
        for (int i = 0; i < slots.Count; i++)
        {
            VersionCard card = FindCardInSlot(slots[i]);
            if (card == null || card.AnswerIndex != i + 1)
            {
                OnWrongAnswer();
                return;
            }
        }

        Solve();
    }

    private void OnWrongAnswer()
    {
        failedAttempts++;
        PlayClip(compareErrorClip);

        switch (failedAttempts)
        {
            case 1:
                SetText(hintText, "연결 순서가 맞지 않습니다.");
                break;
            case 2:
                SetText(hintText, "Some versions modify an existing image instead of adding a new element.");
                break;
            case 3:
                SetText(hintText, "Begin with the version containing only the project title.");
                break;
            default:
                SetText(hintText, "Begin with the version containing only the project title.");
                FlashFirstCard();
                break;
        }
    }

    private void FlashFirstCard()
    {
        foreach (VersionCard card in cards)
        {
            if (card != null && card.AnswerIndex == 1)
            {
                StopAllCoroutines();
                StartCoroutine(FlashRoutine(card));
                break;
            }
        }
    }

    private System.Collections.IEnumerator FlashRoutine(VersionCard card)
    {
        for (int i = 0; i < 3; i++)
        {
            card.SetHighlighted(true);
            yield return new WaitForSecondsRealtime(0.25f);
            card.SetHighlighted(false);
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private void Solve()
    {
        IsSolved = true;
        SetText(hintText, string.Empty);
        SetText(compareResultText, "Version Chain Verified.");
        SetInteractable(submitButton, false);
        SetInteractable(compareButton, false);

        // 슬롯 순서대로 파일 ID를 이으면 접근 코드가 된다.
        StringBuilder code = new StringBuilder();
        for (int i = 0; i < slots.Count; i++)
        {
            VersionCard card = FindCardInSlot(slots[i]);
            if (card != null)
                code.Append(card.FileId);
        }
        AccessCode = code.ToString();
        SetText(codeText, $"ACCESS CODE  {AccessCode}");

        PlayClip(solvedClip);
        GameConditionManager.Instance?.SetCondition(GameCondition.Stage3VersionSolved);
        onSolved?.Invoke();
    }

    // ── helpers ────────────────────────────────────────────────────
    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
            return;

        // 효과음은 일괄 SoundManager로 보낸다. 없으면 로컬 소스로 대체한다.
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

    private static void SetInteractable(Button button, bool value)
    {
        if (button != null)
            button.interactable = value;
    }
}
