using UnityEngine;

namespace ControlS
{
    public sealed class RecoveredDesktopWindow : MonoBehaviour
    {
        [SerializeField] private DesktopWindow window;
        [SerializeField] private InteractionSequence endingSequence;

        public void Configure(DesktopWindow ownerWindow, InteractionSequence ending)
        {
            window = ownerWindow;
            endingSequence = ending;
        }

        public void Commit()
        {
            window?.Close();
            ControlSSceneController.Current?.CloseDesktop();
            endingSequence?.Execute();
        }
    }
}
