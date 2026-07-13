using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ControlS
{
    /// <summary>
    /// Turns the empty Unity template scene into a complete playable vertical slice.
    /// Keeping the prototype runtime-generated lets art replace each prop later without
    /// coupling the game logic to temporary scene assets.
    /// </summary>
    public sealed class ControlSBootstrap : MonoBehaviour
    {
        private ControlSState state;
        private RoomWorld room;
        private VirtualDesktop desktop;
        private Canvas hudCanvas;
        private Text objectiveText;
        private Text promptText;
        private Text messageText;
        private Image messagePanel;
        private RectTransform modalLayer;
        private Image glitchOverlay;
        private RectTransform glitchStripes;
        private Coroutine messageRoutine;
        private Coroutine glitchRoutine;
        private bool modalOpen;
        private bool ending;
        private Camera worldCamera;
        private AudioSource droneSource;
        private AudioSource uiSource;

        public bool BlocksRoomInput => modalOpen || ending || (desktop != null && desktop.IsOpen);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreatePrototype()
        {
            if (FindFirstObjectByType<ControlSBootstrap>() != null) return;
            var root = new GameObject("CONTROL S - Game Runtime");
            root.AddComponent<ControlSBootstrap>();
        }

        private void Awake()
        {
            state = new ControlSState();
            state.MessageRequested += ShowMessage;
            state.GlitchRequested += TriggerGlitch;
            state.Changed += UpdateObjective;

            SetupEventSystem();
            SetupCamera();
            SetupHud();
            SetupAudio();

            desktop = gameObject.AddComponent<VirtualDesktop>();
            desktop.Initialize(this, state);
            room = new RoomWorld(transform, this, state);
            UpdateObjective();
            StartCoroutine(IntroRoutine());
        }

        private void Update()
        {
            room?.Tick(Time.unscaledTime);
            if (worldCamera != null)
            {
                var progress = state.CompletedPuzzleCount / 4f;
                var pulse = Mathf.Sin(Time.unscaledTime * (1.25f + progress)) * .004f;
                worldCamera.backgroundColor = new Color(.012f + pulse, .016f, .021f + progress * .006f, 1f);
            }
        }

        public void OpenDesktop()
        {
            if (ending) return;
            desktop.Open();
        }

        public void SetPrompt(string value)
        {
            if (promptText != null) promptText.text = value;
        }

        public void OpenDrawerKeypad()
        {
            if (ending || modalOpen) return;
            if (state.DrawerOpened)
            {
                state.RequestMessage("열린 서랍 안에는 메모 자국만 남아 있다.\nFAMILY.PNG만 복원할 것.", 3.5f);
                return;
            }

            modalOpen = true;
            modalLayer.gameObject.SetActive(true);
            ClearChildren(modalLayer);
            RuntimeUI.Panel("Dim", modalLayer, new Color(0, 0, 0, .72f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            var panel = RuntimeUI.Panel("Keypad", modalLayer, new Color(.045f, .052f, .058f, 1f),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-330, -220), new Vector2(330, 220));
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.4f, .25f, .17f, 1f);
            outline.effectDistance = new Vector2(3, -3);
            var title = RuntimeUI.Text("Title", panel.transform, "DRAWER LOCK / 4 DIGITS", 28,
                new Color(.88f, .82f, .72f), TextAnchor.UpperCenter);
            title.rectTransform.offsetMin = new Vector2(20, 300);
            title.rectTransform.offsetMax = new Vector2(-20, -24);
            var hint = RuntimeUI.Text("Hint", panel.transform,
                state.PhotoClueRevealed ? "사진에서 드러난 네 자리 숫자" : "단서가 필요하다.", 22,
                new Color(.62f, .65f, .63f), TextAnchor.MiddleCenter);
            hint.rectTransform.offsetMin = new Vector2(20, 175);
            hint.rectTransform.offsetMax = new Vector2(-20, -105);

            var input = RuntimeUI.Input("Code", panel.transform, "0000");
            RuntimeUI.Place(input.GetComponent<RectTransform>(), 0, 8, 360, 68);
            input.characterLimit = 4;
            input.contentType = InputField.ContentType.IntegerNumber;
            var submit = RuntimeUI.Button("Submit", panel.transform, "서랍 열기", () =>
            {
                if (!state.TryDrawerCode(input.text)) return;
                CloseModal();
                PlayUiTone(190f, .12f);
            }, new Color(.31f, .2f, .12f, 1f), new Color(.55f, .34f, .17f, 1f), 23);
            RuntimeUI.Place(submit.GetComponent<RectTransform>(), -105, -105, 250, 60);
            var cancel = RuntimeUI.Button("Cancel", panel.transform, "닫기", CloseModal,
                new Color(.16f, .17f, .18f, 1f), new Color(.28f, .3f, .31f, 1f), 22);
            RuntimeUI.Place(cancel.GetComponent<RectTransform>(), 190, -105, 170, 60);
            input.Select();
            input.ActivateInputField();
        }

        public void BeginEnding()
        {
            if (ending) return;
            ending = true;
            state.StartEnding();
            desktop.Close();
            CloseModal();
            if (room?.Player != null) room.Player.InputEnabled = false;
            StartCoroutine(EndingRoutine());
        }

        public void PlayUiTone(float frequency, float duration)
        {
            if (uiSource == null || !Application.isPlaying) return;
            var sampleRate = 22050;
            var count = Mathf.Max(64, Mathf.RoundToInt(sampleRate * duration));
            var clip = AudioClip.Create("ui_" + Mathf.RoundToInt(frequency), count, 1, sampleRate, false);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var envelope = 1f - i / (float)count;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * .13f * envelope;
            }
            clip.SetData(data, 0);
            uiSource.PlayOneShot(clip);
            Destroy(clip, duration + .2f);
        }

        private void SetupEventSystem()
        {
            if (EventSystem.current != null) return;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            var module = eventSystem.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private void SetupCamera()
        {
            worldCamera = Camera.main;
            if (worldCamera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                worldCamera = cameraObject.GetComponent<Camera>();
            }
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 5.1f;
            worldCamera.transform.position = new Vector3(0, 0, -10);
            worldCamera.transform.rotation = Quaternion.identity;
            worldCamera.backgroundColor = new Color(.012f, .016f, .021f, 1f);
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void SetupHud()
        {
            var canvasObject = new GameObject("Room HUD Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            hudCanvas = canvasObject.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 20;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            // Vignette-like flat edge strips keep the generated room moody.
            var topShade = RuntimeUI.Panel("Top shade", hudCanvas.transform, new Color(0, 0, 0, .42f),
                new Vector2(0, .88f), Vector2.one, Vector2.zero, Vector2.zero);
            topShade.raycastTarget = false;
            var bottomShade = RuntimeUI.Panel("Bottom shade", hudCanvas.transform, new Color(0, 0, 0, .5f),
                Vector2.zero, new Vector2(1, .13f), Vector2.zero, Vector2.zero);
            bottomShade.raycastTarget = false;
            var leftShade = RuntimeUI.Panel("Left shade", hudCanvas.transform, new Color(0, 0, 0, .25f),
                Vector2.zero, new Vector2(.055f, 1), Vector2.zero, Vector2.zero);
            leftShade.raycastTarget = false;
            var rightShade = RuntimeUI.Panel("Right shade", hudCanvas.transform, new Color(0, 0, 0, .25f),
                new Vector2(.945f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            rightShade.raycastTarget = false;

            var objectiveBar = RuntimeUI.Panel("Objective", hudCanvas.transform, new Color(.02f, .026f, .032f, .9f),
                new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-660, -90), new Vector2(660, -20));
            objectiveText = RuntimeUI.Text("Text", objectiveBar.transform, string.Empty, 24,
                new Color(.72f, .86f, .82f), TextAnchor.MiddleCenter);

            var controls = RuntimeUI.Text("Controls", hudCanvas.transform,
                "WASD / 방향키  이동    E / Enter  조사    ESC  컴퓨터 닫기", 18,
                new Color(.61f, .67f, .66f), TextAnchor.UpperLeft);
            controls.rectTransform.anchorMin = new Vector2(0, 1);
            controls.rectTransform.anchorMax = new Vector2(0, 1);
            controls.rectTransform.pivot = new Vector2(0, 1);
            controls.rectTransform.anchoredPosition = new Vector2(28, -22);
            controls.rectTransform.sizeDelta = new Vector2(700, 40);

            promptText = RuntimeUI.Text("Interaction Prompt", hudCanvas.transform, string.Empty, 28,
                new Color(.91f, .87f, .67f), TextAnchor.MiddleCenter);
            promptText.fontStyle = FontStyle.Bold;
            promptText.rectTransform.anchorMin = new Vector2(.5f, 0);
            promptText.rectTransform.anchorMax = new Vector2(.5f, 0);
            promptText.rectTransform.pivot = new Vector2(.5f, 0);
            promptText.rectTransform.anchoredPosition = new Vector2(0, 38);
            promptText.rectTransform.sizeDelta = new Vector2(900, 52);

            messagePanel = RuntimeUI.Panel("Narration", hudCanvas.transform, new Color(.018f, .021f, .025f, .94f),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-650, -365), new Vector2(650, -190));
            messageText = RuntimeUI.Text("Narration Text", messagePanel.transform, string.Empty, 27,
                new Color(.88f, .9f, .86f), TextAnchor.MiddleCenter);
            messageText.rectTransform.offsetMin = new Vector2(28, 14);
            messageText.rectTransform.offsetMax = new Vector2(-28, -14);
            messagePanel.gameObject.SetActive(false);

            modalLayer = RuntimeUI.Rect("Modal Layer", hudCanvas.transform);
            RuntimeUI.Stretch(modalLayer);
            modalLayer.gameObject.SetActive(false);

            glitchOverlay = RuntimeUI.Panel("Glitch Overlay", hudCanvas.transform, new Color(.32f, 0, 0, 0),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            glitchOverlay.raycastTarget = false;
            glitchStripes = RuntimeUI.Rect("Glitch Stripes", glitchOverlay.transform);
            RuntimeUI.Stretch(glitchStripes);
            for (var i = 0; i < 13; i++)
            {
                var stripe = RuntimeUI.Panel("Noise " + i, glitchStripes,
                    i % 3 == 0 ? new Color(.6f, .03f, .025f, .5f) : new Color(.72f, .78f, .75f, .16f),
                    new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 8));
                stripe.raycastTarget = false;
                stripe.gameObject.SetActive(false);
            }
        }

        private void SetupAudio()
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.volume = .8f;

            droneSource = gameObject.AddComponent<AudioSource>();
            droneSource.loop = true;
            droneSource.playOnAwake = false;
            droneSource.volume = .08f;
            var sampleRate = 22050;
            var seconds = 6;
            var count = sampleRate * seconds;
            var clip = AudioClip.Create("Room electrical hum", count, 1, sampleRate, false);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var time = i / (float)sampleRate;
                var wobble = 1f + Mathf.Sin(time * .73f) * .025f;
                data[i] = (Mathf.Sin(2f * Mathf.PI * 46f * wobble * time) * .38f +
                           Mathf.Sin(2f * Mathf.PI * 92f * time) * .1f) * .22f;
            }
            clip.SetData(data, 0);
            droneSource.clip = clip;
            droneSource.Play();
        }

        private IEnumerator IntroRoutine()
        {
            yield return new WaitForSecondsRealtime(.35f);
            ShowMessage("CTRL+S\n\n저장하지 못한 파일을 찾기 위해, 마지막으로 작업하던 방에 돌아왔다.", 5.5f);
        }

        private void UpdateObjective()
        {
            if (objectiveText == null) return;
            objectiveText.text = $"복구 진행  {state.CompletedPuzzleCount}/4     |     {state.Objective}";
        }

        private void ShowMessage(string value, float duration)
        {
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(value, duration));
        }

        private IEnumerator MessageRoutine(string value, float duration)
        {
            messagePanel.gameObject.SetActive(true);
            messageText.text = value;
            yield return new WaitForSecondsRealtime(duration);
            messagePanel.gameObject.SetActive(false);
            messageRoutine = null;
        }

        private void TriggerGlitch(float strength)
        {
            if (glitchRoutine != null) StopCoroutine(glitchRoutine);
            glitchRoutine = StartCoroutine(GlitchRoutine(strength));
        }

        private IEnumerator GlitchRoutine(float strength)
        {
            var duration = Mathf.Lerp(.16f, .95f, strength);
            var elapsed = 0f;
            PlayUiTone(Mathf.Lerp(110f, 55f, strength), duration * .5f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                glitchOverlay.color = new Color(.42f, .01f, .01f,
                    UnityEngine.Random.Range(.03f, .17f + strength * .22f));
                for (var i = 0; i < glitchStripes.childCount; i++)
                {
                    var stripe = glitchStripes.GetChild(i) as RectTransform;
                    if (stripe == null) continue;
                    stripe.gameObject.SetActive(UnityEngine.Random.value < .28f + strength * .35f);
                    stripe.anchorMin = new Vector2(0, UnityEngine.Random.value);
                    stripe.anchorMax = new Vector2(1, stripe.anchorMin.y);
                    stripe.anchoredPosition = Vector2.zero;
                    stripe.sizeDelta = new Vector2(UnityEngine.Random.Range(-80, 90), UnityEngine.Random.Range(2, 18));
                }
                yield return null;
            }
            glitchOverlay.color = new Color(0, 0, 0, 0);
            for (var i = 0; i < glitchStripes.childCount; i++) glitchStripes.GetChild(i).gameObject.SetActive(false);
            glitchRoutine = null;
        }

        private void CloseModal()
        {
            modalOpen = false;
            if (modalLayer != null)
            {
                ClearChildren(modalLayer);
                modalLayer.gameObject.SetActive(false);
            }
        }

        private IEnumerator EndingRoutine()
        {
            TriggerGlitch(1f);
            yield return new WaitForSecondsRealtime(1.05f);
            ShowMessage("저장 중… 25%\n방의 벽과 가구를 현재 파일로 덮어쓰는 중.", 2.6f);
            yield return new WaitForSecondsRealtime(2.9f);
            TriggerGlitch(.8f);
            ShowMessage("저장 중… 74%\nOCCUPANT_00 복원.\n플레이어 프로세스를 종료합니다.", 3.4f);
            yield return new WaitForSecondsRealtime(3.7f);
            TriggerGlitch(1f);
            yield return new WaitForSecondsRealtime(.7f);

            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messagePanel.gameObject.SetActive(false);
            var end = RuntimeUI.Panel("Ending", hudCanvas.transform, new Color(.005f, .006f, .008f, .985f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            end.transform.SetAsLastSibling();
            var title = RuntimeUI.Text("Title", end.transform, "CTRL+S", 82,
                new Color(.68f, .08f, .065f), TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(.15f, .55f);
            title.rectTransform.anchorMax = new Vector2(.85f, .82f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
            var twist = RuntimeUI.Text("Twist", end.transform,
                "당신은 잃어버린 파일을 찾으러 온 사람이 아니었다.\n당신이 바로, 사람을 복구하기 위해 만들어진 자동 저장 파일이었다.\n\nSAVE COMPLETE — PLAYER PROCESS DELETED",
                30, new Color(.78f, .78f, .74f), TextAnchor.MiddleCenter);
            twist.rectTransform.anchorMin = new Vector2(.12f, .25f);
            twist.rectTransform.anchorMax = new Vector2(.88f, .58f);
            twist.rectTransform.offsetMin = twist.rectTransform.offsetMax = Vector2.zero;
            var restart = RuntimeUI.Button("Restart", end.transform, "처음부터 다시 시작", () =>
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex),
                new Color(.18f, .035f, .035f, 1f), new Color(.48f, .07f, .055f, 1f), 23);
            restart.GetComponent<RectTransform>().anchorMin = new Vector2(.5f, .14f);
            restart.GetComponent<RectTransform>().anchorMax = new Vector2(.5f, .14f);
            restart.GetComponent<RectTransform>().sizeDelta = new Vector2(340, 64);
            restart.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
        }
    }
}
