using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ControlS
{
    /// <summary>데스크톱 루트와 공통 입력만 관리합니다. 아이콘과 창 콘텐츠는 개별 컴포넌트가 소유합니다.</summary>
    public sealed class VirtualDesktop : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform desktopRoot;
        [SerializeField] private Text taskbarClock;
        [SerializeField] private Button closeButton;
        [SerializeField] private DesktopShortcut[] shortcuts = Array.Empty<DesktopShortcut>();
        [SerializeField] private DesktopWindow[] windows = Array.Empty<DesktopWindow>();

        private ControlSSceneController game;
        private ControlSState state;

        public bool IsOpen => desktopRoot != null && desktopRoot.gameObject.activeSelf;
        public GameObject DesktopUI => desktopRoot != null ? desktopRoot.gameObject : null;
        public DesktopShortcut[] Shortcuts => shortcuts;
        public DesktopWindow[] Windows => windows;

        public void Configure(Canvas ownerCanvas, RectTransform root, Text clock, Button close,
            DesktopShortcut[] desktopShortcuts, DesktopWindow[] desktopWindows)
        {
            canvas = ownerCanvas;
            desktopRoot = root;
            taskbarClock = clock;
            closeButton = close;
            shortcuts = desktopShortcuts ?? Array.Empty<DesktopShortcut>();
            windows = desktopWindows ?? Array.Empty<DesktopWindow>();
        }

        public void Initialize(ControlSSceneController owner, ControlSState gameState)
        {
            game = owner;
            state = gameState;
            if (!ValidateReferences())
            {
                Debug.LogError("Virtual Desktop scene references are incomplete: " + GetValidationError(), this);
                enabled = false;
                return;
            }

            foreach (var window in windows) window.Initialize(owner);
            foreach (var shortcut in shortcuts) shortcut.Initialize(owner);
            desktopRoot.gameObject.SetActive(false);
        }

        public bool ValidateReferences()
        {
            return string.IsNullOrEmpty(GetValidationError());
        }

        public string GetValidationError()
        {
            if (canvas == null) return "canvas";
            if (desktopRoot == null) return "desktopRoot";
            if (taskbarClock == null) return "taskbarClock";
            if (closeButton == null) return "closeButton";
            if (shortcuts == null || shortcuts.Length == 0) return "shortcuts";
            if (windows == null || windows.Length == 0) return "windows";
            for (var i = 0; i < shortcuts.Length; i++)
                if (shortcuts[i] == null || !shortcuts[i].ValidateReferences())
                    return "shortcut[" + i + "] " + (shortcuts[i] == null ? "null" : shortcuts[i].name);
            for (var i = 0; i < windows.Length; i++)
                if (windows[i] == null || !windows[i].ValidateReferences())
                    return "window[" + i + "] " + (windows[i] == null ? "null" : windows[i].name);
            return string.Empty;
        }

        public void Open()
        {
            if (desktopRoot == null || state == null || state.EndingStarted) return;
            desktopRoot.gameObject.SetActive(true);
            desktopRoot.SetAsLastSibling();
            game.SetPrompt(string.Empty);
            game.PlayUiTone(520f, .07f);
        }

        public void Close()
        {
            if (desktopRoot == null) return;
            desktopRoot.gameObject.SetActive(false);
            game?.PlayUiTone(300f, .05f);
        }

        private void Update()
        {
            if (!IsOpen) return;
            taskbarClock.text = DateTime.Now.ToString("HH:mm") + "  ▣";
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
        }
    }
}
