using UnityEngine;
namespace ControlS
{
    [CreateAssetMenu(fileName = "PhotoWindowContent", menuName = "CONTROL S/Content/Desktop/Photo")]
    public sealed class PhotoWindowContentSO : DesktopWindowContentSO
    {
        [TextArea] public string hiddenClue;
        public string brightnessFormat = "Brightness {0}%";
    }
}
