#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace ControlS.Editor
{
    internal static class ControlSDesktopSceneBuilder
    {
        public static VirtualDesktop Build(Transform parent, ControlSContent content,
            InteractionSequence endingSequence)
        {
            var systemObject = new GameObject("Virtual Desktop System");
            systemObject.transform.SetParent(parent, false);
            var desktop = systemObject.AddComponent<VirtualDesktop>();

            var canvasObject = new GameObject("Virtual Desktop Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(systemObject.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            var root = RuntimeUI.Rect("Desktop", canvas.transform);
            RuntimeUI.Stretch(root);
            var wallpaper = root.gameObject.AddComponent<Image>();
            wallpaper.sprite = RuntimeUI.WhiteSprite;
            wallpaper.color = new Color(.025f, .095f, .105f, 1f);
            RuntimeUI.Panel("Wallpaper line A", root, new Color(.11f, .37f, .36f, .35f),
                new Vector2(.38f, .08f), new Vector2(.39f, .93f), Vector2.zero, Vector2.zero).raycastTarget = false;
            RuntimeUI.Panel("Wallpaper line B", root, new Color(.11f, .37f, .36f, .25f),
                new Vector2(.38f, .57f), new Vector2(.92f, .58f), Vector2.zero, Vector2.zero).raycastTarget = false;
            var watermark = RuntimeUI.Text("Watermark", root, content.desktop.watermark, 34,
                new Color(.18f, .48f, .45f, .2f), TextAnchor.LowerRight);
            watermark.rectTransform.offsetMin = new Vector2(0, 64);
            watermark.rectTransform.offsetMax = new Vector2(-46, -32);

            var iconLayer = RuntimeUI.Rect("Desktop Icons", root);
            RuntimeUI.Stretch(iconLayer);
            iconLayer.offsetMax = new Vector2(0, -20);
            var windowLayer = RuntimeUI.Rect("Windows", root);
            RuntimeUI.Stretch(windowLayer);

            var taskbar = RuntimeUI.Panel("Taskbar", root, new Color(.025f, .03f, .038f, .98f),
                Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 58));
            var closeDesktop = RuntimeUI.Button("Leave Computer", taskbar.transform, content.desktop.exitButton, null,
                new Color(.08f, .11f, .12f, 1f), new Color(.12f, .28f, .26f, 1f), 19);
            var closeRect = closeDesktop.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0, 0);
            closeRect.anchorMax = new Vector2(0, 1);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = new Vector2(240, 0);
            var clock = RuntimeUI.Text("Clock", taskbar.transform, "02:17  ▣", 19,
                new Color(.82f, .88f, .87f), TextAnchor.MiddleCenter);
            clock.rectTransform.anchorMin = new Vector2(1, 0);
            clock.rectTransform.anchorMax = new Vector2(1, 1);
            clock.rectTransform.offsetMin = new Vector2(-155, 0);
            clock.rectTransform.offsetMax = Vector2.zero;

            var windows = new List<DesktopWindow>();
            var readme = BuildReadme(windowLayer, canvas, content, windows);
            var recovery = BuildRecovery(windowLayer, canvas, content, windows);
            var photo = BuildPhoto(windowLayer, canvas, content, windows);
            var recycle = BuildRecycleBin(windowLayer, canvas, content, windows);
            var archive = BuildArchive(windowLayer, canvas, content, windows);
            var recovered = BuildRecovered(windowLayer, canvas, content, endingSequence, windows);

            var shortcuts = new List<DesktopShortcut>
            {
                BuildShortcut(iconLayer, content.desktop.readmeIcon, "TXT", 0, readme),
                BuildShortcut(iconLayer, content.desktop.recoveryIcon, "DIR", 1, recovery),
                BuildShortcut(iconLayer, content.desktop.darkPhotoIcon, "IMG", 2, photo),
                BuildShortcut(iconLayer, content.desktop.recycleBinIcon, "BIN", 3, recycle),
                BuildShortcut(iconLayer, content.desktop.archiveIcon, "LOCK", 4, archive),
                BuildShortcut(iconLayer, content.desktop.recoveredIcon, "SAVE", 5, recovered,
                    new InteractionCondition[]
                    {
                        new ControlSFlagCondition(ControlSFlag.ArchiveSequenceSolved, true)
                    })
            };

            desktop.Configure(canvas, root, clock, closeDesktop, shortcuts.ToArray(), windows.ToArray());
            UnityEventTools.AddPersistentListener(closeDesktop.onClick, desktop.Close);
            root.gameObject.SetActive(false);
            EditorUtility.SetDirty(desktop);
            return desktop;
        }

        private static DesktopShortcut BuildShortcut(RectTransform parent, string label, string type, int row,
            DesktopWindow window, IEnumerable<InteractionCondition> visibility = null)
        {
            var button = RuntimeUI.Button("Icon " + label, parent, $"[{type}]\n{label}", null,
                new Color(.03f, .08f, .09f, .22f), new Color(.13f, .42f, .39f, .7f), 19);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(34, -34 - row * 128);
            rect.sizeDelta = new Vector2(172, 108);
            var shortcut = button.gameObject.AddComponent<DesktopShortcut>();
            shortcut.Configure(button, button.GetComponentInChildren<Text>(), visibility,
                new[]
                {
                    new InteractionRule("창 열기", null,
                        new InteractionAction[] { new OpenDesktopWindowAction(window) })
                });
            UnityEventTools.AddPersistentListener(button.onClick, shortcut.Execute);
            EditorUtility.SetDirty(shortcut);
            return shortcut;
        }

        private static DesktopWindow BuildReadme(RectTransform parent, Canvas canvas, ControlSContent content,
            List<DesktopWindow> windows)
        {
            var refs = BuildWindow(parent, canvas, "README", content.desktop.readmeTitle,
                new Vector2(720, 530), new Vector2(80, 70), 600f);
            var text = RuntimeUI.Text("Document", refs.Content, content.desktop.readmeBody, 25,
                new Color(.8f, .86f, .84f), TextAnchor.UpperLeft);
            text.rectTransform.offsetMin = new Vector2(14, 10);
            text.rectTransform.offsetMax = new Vector2(-14, -10);
            windows.Add(refs.Window);
            return refs.Window;
        }

        private static DesktopWindow BuildRecovery(RectTransform parent, Canvas canvas, ControlSContent content,
            List<DesktopWindow> windows)
        {
            var d = content.desktop;
            var refs = BuildWindow(parent, canvas, "Recovery", d.recoveryTitle,
                new Vector2(780, 500), new Vector2(35, 30), 460f);
            var locked = StatePanel("Locked", refs.Content);
            var lockedInfo = RuntimeUI.Text("Info", locked.transform, d.recoveryLocked, 24,
                new Color(.78f, .86f, .84f), TextAnchor.UpperLeft);
            lockedInfo.rectTransform.anchorMin = new Vector2(0, .48f);
            lockedInfo.rectTransform.anchorMax = Vector2.one;
            lockedInfo.rectTransform.offsetMin = new Vector2(24, 0);
            lockedInfo.rectTransform.offsetMax = new Vector2(-24, -20);
            var password = RuntimeUI.Input("Password", locked.transform, d.passwordPlaceholder);
            RuntimeUI.Place(password.GetComponent<RectTransform>(), 0, -38, 430, 62);
            password.characterLimit = 4;
            password.contentType = InputField.ContentType.IntegerNumber;
            var unlock = RuntimeUI.Button("Unlock", locked.transform, d.unlockButton, null,
                new Color(.08f, .36f, .32f, 1f), new Color(.12f, .56f, .48f, 1f));
            RuntimeUI.Place(unlock.GetComponent<RectTransform>(), 0, -120, 250, 58);

            var waiting = StatePanel("Waiting", refs.Content);
            RuntimeUI.Text("Message", waiting.transform, d.recoveryWaiting, 26,
                new Color(.76f, .86f, .82f), TextAnchor.MiddleCenter);

            var repair = StatePanel("Repair", refs.Content);
            var repairInfo = RuntimeUI.Text("Info", repair.transform, d.repairPrompt, 24,
                new Color(.78f, .86f, .84f), TextAnchor.UpperLeft);
            repairInfo.rectTransform.anchorMin = new Vector2(0, .53f);
            repairInfo.rectTransform.anchorMax = Vector2.one;
            repairInfo.rectTransform.offsetMin = new Vector2(24, 0);
            repairInfo.rectTransform.offsetMax = new Vector2(-24, -20);
            var fileName = RuntimeUI.Input("FileName", repair.transform, d.fileNamePlaceholder,
                d.fileNamePlaceholder);
            RuntimeUI.Place(fileName.GetComponent<RectTransform>(), 0, -28, 500, 64);
            var repairButton = RuntimeUI.Button("Repair", repair.transform, d.repairButton, null,
                new Color(.08f, .36f, .32f, 1f), new Color(.12f, .56f, .48f, 1f));
            RuntimeUI.Place(repairButton.GetComponent<RectTransform>(), 0, -112, 330, 60);

            var complete = StatePanel("Complete", refs.Content);
            RuntimeUI.Text("Message", complete.transform, d.repairComplete, 26,
                new Color(.55f, .9f, .76f), TextAnchor.MiddleCenter);

            var component = refs.Root.gameObject.AddComponent<RecoveryDesktopWindow>();
            component.Configure(locked, waiting, repair, complete, password, fileName);
            UnityEventTools.AddPersistentListener(refs.Window.OnOpened, component.Refresh);
            UnityEventTools.AddPersistentListener(unlock.onClick, component.SubmitPassword);
            UnityEventTools.AddPersistentListener(repairButton.onClick, component.SubmitRepairName);
            windows.Add(refs.Window);
            return refs.Window;
        }

        private static DesktopWindow BuildPhoto(RectTransform parent, Canvas canvas, ControlSContent content,
            List<DesktopWindow> windows)
        {
            var d = content.desktop;
            var refs = BuildWindow(parent, canvas, "Photo", d.photoTitle,
                new Vector2(830, 620), new Vector2(120, -10), 390f);
            var preview = RuntimeUI.Panel("Preview", refs.Content, new Color(.018f, .019f, .022f, 1f),
                new Vector2(.08f, .3f), new Vector2(.92f, .94f), Vector2.zero, Vector2.zero);
            var silhouette = RuntimeUI.Panel("Drawer silhouette", preview.transform,
                new Color(.025f, .027f, .03f, 1f), new Vector2(.5f, .18f), new Vector2(.86f, .76f),
                Vector2.zero, Vector2.zero);
            silhouette.raycastTarget = false;
            var clue = RuntimeUI.Text("Hidden clue", preview.transform, d.photoHiddenClue, 42,
                new Color(.12f, .02f, .02f, 0), TextAnchor.MiddleCenter);
            clue.fontStyle = FontStyle.Bold;
            var label = RuntimeUI.Text("Brightness Label", refs.Content,
                string.Format(d.brightnessFormat, 8), 21, new Color(.76f, .84f, .82f), TextAnchor.MiddleLeft);
            label.rectTransform.anchorMin = new Vector2(.08f, .2f);
            label.rectTransform.anchorMax = new Vector2(.92f, .28f);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            var slider = RuntimeUI.Slider("Brightness", refs.Content, .08f);
            slider.GetComponent<RectTransform>().anchorMin = new Vector2(.08f, .09f);
            slider.GetComponent<RectTransform>().anchorMax = new Vector2(.92f, .19f);
            slider.GetComponent<RectTransform>().offsetMin = slider.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var component = refs.Root.gameObject.AddComponent<PhotoDesktopWindow>();
            component.Configure(slider, preview, silhouette, clue, label);
            UnityEventTools.AddPersistentListener(refs.Window.OnOpened, component.Refresh);
            UnityEventTools.AddPersistentListener(slider.onValueChanged, component.OnBrightnessChanged);
            windows.Add(refs.Window);
            return refs.Window;
        }

        private static DesktopWindow BuildRecycleBin(RectTransform parent, Canvas canvas, ControlSContent content,
            List<DesktopWindow> windows)
        {
            var d = content.desktop;
            var refs = BuildWindow(parent, canvas, "Recycle Bin", d.recycleTitle,
                new Vector2(760, 520), new Vector2(-15, -15), 330f);
            var info = RuntimeUI.Text("Info", refs.Content, d.recycleUnknown, 23,
                new Color(.78f, .85f, .84f), TextAnchor.UpperLeft);
            info.rectTransform.anchorMin = new Vector2(0, .78f);
            info.rectTransform.anchorMax = Vector2.one;
            info.rectTransform.offsetMin = new Vector2(20, 0);
            info.rectTransform.offsetMax = new Vector2(-20, -10);
            var list = StatePanel("File List", refs.Content);
            var family = FileRow(list.transform, d.familyFile, d.familyDetail, .36f, d.restoreButton);
            var occupant = FileRow(list.transform, d.occupantFile, d.occupantDetail, -.08f, d.restoreButton);
            var empty = StatePanel("Empty", refs.Content);
            RuntimeUI.Text("Message", empty.transform, d.recycleDone, 27,
                new Color(.57f, .86f, .73f), TextAnchor.MiddleCenter);
            var component = refs.Root.gameObject.AddComponent<RecycleBinDesktopWindow>();
            component.Configure(info, list, empty);
            UnityEventTools.AddPersistentListener(refs.Window.OnOpened, component.Refresh);
            UnityEventTools.AddPersistentListener(family.onClick, component.RestoreFamily);
            UnityEventTools.AddPersistentListener(occupant.onClick, component.RestoreOccupant);
            windows.Add(refs.Window);
            return refs.Window;
        }

        private static DesktopWindow BuildArchive(RectTransform parent, Canvas canvas, ControlSContent content,
            List<DesktopWindow> windows)
        {
            var d = content.desktop;
            var refs = BuildWindow(parent, canvas, "Archive", d.archiveTitle,
                new Vector2(820, 560), new Vector2(65, 18), 420f);
            var locked = StatePanel("Locked", refs.Content);
            RuntimeUI.Text("Message", locked.transform, d.archiveLocked, 28,
                new Color(.76f, .45f, .4f), TextAnchor.MiddleCenter);
            var unknown = StatePanel("Unknown", refs.Content);
            RuntimeUI.Text("Message", unknown.transform, d.archiveUnknown, 27,
                new Color(.76f, .83f, .81f), TextAnchor.MiddleCenter);
            var solved = StatePanel("Solved", refs.Content);
            RuntimeUI.Text("Message", solved.transform, d.archiveSolved, 30,
                new Color(.52f, .91f, .73f), TextAnchor.MiddleCenter);
            var puzzle = StatePanel("Puzzle", refs.Content);
            var status = RuntimeUI.Text("Sequence", puzzle.transform,
                d.archiveSequencePrefix + d.archiveEmpty, 27,
                new Color(.74f, .86f, .82f), TextAnchor.MiddleCenter);
            status.rectTransform.anchorMin = new Vector2(0, .65f);
            status.rectTransform.anchorMax = Vector2.one;
            status.rectTransform.offsetMin = Vector2.zero;
            status.rectTransform.offsetMax = new Vector2(0, -10);
            var component = refs.Root.gameObject.AddComponent<ArchiveDesktopWindow>();
            component.Configure(locked, unknown, puzzle, solved, status);
            for (var i = 1; i <= 4; i++)
            {
                var button = RuntimeUI.Button("Fragment " + i, puzzle.transform,
                    $"[{i}]\nfragment_{(char)('A' + i - 1)}.log", null,
                    new Color(.075f, .18f, .18f, 1f), new Color(.13f, .41f, .37f, 1f), 20);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(165, 145);
                rect.anchoredPosition = new Vector2((i - 2.5f) * 180f, -42);
                UnityEventTools.AddIntPersistentListener(button.onClick, component.PushFragment, i);
            }
            UnityEventTools.AddPersistentListener(refs.Window.OnOpened, component.Refresh);
            windows.Add(refs.Window);
            return refs.Window;
        }

        private static DesktopWindow BuildRecovered(RectTransform parent, Canvas canvas, ControlSContent content,
            InteractionSequence ending, List<DesktopWindow> windows)
        {
            var d = content.desktop;
            var refs = BuildWindow(parent, canvas, "Recovered", d.recoveredTitle,
                new Vector2(850, 580), Vector2.zero, 180f);
            var warning = RuntimeUI.Text("Warning", refs.Content, d.recoveredWarning, 27,
                new Color(.9f, .72f, .67f), TextAnchor.UpperLeft);
            warning.rectTransform.offsetMin = new Vector2(28, 92);
            warning.rectTransform.offsetMax = new Vector2(-28, -20);
            var commit = RuntimeUI.Button("Commit", refs.Content, d.recoveredButton, null,
                new Color(.43f, .075f, .065f, 1f), new Color(.75f, .12f, .09f, 1f), 23);
            var rect = commit.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.sizeDelta = new Vector2(500, 66);
            rect.anchoredPosition = new Vector2(0, 8);
            var component = refs.Root.gameObject.AddComponent<RecoveredDesktopWindow>();
            component.Configure(refs.Window, ending);
            UnityEventTools.AddPersistentListener(commit.onClick, component.Commit);
            windows.Add(refs.Window);
            return refs.Window;
        }

        private static WindowReferences BuildWindow(RectTransform parent, Canvas canvas, string key, string title,
            Vector2 size, Vector2 position, float tone)
        {
            var root = RuntimeUI.Rect("Window - " + key, parent);
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
            root.pivot = new Vector2(.5f, .5f);
            root.sizeDelta = size;
            root.anchoredPosition = position;
            var background = root.gameObject.AddComponent<Image>();
            background.sprite = RuntimeUI.WhiteSprite;
            background.color = new Color(.035f, .042f, .05f, .995f);
            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.19f, .58f, .53f, .8f);
            outline.effectDistance = new Vector2(2, -2);
            var header = RuntimeUI.Panel("Title Bar", root, new Color(.075f, .23f, .225f, 1f),
                new Vector2(0, 1), Vector2.one, new Vector2(0, -48), Vector2.zero);
            var titleText = RuntimeUI.Text("Title", header.transform, "▣  " + title, 22, Color.white);
            titleText.rectTransform.offsetMin = new Vector2(14, 0);
            titleText.rectTransform.offsetMax = new Vector2(-60, 0);
            var close = RuntimeUI.Button("Close", header.transform, "×", null,
                new Color(.35f, .1f, .1f, 1f), new Color(.72f, .17f, .15f, 1f), 28);
            close.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
            close.GetComponent<RectTransform>().anchorMax = Vector2.one;
            close.GetComponent<RectTransform>().offsetMin = new Vector2(-52, 0);
            close.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var drag = header.gameObject.AddComponent<WindowDragHandle>();
            drag.Configure(root, canvas);
            var content = RuntimeUI.Rect("Content", root);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(18, 18);
            content.offsetMax = new Vector2(-18, -62);
            var window = root.gameObject.AddComponent<DesktopWindow>();
            window.Configure(root, close, tone);
            UnityEventTools.AddPersistentListener(close.onClick, window.Close);
            root.gameObject.SetActive(false);
            EditorUtility.SetDirty(window);
            return new WindowReferences(root, content, window);
        }

        private static GameObject StatePanel(string name, Transform parent)
        {
            var panel = RuntimeUI.Rect(name, parent);
            RuntimeUI.Stretch(panel);
            return panel.gameObject;
        }

        private static Button FileRow(Transform parent, string name, string detail, float yNormalized,
            string buttonLabel)
        {
            var row = RuntimeUI.Panel("Row " + name, parent, new Color(.07f, .085f, .095f, 1f),
                new Vector2(.03f, .5f), new Vector2(.97f, .5f), new Vector2(0, -30), new Vector2(0, 78));
            row.rectTransform.anchoredPosition = new Vector2(0, yNormalized * 250f);
            var text = RuntimeUI.Text("File", row.transform, $"[FILE]  {name}\n           {detail}", 22,
                new Color(.8f, .87f, .85f), TextAnchor.MiddleLeft);
            text.rectTransform.offsetMin = new Vector2(16, 4);
            text.rectTransform.offsetMax = new Vector2(-180, -4);
            var button = RuntimeUI.Button("Restore", row.transform, buttonLabel, null,
                new Color(.09f, .29f, .27f, 1f), new Color(.13f, .49f, .43f, 1f), 20);
            button.GetComponent<RectTransform>().anchorMin = new Vector2(1, .18f);
            button.GetComponent<RectTransform>().anchorMax = new Vector2(1, .82f);
            button.GetComponent<RectTransform>().offsetMin = new Vector2(-158, 0);
            button.GetComponent<RectTransform>().offsetMax = new Vector2(-16, 0);
            return button;
        }

        private sealed class WindowReferences
        {
            public readonly RectTransform Root;
            public readonly RectTransform Content;
            public readonly DesktopWindow Window;

            public WindowReferences(RectTransform root, RectTransform content, DesktopWindow window)
            {
                Root = root;
                Content = content;
                Window = window;
            }
        }
    }
}
#endif
