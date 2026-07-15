using UnityEngine;
namespace ControlS
{
    [CreateAssetMenu(fileName = "HudContent", menuName = "CONTROL S/Content/HUD")]
    public sealed class HudContentSO : ScriptableObject
    {
        [TextArea] public string controls;
        public string drawerTitle, drawerHintRevealed, drawerHintMissing, drawerPlaceholder, drawerSubmit, close;
        public string endingTitle;
        [TextArea(4, 10)] public string endingBody;
        public string restart;
    }
}
