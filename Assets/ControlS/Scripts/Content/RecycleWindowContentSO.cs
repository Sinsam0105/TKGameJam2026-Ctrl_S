using UnityEngine;
namespace ControlS
{
    [CreateAssetMenu(fileName = "RecycleWindowContent", menuName = "CONTROL S/Content/Desktop/Recycle Bin")]
    public sealed class RecycleWindowContentSO : DesktopWindowContentSO
    {
        [TextArea] public string unknown, known, empty;
        public string familyFile, familyDetail, occupantFile, occupantDetail, restoreButton;
    }
}
