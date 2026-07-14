using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class RecoveryDesktopWindow : MonoBehaviour
    {
        [SerializeField] private GameObject lockedPanel;
        [SerializeField] private GameObject waitingPanel;
        [SerializeField] private GameObject repairPanel;
        [SerializeField] private GameObject completePanel;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private InputField fileNameInput;

        public void Configure(GameObject locked, GameObject waiting, GameObject repair, GameObject complete,
            InputField password, InputField fileName)
        {
            lockedPanel = locked;
            waitingPanel = waiting;
            repairPanel = repair;
            completePanel = complete;
            passwordInput = password;
            fileNameInput = fileName;
        }

        public void Refresh()
        {
            var state = ControlSState.Current;
            if (state == null) return;
            SetOnly(!state.TimePasswordSolved ? lockedPanel :
                !state.KeycapCollected ? waitingPanel :
                !state.SaveFileRepaired ? repairPanel : completePanel);
            if (lockedPanel.activeSelf)
            {
                passwordInput.text = string.Empty;
                passwordInput.Select();
                passwordInput.ActivateInputField();
            }
        }

        public void SubmitPassword()
        {
            var state = ControlSState.Current;
            if (state != null && state.TryTimePassword(passwordInput.text)) Refresh();
        }

        public void SubmitRepairName()
        {
            var state = ControlSState.Current;
            if (state != null && state.TryRepairSaveName(fileNameInput.text)) Refresh();
        }

        private void SetOnly(GameObject active)
        {
            lockedPanel.SetActive(active == lockedPanel);
            waitingPanel.SetActive(active == waitingPanel);
            repairPanel.SetActive(active == repairPanel);
            completePanel.SetActive(active == completePanel);
        }
    }
}
