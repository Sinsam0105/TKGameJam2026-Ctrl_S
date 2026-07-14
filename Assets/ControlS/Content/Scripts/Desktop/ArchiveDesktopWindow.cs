using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class ArchiveDesktopWindow : MonoBehaviour
    {
        [SerializeField] private GameObject lockedPanel;
        [SerializeField] private GameObject unknownPanel;
        [SerializeField] private GameObject puzzlePanel;
        [SerializeField] private GameObject solvedPanel;
        [SerializeField] private Text sequenceStatus;

        private readonly List<int> sequence = new List<int>();

        public void Configure(GameObject locked, GameObject unknown, GameObject puzzle, GameObject solved,
            Text status)
        {
            lockedPanel = locked;
            unknownPanel = unknown;
            puzzlePanel = puzzle;
            solvedPanel = solved;
            sequenceStatus = status;
        }

        public void Refresh()
        {
            var state = ControlSState.Current;
            if (state == null) return;
            lockedPanel.SetActive(!state.SaveFileRepaired);
            unknownPanel.SetActive(state.SaveFileRepaired && !state.PhotoSequenceDiscovered);
            solvedPanel.SetActive(state.ArchiveSequenceSolved);
            puzzlePanel.SetActive(state.SaveFileRepaired && state.PhotoSequenceDiscovered &&
                                  !state.ArchiveSequenceSolved);
            sequence.Clear();
            sequenceStatus.text = state.Content.desktop.archiveSequencePrefix + state.Content.desktop.archiveEmpty;
        }

        public void PushFragment(int index)
        {
            var state = ControlSState.Current;
            if (state == null || !puzzlePanel.activeSelf) return;
            if (sequence.Count >= 4) sequence.Clear();
            sequence.Add(index);
            sequenceStatus.text = state.Content.desktop.archiveSequencePrefix + string.Join("  →  ", sequence);
            ControlSSceneController.Current?.PlayUiTone(330f + index * 65f, .07f);
            if (sequence.Count != 4) return;
            if (state.TryArchiveSequence(sequence.ToArray())) Refresh();
            else
            {
                sequence.Clear();
                sequenceStatus.text = state.Content.desktop.archiveSequencePrefix + state.Content.desktop.archiveEmpty;
            }
        }
    }
}
