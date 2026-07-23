using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 3단계 버전 카드 한 장. 드래그로 슬롯에 배치하고, 짧게 누르면 Compare 선택이 된다.
/// 실제 정렬/판정은 VersionChainPuzzle이 맡고, 이 컴포넌트는 입력만 위임한다.
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public sealed class VersionCard : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private string fileId = "A";     // 파일 식별자 (A/B/C/D)
    [SerializeField] private int answerIndex = 1;     // 정답 순서 (1~6)
    [SerializeField] private Text labelText;          // 카드에 표시할 요소 설명
    [SerializeField] private Image selectionFrame;    // Compare 선택 테두리
    [SerializeField] private Image highlightFrame;    // 힌트 점멸 테두리

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private VersionChainPuzzle puzzle;
    private bool dragging;

    public string FileId => fileId;
    public int AnswerIndex => answerIndex;
    public RectTransform Rect => rectTransform;

    public void Configure(VersionChainPuzzle owner, string id, int index, string label)
    {
        puzzle = owner;
        fileId = id;
        answerIndex = index;
        if (labelText != null)
            labelText.text = label;
        SetSelected(false);
        SetHighlighted(false);
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        puzzle ??= GetComponentInParent<VersionChainPuzzle>();
    }

    public void SetSelected(bool value)
    {
        if (selectionFrame != null)
            selectionFrame.enabled = value;
    }

    public void SetHighlighted(bool value)
    {
        if (highlightFrame != null)
            highlightFrame.enabled = value;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas != null)
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        dragging = false;
        puzzle?.OnCardDropped(this, eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 드래그로 끝난 입력은 클릭으로 치지 않는다.
        if (dragging || eventData.dragging)
            return;
        puzzle?.OnCardClicked(this);
    }
}
