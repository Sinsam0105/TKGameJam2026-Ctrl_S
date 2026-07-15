using System.Collections.Generic;
using UnityEngine;
namespace ControlS
{
    [CreateAssetMenu(fileName = "GameContentSet", menuName = "CONTROL S/Content/Game Content Set")]
    public sealed class GameContentSetSO : ScriptableObject
    {
        public ObjectiveSetSO objectives;
        public ProgressSetSO progress;
        public HudContentSO hud;
        public RoomArtSetSO roomArt;
        public DesktopThemeSO desktopTheme;
        public SoundscapeSO soundscape;
        public ReadmeWindowContentSO readme;
        public RecoveryWindowContentSO recovery;
        public PhotoWindowContentSO photo;
        public RecycleWindowContentSO recycleBin;
        public ArchiveWindowContentSO archive;
        public RecoveredWindowContentSO recovered;
        public List<PuzzleDefinitionSO> puzzles = new();
    }
}
