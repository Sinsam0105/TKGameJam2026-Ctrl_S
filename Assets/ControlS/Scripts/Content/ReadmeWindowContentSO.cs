using UnityEngine;
namespace ControlS
{
    [CreateAssetMenu(fileName = "ReadmeWindowContent", menuName = "CONTROL S/Content/Desktop/Readme")]
    public sealed class ReadmeWindowContentSO : DesktopWindowContentSO
    {
        [TextArea(6, 14)] public string body;
    }
}
