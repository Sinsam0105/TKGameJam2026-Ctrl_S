using UnityEngine;
namespace ControlS
{
    [CreateAssetMenu(fileName = "DesktopTheme", menuName = "CONTROL S/Content/Desktop Theme")]
    public sealed class DesktopThemeSO : ScriptableObject
    {
        public Sprite wallpaper;
        public Color wallpaperColor = new(.035f, .055f, .065f, 1f);
        public string watermark, exitButton;
    }
}
