using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ControlS
{
    /// <summary>
    /// Runs the game using objects and UI references authored directly in SampleScene.
    /// It intentionally does not create the room, player, HUD, desktop, camera or event system.
    /// </summary>
    public sealed class ControlSSceneController : MonoBehaviour
    {
        private ControlSState state;

        [Header("Scene Systems")]
        [SerializeField] private RoomSceneView room;
        [SerializeField] private VirtualDesktop desktop;
        [SerializeField] private Camera worldCamera;

        [Header("HUD References")]
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text messageText;
        [SerializeField] private Image messagePanel;
        [SerializeField] private RectTransform modalLayer;
        [SerializeField] private Image glitchOverlay;
        [SerializeField] private RectTransform glitchStripes;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource droneSource;
        [SerializeField] private AudioSource uiSource;

        private Coroutine messageRoutine;
        private Coroutine glitchRoutine;
        private bool modalOpen;
        private bool ending;

        public bool BlocksRoomInput => modalOpen || ending || (desktop != null && desktop.IsOpen);

        public void ConfigureSceneReferences(RoomSceneView roomView, VirtualDesktop desktopView, Camera sceneCamera,
            Canvas sceneHud, Text objective, Text prompt, Text narration, Image narrationPanel,
            RectTransform modal, Image glitch, RectTransform stripes, AudioSource drone, AudioSource ui)
        {
            room = roomView;
            desktop = desktopView;
            worldCamera = sceneCamera;
            hudCanvas = sceneHud;
            objectiveText = objective;
            promptText = prompt;
            messageText = narration;
            messagePanel = narrationPanel;
            modalLayer = modal;
            glitchOverlay = glitch;
            glitchStripes = stripes;
            droneSource = drone;
            uiSource = ui;
        }

        private void Awake()
        {
            if (room == null || desktop == null || worldCamera == null || hudCanvas == null ||
                objectiveText == null || promptText == null || messageText == null || messagePanel == null ||
                modalLayer == null || glitchOverlay == null || glitchStripes == null)
            {
                Debug.LogError("CONTROL S scene references are incomplete. Rebuild SampleScene from Tools > CONTROL S > Rebuild SampleScene.", this);
                enabled = false;
                return;
            }

            state = new ControlSState();
            state.MessageRequested += ShowMessage;
            state.GlitchRequested += TriggerGlitch;
            state.Changed += UpdateObjective;

            SetupAudio();
            desktop.Initialize(this, state);
            room.Initialize(this, state);
            ApplyRuntimeFonts();
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

        private void SetupAudio()
        {
            if (uiSource == null) uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.volume = .8f;

            if (droneSource == null) droneSource = gameObject.AddComponent<AudioSource>();
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

        private void ApplyRuntimeFonts()
        {
            var dynamicFont = RuntimeUI.Font;
            if (hudCanvas != null)
            {
                foreach (var text in hudCanvas.GetComponentsInChildren<Text>(true)) text.font = dynamicFont;
            }
            if (desktop != null)
            {
                foreach (var text in desktop.GetComponentsInChildren<Text>(true)) text.font = dynamicFont;
            }
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
