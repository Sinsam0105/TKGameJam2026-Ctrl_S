using System.Collections;
using Sinsam.SingletonSystem;
using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class UIManager : MonoSingleton<UIManager>
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text messageText;
        [SerializeField] private Image messagePanel;
        [SerializeField] private Image glitchOverlay;
        [SerializeField] private RectTransform glitchStripes;

        private Coroutine messageRoutine;
        private Coroutine glitchRoutine;
        private GameObject activeInteractionUI;
        private ProgressManager progressManager;
        private ContentManager contentManager;
        private SoundManager soundManager;

        public bool BlocksRoomInput => activeInteractionUI != null && activeInteractionUI.activeInHierarchy;

        protected override bool ShouldPersist() => false;

        protected override void Awake()
        {
            base.Awake();
            if (messagePanel != null) messagePanel.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            progressManager = ProgressManager.Instance;
            contentManager = ContentManager.Instance;
            soundManager = SoundManager.Instance;
            if (progressManager != null) progressManager.ProgressChanged += RefreshObjective;
            if (contentManager != null) contentManager.ContentChanged += HandleContentChanged;
            RefreshContent();
            RefreshObjective();
        }

        private void OnDisable()
        {
            if (progressManager != null) progressManager.ProgressChanged -= RefreshObjective;
            if (contentManager != null) contentManager.ContentChanged -= HandleContentChanged;
            progressManager = null;
            contentManager = null;
            soundManager = null;
        }

        public bool ValidateReferences() => canvas != null && objectiveText != null && promptText != null &&
            messageText != null && messagePanel != null && glitchOverlay != null && glitchStripes != null;

        public void RefreshContent()
        {
            var hud = contentManager != null ? contentManager.Current?.hud : null;
            if (hud == null || canvas == null) return;
            var controls = canvas.transform.Find("Controls")?.GetComponent<Text>();
            if (controls != null) controls.text = hud.controls;
            SetNamedText("Drawer Keypad UI", "Title", hud.drawerTitle);
            SetNamedText("Ending UI", "Title", hud.endingTitle);
            SetNamedText("Ending UI", "Body", hud.endingBody);
            SetNamedButton("Ending UI", "Restart", hud.restart);
        }

        private void HandleContentChanged(GameContentSetSO _) { RefreshContent(); RefreshObjective(); }

        private void SetNamedText(string parentName, string childName, string value)
        {
            var parent = FindTransform(canvas.transform, parentName);
            var child = parent != null ? FindTransform(parent, childName) : null;
            var text = child != null ? child.GetComponent<Text>() : null;
            if (text != null) text.text = value;
        }

        private void SetNamedButton(string parentName, string childName, string value)
        {
            var parent = FindTransform(canvas.transform, parentName);
            var child = parent != null ? FindTransform(parent, childName) : null;
            var text = child != null ? child.GetComponentInChildren<Text>(true) : null;
            if (text != null) text.text = value;
        }

        private static Transform FindTransform(Transform root, string targetName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == targetName) return child;
            return null;
        }

        public void RefreshObjective()
        {
            var objectives = contentManager != null ? contentManager.Current?.objectives : null;
            if (objectiveText == null || objectives == null || progressManager == null) return;
            objectiveText.text = string.Format(objectives.HudFormat,
                objectives.GetCompletedCount(progressManager), objectives.TrackedProgress.Count,
                objectives.GetCurrentObjective(progressManager));
        }

        public void SetPrompt(string value)
        {
            if (promptText != null) promptText.text = value ?? string.Empty;
        }

        public void ShowNarration(NarrationSO narration)
        {
            if (narration == null || messagePanel == null || messageText == null) return;
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(narration));
        }

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
            if (uiObject.activeSelf) HideUI(uiObject); else ShowUI(uiObject);
        }

        public void HideCurrentUI()
        {
            if (activeInteractionUI != null) HideUI(activeInteractionUI);
        }

        public void TriggerGlitch(float strength)
        {
            if (glitchOverlay == null || glitchStripes == null) return;
            if (glitchRoutine != null) StopCoroutine(glitchRoutine);
            glitchRoutine = StartCoroutine(GlitchRoutine(Mathf.Clamp01(strength)));
        }

        private IEnumerator MessageRoutine(NarrationSO narration)
        {
            messagePanel.gameObject.SetActive(true);
            messageText.text = narration.Text;
            yield return new WaitForSecondsRealtime(narration.Duration);
            messagePanel.gameObject.SetActive(false);
            messageRoutine = null;
        }

        private IEnumerator GlitchRoutine(float strength)
        {
            var duration = Mathf.Lerp(.16f, .95f, strength);
            soundManager?.PlayUiTone(Mathf.Lerp(110f, 55f, strength), duration * .5f);
            soundManager?.PlayGlitch(strength);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                glitchOverlay.color = new Color(.42f, .01f, .01f, Random.Range(.03f, .17f + strength * .22f));
                for (var i = 0; i < glitchStripes.childCount; i++)
                {
                    if (glitchStripes.GetChild(i) is not RectTransform stripe) continue;
                    stripe.gameObject.SetActive(Random.value < .28f + strength * .35f);
                    stripe.anchorMin = new Vector2(0, Random.value);
                    stripe.anchorMax = new Vector2(1, stripe.anchorMin.y);
                    stripe.anchoredPosition = Vector2.zero;
                    stripe.sizeDelta = new Vector2(Random.Range(-80, 90), Random.Range(2, 18));
                }
                yield return null;
            }
            glitchOverlay.color = Color.clear;
            for (var i = 0; i < glitchStripes.childCount; i++) glitchStripes.GetChild(i).gameObject.SetActive(false);
            glitchRoutine = null;
        }
    }
}
