using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class ArchiveDesktopWindow : MonoBehaviour
    {
        [SerializeField] private ConditionalViewGroup states;
        [SerializeField] private Text sequenceStatus;
        [SerializeField] private IntSequenceEnter enter;
        [SerializeField] private ArchiveWindowContentSO content;

        public void Refresh()
        {
            if (content == null && ContentManager.HasInstance) content = ContentManager.Instance.Current?.archive;
            states?.Refresh();
            enter?.Reset();
            SetPanelText("Locked", content?.locked);
            SetPanelText("Unknown", content?.unknown);
            SetPanelText("Solved", content?.solved);
        }

        public void PushFragment(int index)
        {
            enter?.Push(index);
            SoundManager.Instance?.PlayUiTone(330f + index * 65f, .07f);
        }

        public void SetSequenceText(string value)
        {
            if (sequenceStatus != null)
                sequenceStatus.text = (content != null ? content.sequencePrefix : string.Empty) + value;
        }

        private void SetPanelText(string panelName, string value)
        {
            if (value == null) return;
            Transform panel = null;
            foreach (var child in GetComponentsInChildren<Transform>(true)) if (child.name == panelName) { panel = child; break; }
            if (panel == null) return;
            var text = panel.GetComponentInChildren<Text>(true); if (text != null) text.text = value;
        }
    }
}
