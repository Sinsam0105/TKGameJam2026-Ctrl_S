using UnityEngine;
namespace ControlS
{
    [CreateAssetMenu(fileName = "ArchiveWindowContent", menuName = "CONTROL S/Content/Desktop/Archive")]
    public sealed class ArchiveWindowContentSO : DesktopWindowContentSO
    {
        [TextArea] public string locked, unknown, solved;
        public string sequencePrefix, emptySequence;
    }
}
