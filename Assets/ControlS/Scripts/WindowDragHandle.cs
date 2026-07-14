using UnityEngine;
using UnityEngine.EventSystems;

namespace ControlS
{
    public sealed class WindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Canvas canvas;

        public void Configure(RectTransform window, Canvas owner)
        {
            target = window;
            canvas = owner;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (target != null) target.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null || canvas == null) return;
            target.anchoredPosition += eventData.delta / Mathf.Max(.01f, canvas.scaleFactor);
        }
    }
}
