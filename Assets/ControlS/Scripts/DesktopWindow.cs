using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ControlS
{
    /// <summary>씬에 미리 배치된 데스크톱 창의 공통 열기/닫기 동작입니다.</summary>
    public sealed class DesktopWindow : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Button closeButton;
        [SerializeField] private float openTone = 440f;
        [SerializeField] private UnityEvent onOpened = new UnityEvent();

        private ControlSSceneController scene;

        public GameObject Root => root != null ? root.gameObject : gameObject;
        public UnityEvent OnOpened => onOpened;
        public bool IsOpen => Root.activeSelf;

        public void Configure(RectTransform windowRoot, Button close, float tone)
        {
            root = windowRoot;
            closeButton = close;
            openTone = tone;
        }

        public void Initialize(ControlSSceneController owner)
        {
            scene = owner;
            Root.SetActive(false);
        }

        public bool ValidateReferences() => root != null && closeButton != null;

        public void Open()
        {
            Root.SetActive(true);
            root.SetAsLastSibling();
            scene?.PlayUiTone(openTone, .05f);
            onOpened?.Invoke();
        }

        public void Close() => Root.SetActive(false);
    }
}
