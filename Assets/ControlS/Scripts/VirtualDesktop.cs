using System;
using Sinsam.SingletonSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class VirtualDesktop : MonoSingleton<VirtualDesktop>
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform desktopRoot;
        [SerializeField] private Text taskbarClock;
        [SerializeField] private Button closeButton;
        [SerializeField] private ProgressFlagSO blockOpenFlag;
        [SerializeField] private DesktopShortcut[] shortcuts = Array.Empty<DesktopShortcut>();
        [SerializeField] private DesktopWindow[] windows = Array.Empty<DesktopWindow>();

        private ProgressManager progressManager;
        private ContentManager contentManager;
        private UIManager uiManager;
        private SoundManager soundManager;

        public bool IsOpen => desktopRoot != null && desktopRoot.gameObject.activeSelf;
        public GameObject DesktopUI => desktopRoot != null ? desktopRoot.gameObject : null;
        public DesktopShortcut[] Shortcuts => shortcuts;
        public DesktopWindow[] Windows => windows;

        protected override bool ShouldPersist() => false;

        protected override void Awake()
        {
            base.Awake();
            if (!ValidateReferences())
            {
                Debug.LogError("Virtual Desktop references are incomplete: " + GetValidationError(), this);
                enabled = false;
                return;
            }
            desktopRoot.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            progressManager = ProgressManager.Instance;
            contentManager = ContentManager.Instance;
            uiManager = UIManager.Instance;
            soundManager = SoundManager.Instance;
            if (progressManager != null) progressManager.ProgressChanged += RefreshShortcuts;
            if (contentManager != null) contentManager.ContentChanged += HandleContentChanged;
            ApplyContent();
            RefreshShortcuts();
        }

        private void OnDisable()
        {
            if (progressManager != null) progressManager.ProgressChanged -= RefreshShortcuts;
            if (contentManager != null) contentManager.ContentChanged -= HandleContentChanged;
            progressManager = null;
            contentManager = null;
            uiManager = null;
            soundManager = null;
        }

        public bool ValidateReferences() => string.IsNullOrEmpty(GetValidationError());

        public string GetValidationError()
        {
            if (canvas == null) return "canvas";
            if (desktopRoot == null) return "desktopRoot";
            if (taskbarClock == null) return "taskbarClock";
            if (closeButton == null) return "closeButton";
            if (shortcuts == null || shortcuts.Length == 0) return "shortcuts";
            if (windows == null || windows.Length == 0) return "windows";
            for (var i = 0; i < shortcuts.Length; i++)
                if (shortcuts[i] == null || !shortcuts[i].ValidateReferences()) return "shortcut[" + i + "]";
            for (var i = 0; i < windows.Length; i++)
                if (windows[i] == null || !windows[i].ValidateReferences()) return "window[" + i + "]";
            return string.Empty;
        }

        public void Open()
        {
            if (desktopRoot == null || (blockOpenFlag != null && progressManager != null && progressManager.GetFlag(blockOpenFlag))) return;
            desktopRoot.gameObject.SetActive(true);
            desktopRoot.SetAsLastSibling();
            uiManager?.SetPrompt(string.Empty);
            soundManager?.PlayUiTone(520f, .07f);
        }

        public void Close()
        {
            if (desktopRoot == null) return;
            desktopRoot.gameObject.SetActive(false);
            soundManager?.PlayUiTone(300f, .05f);
        }

        private void Update()
        {
            if (!IsOpen) return;
            taskbarClock.text = DateTime.Now.ToString("HH:mm") + "  ▣";
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) Close();
        }

        private void RefreshShortcuts()
        {
            if (shortcuts == null) return;
            foreach (var shortcut in shortcuts) if (shortcut != null) shortcut.RefreshVisibility();
        }

        private void HandleContentChanged(GameContentSetSO _) => ApplyContent();

        public void ApplyContent()
        {
            var set = contentManager != null ? contentManager.Current : null;
            var theme = set != null ? set.desktopTheme : null;
            if (desktopRoot == null || theme == null) return;
            var wallpaper = desktopRoot.GetComponent<Image>();
            if (wallpaper != null)
            {
                if (theme.wallpaper != null) wallpaper.sprite = theme.wallpaper;
                wallpaper.color = theme.wallpaperColor;
            }
            var watermark = FindText(desktopRoot, "Watermark");
            if (watermark != null) watermark.text = theme.watermark;
            var closeLabel = closeButton != null ? closeButton.GetComponentInChildren<Text>(true) : null;
            if (closeLabel != null) closeLabel.text = theme.exitButton;
            if (set.readme != null)
            {
                var readme = Array.Find(windows, item => item != null && item.name.Contains("Readme"));
                if (readme != null)
                {
                    var document = FindText(readme.transform, "Document");
                    if (document != null) document.text = set.readme.body;
                }
            }
        }

        private static Text FindText(Transform root, string objectName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == objectName && child.TryGetComponent<Text>(out var text)) return text;
            return null;
        }
    }
}
