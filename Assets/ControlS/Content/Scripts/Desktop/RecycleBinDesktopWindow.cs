using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class RecycleBinDesktopWindow : MonoBehaviour
    {
        [SerializeField] private Text info;
        [SerializeField] private GameObject fileList;
        [SerializeField] private GameObject emptyPanel;

        public void Configure(Text infoText, GameObject list, GameObject empty)
        {
            info = infoText;
            fileList = list;
            emptyPanel = empty;
        }

        public void Refresh()
        {
            var state = ControlSState.Current;
            if (state == null) return;
            info.text = state.DrawerOpened
                ? state.Content.desktop.recycleKnown
                : state.Content.desktop.recycleUnknown;
            fileList.SetActive(!state.FamilyPhotoRestored);
            emptyPanel.SetActive(state.FamilyPhotoRestored);
        }

        public void RestoreFamily()
        {
            var state = ControlSState.Current;
            if (state != null && state.TryRestoreFile(true)) Refresh();
        }

        public void RestoreOccupant() => ControlSState.Current?.TryRestoreFile(false);
    }
}
