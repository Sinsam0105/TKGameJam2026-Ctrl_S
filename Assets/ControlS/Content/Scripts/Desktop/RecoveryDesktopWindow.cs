using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class RecoveryDesktopWindow : MonoBehaviour
    {
        [SerializeField] private ConditionalViewGroup states;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private InputField fileNameInput;
        [SerializeField] private TextEnter passwordEnter;
        [SerializeField] private TextEnter fileNameEnter;
        [SerializeField] private RecoveryWindowContentSO content;

        public void Refresh()
        {
            ApplyContent();
            states?.Refresh();
            if (passwordInput != null && passwordInput.gameObject.activeInHierarchy)
            {
                passwordEnter?.Reset();
                passwordInput.Select();
                passwordInput.ActivateInputField();
            }
        }

        public void SubmitPassword() { passwordEnter?.EnterAnswer(); states?.Refresh(); }
        public void SubmitRepairName() { fileNameEnter?.EnterAnswer(); states?.Refresh(); }

        private void ApplyContent()
        {
            if (content == null && ContentManager.HasInstance) content = ContentManager.Instance.Current?.recovery;
            if (content == null) return;
            if (passwordInput != null) passwordInput.placeholder.GetComponent<Text>().text = content.passwordPlaceholder;
            if (fileNameInput != null) fileNameInput.placeholder.GetComponent<Text>().text = content.fileNamePlaceholder;
            SetText("Locked", "Info", content.locked);
            SetText("Waiting", "Message", content.waiting);
            SetText("Repair", "Info", content.repairPrompt);
            SetText("Complete", "Message", content.complete);
            SetButton("Unlock", content.unlockButton);
            SetButton("Repair", content.repairButton);
        }

        private void SetText(string parentName, string childName, string value)
        {
            var parent = Find(parentName); if (parent == null) return;
            foreach (var child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == childName && child.TryGetComponent<Text>(out var text)) { text.text = value; return; }
        }

        private void SetButton(string name, string value)
        {
            var target = Find(name); var text = target != null ? target.GetComponentInChildren<Text>(true) : null;
            if (text != null) text.text = value;
        }

        private Transform Find(string name)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }
    }
}
