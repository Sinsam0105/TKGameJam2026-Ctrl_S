using UnityEngine;
using UnityEngine.EventSystems;

public class ComputerWindowedUI : BaseWindowedUI,
    IDragHandler,
    IBeginDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    protected override void Awake()
    {
        base.Awake();

        rectTransform = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        UIManager.Instance.SetWindowFront(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null || canvas == null)
        {
            return;
        }

        rectTransform.anchoredPosition +=
            eventData.delta / canvas.scaleFactor;
    }
}