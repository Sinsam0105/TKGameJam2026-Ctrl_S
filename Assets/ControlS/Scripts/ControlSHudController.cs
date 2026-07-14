using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    /// <summary>Owns all room HUD, narration, modal and glitch presentation.</summary>
    public sealed class ControlSHudController : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text messageText;
        [SerializeField] private Image messagePanel;
        [SerializeField] private Image glitchOverlay;
        [SerializeField] private RectTransform glitchStripes;

        private ControlSContent content;
        private ControlSState state;
        private ControlSAtmosphereController atmosphere;
        private Coroutine messageRoutine;
        private Coroutine glitchRoutine;
        private GameObject activeInteractionUI;

        public bool BlocksRoomInput => activeInteractionUI != null && activeInteractionUI.activeInHierarchy;

        public void Configure(Canvas targetCanvas, Text objective, Text prompt, Text narration,
            Image narrationPanel, Image glitch, RectTransform stripes)
        {
            canvas = targetCanvas;
            objectiveText = objective;
            promptText = prompt;
            messageText = narration;
            messagePanel = narrationPanel;
            glitchOverlay = glitch;
            glitchStripes = stripes;
        }

        public bool ValidateReferences() => canvas != null && objectiveText != null && promptText != null &&
            messageText != null && messagePanel != null && glitchOverlay != null &&
            glitchStripes != null;

        public void Initialize(ControlSContent gameContent, ControlSState gameState,
            ControlSAtmosphereController atmosphereController)
        {
            content = gameContent;
            state = gameState;
            atmosphere = atmosphereController;
            state.NarrationRequested += ShowNarration;
            state.GlitchRequested += TriggerGlitch;
            state.Changed += UpdateObjective;

            foreach (var text in canvas.GetComponentsInChildren<Text>(true)) text.font = RuntimeUI.Font;
            var controls = canvas.transform.Find("Controls")?.GetComponent<Text>();
            if (controls != null) controls.text = content.hud.controls;
            UpdateObjective();
        }

        public void SetPrompt(string value)
        {
            if (promptText != null) promptText.text = value;
        }

        /// <summary>UnityEvent에서 받은 NarrationSO를 표시합니다.</summary>
        public void ShowNarration(NarrationSO narration)
        {
            if (narration == null) return;
            ShowMessage(narration.Text, narration.Duration);
        }

        /// <summary>씬에 배치된 UI GameObject를 활성화하고 입력 차단 대상으로 전환합니다.</summary>
        public void ShowUI(GameObject uiObject)
        {
            if (uiObject == null) return;
            if (activeInteractionUI != null && activeInteractionUI != uiObject)
                activeInteractionUI.SetActive(false);
            activeInteractionUI = uiObject;
            uiObject.SetActive(true);
            uiObject.transform.SetAsLastSibling();
            SetPrompt(string.Empty);
        }

        public void HideUI(GameObject uiObject)
        {
            if (uiObject == null) return;
            uiObject.SetActive(false);
            if (activeInteractionUI == uiObject) activeInteractionUI = null;
        }

        public void ToggleUI(GameObject uiObject)
        {
            if (uiObject == null) return;
            if (uiObject.activeSelf) HideUI(uiObject);
            else ShowUI(uiObject);
        }

        public void HideCurrentUI()
        {
            if (activeInteractionUI != null) HideUI(activeInteractionUI);
        }

        private void UpdateObjective()
        {
            if (objectiveText != null)
                objectiveText.text = string.Format(content.objectives.hudFormat, state.CompletedPuzzleCount, state.Objective);
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
            atmosphere?.PlayUiTone(Mathf.Lerp(110f, 55f, strength), duration * .5f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                glitchOverlay.color = new Color(.42f, .01f, .01f,
                    Random.Range(.03f, .17f + strength * .22f));
                for (var i = 0; i < glitchStripes.childCount; i++)
                {
                    var stripe = glitchStripes.GetChild(i) as RectTransform;
                    if (stripe == null) continue;
                    stripe.gameObject.SetActive(Random.value < .28f + strength * .35f);
                    stripe.anchorMin = new Vector2(0, Random.value);
                    stripe.anchorMax = new Vector2(1, stripe.anchorMin.y);
                    stripe.anchoredPosition = Vector2.zero;
                    stripe.sizeDelta = new Vector2(Random.Range(-80, 90), Random.Range(2, 18));
                }
                yield return null;
            }
            glitchOverlay.color = new Color(0, 0, 0, 0);
            for (var i = 0; i < glitchStripes.childCount; i++) glitchStripes.GetChild(i).gameObject.SetActive(false);
            glitchRoutine = null;
        }

    }
}
