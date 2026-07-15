using UnityEngine;

namespace ControlS
{
    public sealed class RecoveredDesktopWindow : MonoBehaviour
    {
        [SerializeField] private DesktopWindow window;
        [SerializeField] private InteractionSequence endingSequence;
        [SerializeField] private RecoveredWindowContentSO content;

        private void OnEnable()
        {
            if (content == null && ContentManager.HasInstance) content = ContentManager.Instance.Current?.recovered;
            if (content == null) return;
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Warning" && child.TryGetComponent<UnityEngine.UI.Text>(out var warning)) warning.text = content.warning;
                if (child.name == "Commit")
                {
                    var label = child.GetComponentInChildren<UnityEngine.UI.Text>(true);
                    if (label != null) label.text = content.commitButton;
                }
            }
        }

        public void Configure(DesktopWindow ownerWindow, InteractionSequence ending)
        {
            window = ownerWindow;
            endingSequence = ending;
        }

        public void Commit()
        {
            window?.Close();
            VirtualDesktop.Instance?.Close();
            endingSequence?.Execute();
        }
    }
}
