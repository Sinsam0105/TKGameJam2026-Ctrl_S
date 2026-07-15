using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class DrawerKeypadContent : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private InputField codeInput;
        [SerializeField] private Text hintText;
        [SerializeField] private TextEnter enter;
        [SerializeField] private ProgressFlagSO clueRevealedFlag;
        [SerializeField] private HudContentSO content;

        public GameObject Root => root;
        public bool ValidateReferences() => root != null && codeInput != null && hintText != null && enter != null;

        private void Awake()
        {
            if (root != null && root != gameObject) root.SetActive(false);
        }

        private void OnEnable()
        {
            if (content == null && ContentManager.HasInstance) content = ContentManager.Instance.Current?.hud;
            if (hintText != null && content != null)
                hintText.text = clueRevealedFlag != null && ProgressManager.Instance.GetFlag(clueRevealedFlag)
                    ? content.drawerHintRevealed : content.drawerHintMissing;
            enter?.Reset();
            if (codeInput != null)
            {
                if (content != null && codeInput.placeholder is Text placeholder) placeholder.text = content.drawerPlaceholder;
                codeInput.Select();
                codeInput.ActivateInputField();
            }
        }

        public void Submit() => enter?.EnterAnswer();
        public void Close() => UIManager.Instance?.HideUI(root != null ? root : gameObject);
    }
}
