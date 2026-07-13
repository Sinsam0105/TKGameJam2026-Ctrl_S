using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ControlS
{
    internal sealed class WindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform target;
        private Canvas canvas;

        public void Initialize(RectTransform window, Canvas owner)
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

    internal sealed class VirtualDesktop : MonoBehaviour
    {
        private ControlSBootstrap game;
        private ControlSState state;
        private Canvas canvas;
        private RectTransform desktopRoot;
        private RectTransform iconLayer;
        private RectTransform windowLayer;
        private Text taskbarClock;
        private readonly Dictionary<string, RectTransform> openWindows = new Dictionary<string, RectTransform>();

        public bool IsOpen => desktopRoot != null && desktopRoot.gameObject.activeSelf;

        public void Initialize(ControlSBootstrap owner, ControlSState gameState)
        {
            game = owner;
            state = gameState;
            BuildCanvas();
            state.Changed += RefreshIcons;
            RefreshIcons();
            desktopRoot.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (desktopRoot == null || state.EndingStarted) return;
            desktopRoot.gameObject.SetActive(true);
            desktopRoot.SetAsLastSibling();
            game.SetPrompt(string.Empty);
            game.PlayUiTone(520f, .07f);
        }

        public void Close()
        {
            if (desktopRoot == null) return;
            desktopRoot.gameObject.SetActive(false);
            game.PlayUiTone(300f, .05f);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (taskbarClock != null) taskbarClock.text = DateTime.Now.ToString("HH:mm") + "  ▣";
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("Virtual Desktop Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            desktopRoot = RuntimeUI.Rect("Desktop", canvas.transform);
            RuntimeUI.Stretch(desktopRoot);
            var wallpaper = desktopRoot.gameObject.AddComponent<Image>();
            wallpaper.sprite = RuntimeUI.WhiteSprite;
            wallpaper.color = new Color(.025f, .095f, .105f, 1f);

            // Sparse wallpaper geometry evokes a room blueprint without external artwork.
            RuntimeUI.Panel("Wallpaper line A", desktopRoot, new Color(.11f, .37f, .36f, .35f),
                new Vector2(.38f, .08f), new Vector2(.39f, .93f), Vector2.zero, Vector2.zero).raycastTarget = false;
            RuntimeUI.Panel("Wallpaper line B", desktopRoot, new Color(.11f, .37f, .36f, .25f),
                new Vector2(.38f, .57f), new Vector2(.92f, .58f), Vector2.zero, Vector2.zero).raycastTarget = false;
            var watermark = RuntimeUI.Text("Watermark", desktopRoot, "CTRL_S RECOVERY ENVIRONMENT\nROOM INSTANCE 02:17",
                34, new Color(.18f, .48f, .45f, .2f), TextAnchor.LowerRight);
            watermark.rectTransform.offsetMin = new Vector2(0, 64);
            watermark.rectTransform.offsetMax = new Vector2(-46, -32);

            iconLayer = RuntimeUI.Rect("Desktop Icons", desktopRoot);
            RuntimeUI.Stretch(iconLayer);
            iconLayer.offsetMax = new Vector2(0, -20);
            windowLayer = RuntimeUI.Rect("Windows", desktopRoot);
            RuntimeUI.Stretch(windowLayer);

            var taskbar = RuntimeUI.Panel("Taskbar", desktopRoot, new Color(.025f, .03f, .038f, .98f),
                new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 58));
            var home = RuntimeUI.Button("Leave Computer", taskbar.transform, "▣  방으로 돌아가기", Close,
                new Color(.08f, .11f, .12f, 1f), new Color(.12f, .28f, .26f, 1f), 19);
            var homeRt = home.GetComponent<RectTransform>();
            homeRt.anchorMin = new Vector2(0, 0);
            homeRt.anchorMax = new Vector2(0, 1);
            homeRt.offsetMin = Vector2.zero;
            homeRt.offsetMax = new Vector2(240, 0);

            taskbarClock = RuntimeUI.Text("Clock", taskbar.transform, "02:17  ▣", 19,
                new Color(.82f, .88f, .87f), TextAnchor.MiddleCenter);
            taskbarClock.rectTransform.anchorMin = new Vector2(1, 0);
            taskbarClock.rectTransform.anchorMax = new Vector2(1, 1);
            taskbarClock.rectTransform.offsetMin = new Vector2(-155, 0);
            taskbarClock.rectTransform.offsetMax = Vector2.zero;
        }

        private void RefreshIcons()
        {
            if (iconLayer == null) return;
            for (var i = iconLayer.childCount - 1; i >= 0; i--) Destroy(iconLayer.GetChild(i).gameObject);

            var row = 0;
            AddIcon("README.txt", "TXT", OpenReadme, row++);
            AddIcon("RECOVERY", "DIR", OpenRecovery, row++);
            AddIcon("dark_photo.img", "IMG", OpenDarkPhoto, row++);
            AddIcon("Recycle Bin", "BIN", OpenRecycleBin, row++);
            AddIcon("ARCHIVE", state.SaveFileRepaired ? "DIR" : "LOCK", OpenArchive, row++);
            if (state.ArchiveSequenceSolved)
                AddIcon("RECOVERED.save", "SAVE", OpenRecovered, row++);
        }

        private void AddIcon(string label, string type, UnityEngine.Events.UnityAction action, int row)
        {
            var button = RuntimeUI.Button("Icon " + label, iconLayer, $"[{type}]\n{label}", action,
                new Color(.03f, .08f, .09f, .22f), new Color(.13f, .42f, .39f, .7f), 19);
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(34, -34 - row * 128);
            rt.sizeDelta = new Vector2(172, 108);
        }

        private RectTransform CreateWindow(string key, string title, Vector2 size, Vector2? position = null)
        {
            if (openWindows.TryGetValue(key, out var existing) && existing != null) Destroy(existing.gameObject);
            openWindows.Remove(key);

            var root = RuntimeUI.Rect("Window - " + key, windowLayer);
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
            root.pivot = new Vector2(.5f, .5f);
            root.sizeDelta = size;
            root.anchoredPosition = position ?? new Vector2(UnityEngine.Random.Range(-80, 110), UnityEngine.Random.Range(-65, 80));
            var shadow = root.gameObject.AddComponent<Image>();
            shadow.sprite = RuntimeUI.WhiteSprite;
            shadow.color = new Color(.035f, .042f, .05f, .995f);

            var border = root.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(.19f, .58f, .53f, .8f);
            border.effectDistance = new Vector2(2, -2);

            var header = RuntimeUI.Panel("Title Bar", root, new Color(.075f, .23f, .225f, 1f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -48), Vector2.zero);
            var titleText = RuntimeUI.Text("Title", header.transform, "▣  " + title, 22, Color.white);
            titleText.rectTransform.offsetMin = new Vector2(14, 0);
            titleText.rectTransform.offsetMax = new Vector2(-60, 0);
            var close = RuntimeUI.Button("Close", header.transform, "×", () =>
            {
                openWindows.Remove(key);
                Destroy(root.gameObject);
            }, new Color(.35f, .1f, .1f, 1f), new Color(.72f, .17f, .15f, 1f), 28);
            close.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
            close.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            close.GetComponent<RectTransform>().offsetMin = new Vector2(-52, 0);
            close.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var drag = header.gameObject.AddComponent<WindowDragHandle>();
            drag.Initialize(root, canvas);

            var content = RuntimeUI.Rect("Content", root);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(18, 18);
            content.offsetMax = new Vector2(-18, -62);
            openWindows[key] = root;
            root.SetAsLastSibling();
            return content;
        }

        private void OpenReadme()
        {
            game.PlayUiTone(600f, .04f);
            var content = CreateWindow("readme", "README.txt — Notepad", new Vector2(720, 530), new Vector2(80, 70));
            var text = RuntimeUI.Text("Document", content,
                "CTRL_S 자동 복구 안내\n\n" +
                "저장 실패 감지: 02:17:00\n복구 대상: occupant_session.save\n임시 조각: 4\n\n" +
                "1. 방 안의 마지막 정상 상태를 확인하십시오.\n" +
                "2. RECOVERY 폴더의 생성 시각 잠금을 해제하십시오.\n" +
                "3. 손상된 파일명은 물리 입력 장치와 대조하십시오.\n\n" +
                "※ 복구 도중 방에 없던 물건이 보이더라도 만지지 마십시오.\n" +
                "※ 화면 속 방과 현재 방이 다를 경우, 현재 방이 잘못된 것입니다.",
                25, new Color(.8f, .86f, .84f), TextAnchor.UpperLeft);
            text.rectTransform.offsetMin = new Vector2(14, 10);
            text.rectTransform.offsetMax = new Vector2(-14, -10);
        }

        private void OpenRecovery()
        {
            game.PlayUiTone(460f, .05f);
            var content = CreateWindow("recovery", "C:\\RECOVERY", new Vector2(780, 500), new Vector2(35, 30));
            if (!state.TimePasswordSolved)
            {
                var info = RuntimeUI.Text("Info", content,
                    "보호된 폴더\n생성 시각(HHMM)을 입력하십시오.\n힌트: 현실 공간의 정지된 시간",
                    24, new Color(.78f, .86f, .84f), TextAnchor.UpperLeft);
                info.rectTransform.anchorMin = new Vector2(0, .48f);
                info.rectTransform.anchorMax = Vector2.one;
                info.rectTransform.offsetMin = new Vector2(24, 0);
                info.rectTransform.offsetMax = new Vector2(-24, -20);

                var input = RuntimeUI.Input("Password", content, "HHMM");
                Place(input.GetComponent<RectTransform>(), 0, -38, 430, 62);
                input.characterLimit = 4;
                input.contentType = InputField.ContentType.IntegerNumber;
                var unlock = RuntimeUI.Button("Unlock", content, "잠금 해제", () =>
                {
                    if (state.TryTimePassword(input.text)) OpenRecovery();
                }, new Color(.08f, .36f, .32f, 1f), new Color(.12f, .56f, .48f, 1f));
                Place(unlock.GetComponent<RectTransform>(), 0, -120, 250, 58);
                input.Select();
                input.ActivateInputField();
                return;
            }

            if (!state.KeycapCollected)
            {
                var waiting = RuntimeUI.Text("Waiting", content,
                    "SAVE_?.tmp\n\nSTATUS: 파일명 손상\n누락된 문자: 물리 키보드와 대조 필요\n\n시계 아래에서 입력 장치 조각이 감지되었습니다.",
                    26, new Color(.76f, .86f, .82f), TextAnchor.UpperLeft);
                waiting.rectTransform.offsetMin = new Vector2(26, 20);
                waiting.rectTransform.offsetMax = new Vector2(-26, -20);
                return;
            }

            if (!state.SaveFileRepaired)
            {
                var info = RuntimeUI.Text("Repair Info", content,
                    "손상된 파일명을 완성하십시오.\n확장자를 포함한 전체 이름이 필요합니다.",
                    24, new Color(.78f, .86f, .84f), TextAnchor.UpperLeft);
                info.rectTransform.anchorMin = new Vector2(0, .53f);
                info.rectTransform.anchorMax = Vector2.one;
                info.rectTransform.offsetMin = new Vector2(24, 0);
                info.rectTransform.offsetMax = new Vector2(-24, -20);
                var fileName = RuntimeUI.Input("FileName", content, "SAVE_?.tmp", "SAVE_?.tmp");
                Place(fileName.GetComponent<RectTransform>(), 0, -28, 500, 64);
                var repair = RuntimeUI.Button("Repair", content, "파일명 변경 및 복구", () =>
                {
                    if (state.TryRepairSaveName(fileName.text)) OpenRecovery();
                }, new Color(.08f, .36f, .32f, 1f), new Color(.12f, .56f, .48f, 1f));
                Place(repair.GetComponent<RectTransform>(), 0, -112, 330, 60);
                return;
            }

            var complete = RuntimeUI.Text("Complete", content,
                "✓ SAVE_S.tmp\n\n복구 상태: 정상\n수정 시각: 02:17\n내용: 다음 조각의 위치는 이미지 캐시에 있음\n\nC:\\CACHE\\dark_photo.img",
                26, new Color(.55f, .9f, .76f), TextAnchor.UpperLeft);
            complete.rectTransform.offsetMin = new Vector2(26, 20);
            complete.rectTransform.offsetMax = new Vector2(-26, -20);
        }

        private void OpenDarkPhoto()
        {
            game.PlayUiTone(390f, .05f);
            var content = CreateWindow("photo", "dark_photo.img — Image Inspector", new Vector2(830, 620), new Vector2(120, -10));
            var preview = RuntimeUI.Panel("Preview", content, new Color(.018f, .019f, .022f, 1f),
                new Vector2(.08f, .3f), new Vector2(.92f, .94f), Vector2.zero, Vector2.zero);
            var silhouette = RuntimeUI.Panel("Drawer silhouette", preview.transform, new Color(.025f, .027f, .03f, 1f),
                new Vector2(.5f, .18f), new Vector2(.86f, .76f), Vector2.zero, Vector2.zero);
            silhouette.raycastTarget = false;
            var clue = RuntimeUI.Text("Hidden clue", preview.transform, "DRAWER\n4 3 1 2", 42,
                new Color(.12f, .02f, .02f, 0), TextAnchor.MiddleCenter);
            clue.fontStyle = FontStyle.Bold;

            var label = RuntimeUI.Text("Brightness Label", content, "BRIGHTNESS  08%", 21,
                new Color(.76f, .84f, .82f), TextAnchor.MiddleLeft);
            label.rectTransform.anchorMin = new Vector2(.08f, .2f);
            label.rectTransform.anchorMax = new Vector2(.92f, .28f);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            var slider = RuntimeUI.Slider("Brightness", content, state.PhotoClueRevealed ? .88f : .08f);
            slider.GetComponent<RectTransform>().anchorMin = new Vector2(.08f, .09f);
            slider.GetComponent<RectTransform>().anchorMax = new Vector2(.92f, .19f);
            slider.GetComponent<RectTransform>().offsetMin = slider.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            slider.onValueChanged.AddListener(value =>
            {
                var light = Mathf.Lerp(.018f, .52f, value);
                preview.color = new Color(light * .55f, light * .58f, light * .63f, 1f);
                silhouette.color = new Color(light * .34f, light * .29f, light * .27f, 1f);
                clue.color = new Color(.48f, .055f, .045f, Mathf.InverseLerp(.68f, .86f, value));
                label.text = $"BRIGHTNESS  {Mathf.RoundToInt(value * 100):00}%";
                if (value >= .82f) state.RevealPhotoClue();
            });
            slider.onValueChanged.Invoke(slider.value);
        }

        private void OpenRecycleBin()
        {
            game.PlayUiTone(330f, .05f);
            var content = CreateWindow("trash", "Recycle Bin", new Vector2(760, 520), new Vector2(-15, -15));
            var info = RuntimeUI.Text("Info", content,
                state.DrawerOpened ? "삭제된 항목 2개 — 서랍의 메모와 대조하십시오." : "삭제된 항목 2개 — 복원 대상 불명",
                23, new Color(.78f, .85f, .84f), TextAnchor.UpperLeft);
            info.rectTransform.anchorMin = new Vector2(0, .78f);
            info.rectTransform.anchorMax = Vector2.one;
            info.rectTransform.offsetMin = new Vector2(20, 0);
            info.rectTransform.offsetMax = new Vector2(-20, -10);

            if (state.FamilyPhotoRestored)
            {
                var done = RuntimeUI.Text("Empty", content,
                    "FAMILY.PNG가 원래 위치로 복원되었습니다.\n\n남은 항목:\nDO_NOT_RESTORE.exe  0 KB",
                    27, new Color(.57f, .86f, .73f), TextAnchor.UpperLeft);
                done.rectTransform.offsetMin = new Vector2(24, 26);
                done.rectTransform.offsetMax = new Vector2(-24, -85);
                return;
            }

            AddFileRow(content, "FAMILY.PNG", "1.8 MB  |  삭제: 02:16", .36f, () =>
            {
                if (state.TryRestoreFile(true)) OpenRecycleBin();
            });
            AddFileRow(content, "DO_NOT_RESTORE.exe", "0 KB  |  삭제: 02:17", -.08f, () => state.TryRestoreFile(false));
        }

        private void AddFileRow(RectTransform content, string name, string detail, float yNormalized,
            UnityEngine.Events.UnityAction action)
        {
            var row = RuntimeUI.Panel("Row " + name, content, new Color(.07f, .085f, .095f, 1f),
                new Vector2(.03f, .5f), new Vector2(.97f, .5f), new Vector2(0, -30), new Vector2(0, 78));
            row.rectTransform.anchoredPosition = new Vector2(0, yNormalized * 250f);
            var text = RuntimeUI.Text("File", row.transform, $"[FILE]  {name}\n           {detail}", 22,
                new Color(.8f, .87f, .85f), TextAnchor.MiddleLeft);
            text.rectTransform.offsetMin = new Vector2(16, 4);
            text.rectTransform.offsetMax = new Vector2(-180, -4);
            var restore = RuntimeUI.Button("Restore", row.transform, "복원", action,
                new Color(.09f, .29f, .27f, 1f), new Color(.13f, .49f, .43f, 1f), 20);
            restore.GetComponent<RectTransform>().anchorMin = new Vector2(1, .18f);
            restore.GetComponent<RectTransform>().anchorMax = new Vector2(1, .82f);
            restore.GetComponent<RectTransform>().offsetMin = new Vector2(-158, 0);
            restore.GetComponent<RectTransform>().offsetMax = new Vector2(-16, 0);
        }

        private void OpenArchive()
        {
            game.PlayUiTone(420f, .05f);
            var content = CreateWindow("archive", "C:\\ARCHIVE", new Vector2(820, 560), new Vector2(65, 18));
            if (!state.SaveFileRepaired)
            {
                var locked = RuntimeUI.Text("Locked", content,
                    "🔒 ARCHIVE\n\n의존 파일 SAVE_?.tmp가 손상되어 폴더를 열 수 없습니다.", 28,
                    new Color(.76f, .45f, .4f), TextAnchor.MiddleCenter);
                return;
            }
            if (!state.PhotoSequenceDiscovered)
            {
                var unknown = RuntimeUI.Text("Unknown", content,
                    "손상 로그 조각 4개가 발견되었습니다.\n실행 순서를 알 수 없습니다.\n\n복원된 현실 오브젝트에 인덱스가 기록되어 있습니다.", 27,
                    new Color(.76f, .83f, .81f), TextAnchor.MiddleCenter);
                return;
            }
            if (state.ArchiveSequenceSolved)
            {
                var solved = RuntimeUI.Text("Solved", content,
                    "✓ 조각 결합 완료\n\nRECOVERED.save가 바탕화면에 생성되었습니다.", 30,
                    new Color(.52f, .91f, .73f), TextAnchor.MiddleCenter);
                return;
            }

            var sequence = new List<int>();
            var status = RuntimeUI.Text("Sequence", content, "실행 순서: _  _  _  _", 27,
                new Color(.74f, .86f, .82f), TextAnchor.MiddleCenter);
            status.rectTransform.anchorMin = new Vector2(0, .65f);
            status.rectTransform.anchorMax = Vector2.one;
            status.rectTransform.offsetMin = new Vector2(0, 0);
            status.rectTransform.offsetMax = new Vector2(0, -10);

            for (var i = 1; i <= 4; i++)
            {
                var index = i;
                var button = RuntimeUI.Button("Fragment " + index, content,
                    $"[{index}]\nfragment_{(char)('A' + i - 1)}.log", () =>
                    {
                        if (sequence.Count >= 4) sequence.Clear();
                        sequence.Add(index);
                        status.text = "실행 순서: " + string.Join("  →  ", sequence);
                        game.PlayUiTone(330f + index * 65f, .07f);
                        if (sequence.Count != 4) return;
                        if (state.TryArchiveSequence(sequence.ToArray()))
                        {
                            status.text = "결합 완료 — RECOVERED.save 생성";
                            RefreshIcons();
                        }
                        else
                        {
                            sequence.Clear();
                            status.text = "순서 오류. 다시 시도: _  _  _  _";
                        }
                    }, new Color(.075f, .18f, .18f, 1f), new Color(.13f, .41f, .37f, 1f), 20);
                var rt = button.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
                rt.sizeDelta = new Vector2(165, 145);
                rt.anchoredPosition = new Vector2((i - 2.5f) * 180f, -42);
            }
        }

        private void OpenRecovered()
        {
            game.PlayUiTone(180f, .15f);
            var content = CreateWindow("recovered", "RECOVERED.save — 0 KB", new Vector2(850, 580), Vector2.zero);
            var warning = RuntimeUI.Text("Warning", content,
                "RECOVERY RESULT\n\n" +
                "대상 세션: OCCUPANT_00\n" +
                "플레이어 프로세스: AUTOSAVE_AGENT\n" +
                "현실 일치율: 0%\n\n" +
                "경고: 복구된 것은 파일이 아닙니다.\n" +
                "경고: 방 안의 '사람'은 저장 프로세스가 만든 커서입니다.\n\n" +
                "마지막 블록을 기록하면 OCCUPANT_00이 현재 방을 덮어씁니다.",
                27, new Color(.9f, .72f, .67f), TextAnchor.UpperLeft);
            warning.rectTransform.offsetMin = new Vector2(28, 92);
            warning.rectTransform.offsetMax = new Vector2(-28, -20);
            var open = RuntimeUI.Button("Commit", content, "그래도 마지막 블록 저장 (CTRL+S)", () =>
            {
                Close();
                game.BeginEnding();
            }, new Color(.43f, .075f, .065f, 1f), new Color(.75f, .12f, .09f, 1f), 23);
            open.GetComponent<RectTransform>().anchorMin = new Vector2(.5f, 0);
            open.GetComponent<RectTransform>().anchorMax = new Vector2(.5f, 0);
            open.GetComponent<RectTransform>().pivot = new Vector2(.5f, 0);
            open.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 66);
            open.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 8);
        }

        private static void Place(RectTransform rt, float x, float y, float width, float height)
        {
            RuntimeUI.Place(rt, x, y, width, height);
        }
    }
}
