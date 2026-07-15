using UnityEngine;
namespace ControlS
{
    [CreateAssetMenu(fileName = "RecoveryWindowContent", menuName = "CONTROL S/Content/Desktop/Recovery")]
    public sealed class RecoveryWindowContentSO : DesktopWindowContentSO
    {
        [TextArea] public string locked;
        public string passwordPlaceholder, unlockButton;
        [TextArea] public string waiting;
        [TextArea] public string repairPrompt;
        public string fileNamePlaceholder, repairButton;
        [TextArea] public string complete;
    }
}
