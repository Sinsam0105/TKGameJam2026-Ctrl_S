using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    /// <summary>Content-specific drawer puzzle. The UI hierarchy itself is authored in the scene.</summary>
    public sealed class DrawerKeypadContent : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private InputField codeInput;
        [SerializeField] private Text hintText;

        private ControlSContent content;
        private ControlSState state;
        private ControlSHudController hud;
        private ControlSAtmosphereController atmosphere;

        public GameObject Root => root;

        public bool ValidateReferences() => root != null && codeInput != null && hintText != null;

        public void Configure(GameObject uiRoot, InputField input, Text hint)
        {
            root = uiRoot;
            codeInput = input;
            hintText = hint;
        }

        private void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        public void Initialize(ControlSContent gameContent, ControlSState gameState,
            ControlSHudController hudController, ControlSAtmosphereController atmosphereController)
        {
            content = gameContent;
            state = gameState;
            hud = hudController;
            atmosphere = atmosphereController;
            if (codeInput != null)
            {
                codeInput.characterLimit = Mathf.Max(1, content.puzzles.drawerCode?.Length ?? 4);
                codeInput.contentType = InputField.ContentType.IntegerNumber;
            }
            if (root != null) root.SetActive(false);
        }

        public void Open(GameObject uiObject)
        {
            if (state == null || hud == null) return;
            var target = uiObject != null ? uiObject : root;
            if (target == null) return;
            root = target;
            if (hintText != null)
                hintText.text = state.PhotoClueRevealed
                    ? content.hud.keypadHintRevealed
                    : content.hud.keypadHintMissing;
            if (codeInput != null) codeInput.text = string.Empty;
            hud.ShowUI(root);
            if (codeInput != null)
            {
                codeInput.Select();
                codeInput.ActivateInputField();
            }
        }

        public void Submit()
        {
            if (state == null || codeInput == null || !state.TryDrawerCode(codeInput.text)) return;
            Close();
            atmosphere?.PlayUiTone(190f, .12f);
        }

        public void Close()
        {
            if (root != null) hud?.HideUI(root);
        }
    }
}
