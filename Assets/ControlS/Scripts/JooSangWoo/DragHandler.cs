using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class DragHandler : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [SerializeField] private int pieceId;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private PictureCollector collector;
    private Transform homeParent;
    private Vector2 homePosition;
    private bool placed;
    private Coroutine returnRoutine;

    public int PieceId => pieceId;
    public bool IsPlaced => placed;

    public void Configure(int id, PictureCollector owner)
    {
        pieceId = id;
        collector = owner;
    }

    public void SetHomePosition(Vector2 position)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        homeParent = transform.parent;
        homePosition = position;
        if (!placed)
            rectTransform.anchoredPosition = homePosition;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        collector ??= GetComponentInParent<PictureCollector>();
        homeParent = transform.parent;
        homePosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (placed)
            return;

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!placed && canvas != null)
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        if (!placed)
            collector?.TryPlace(this, eventData.position, eventData.pressEventCamera);
    }

    public void SnapTo(RectTransform answer)
    {
        placed = true;
        transform.SetParent(answer, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = answer.rect.size;
        canvasGroup.blocksRaycasts = false;
    }

    public void RejectDrop()
    {
        if (returnRoutine != null)
            StopCoroutine(returnRoutine);
        returnRoutine = StartCoroutine(ShakeAndReturn());
    }

    public void ResetPiece()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (homeParent == null)
        {
            homeParent = transform.parent;
            homePosition = rectTransform.anchoredPosition;
        }

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        placed = false;
        transform.SetParent(homeParent, false);
        rectTransform.anchoredPosition = homePosition;
        canvasGroup.blocksRaycasts = true;
    }

    private IEnumerator ShakeAndReturn()
    {
        Vector2 origin = rectTransform.anchoredPosition;
        const float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float offset = Mathf.Sin(elapsed * 75f) * 10f * (1f - elapsed / duration);
            rectTransform.anchoredPosition = origin + Vector2.right * offset;
            yield return null;
        }

        rectTransform.anchoredPosition = homePosition;
        returnRoutine = null;
    }
}
