using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class RecycleBinDesktopWindow : MonoBehaviour
    {
        [SerializeField] private Text info;
        [SerializeField] private ConditionalViewGroup states;
        [SerializeField] private BoolEnter enter;
        [SerializeField] private ProgressFlagSO clueFlag;
        [SerializeField] private RecycleWindowContentSO content;

        public void Refresh()
        {
            if (content == null && ContentManager.HasInstance) content = ContentManager.Instance.Current?.recycleBin;
            if (info != null && content != null)
                info.text = clueFlag != null && ProgressManager.Instance.GetFlag(clueFlag)
                    ? content.known : content.unknown;
            states?.Refresh();
            var empty = FindText("Empty", "Message");
            if (empty != null && content != null) empty.text = content.empty;
        }

        public void RestoreFamily() { enter?.Enter(true); states?.Refresh(); }
        public void RestoreOccupant() => enter?.Enter(false);

        private Text FindText(string parentName, string childName)
        {
            Transform parent = null;
            foreach (var child in GetComponentsInChildren<Transform>(true)) if (child.name == parentName) { parent = child; break; }
            if (parent == null) return null;
            foreach (var child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == childName && child.TryGetComponent<Text>(out var text)) return text;
            return null;
        }
    }
}
