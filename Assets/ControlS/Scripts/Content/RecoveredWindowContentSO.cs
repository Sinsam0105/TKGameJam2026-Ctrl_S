using UnityEngine;
namespace ControlS
{
    [CreateAssetMenu(fileName = "RecoveredWindowContent", menuName = "CONTROL S/Content/Desktop/Recovered")]
    public sealed class RecoveredWindowContentSO : DesktopWindowContentSO
    {
        [TextArea(4, 10)] public string warning;
        public string commitButton;
    }
}
