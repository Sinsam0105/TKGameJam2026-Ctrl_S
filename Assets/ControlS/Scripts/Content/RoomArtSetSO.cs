using UnityEngine;
namespace ControlS
{
    [CreateAssetMenu(fileName = "RoomArtSet", menuName = "CONTROL S/Content/Room Art Set")]
    public sealed class RoomArtSetSO : ScriptableObject
    {
        public Sprite floor, player, restoredPhoto;
        public Color floorBaseColor = new(.075f, .09f, .105f, 1f);
    }
}
