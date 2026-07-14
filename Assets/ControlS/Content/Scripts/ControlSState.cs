using System;
using UnityEngine;

namespace ControlS
{
    public enum ControlSFlag
    {
        ClockInspected,
        TimePasswordSolved,
        KeycapCollected,
        SaveFileRepaired,
        PhotoClueRevealed,
        DrawerOpened,
        FamilyPhotoRestored,
        PhotoSequenceDiscovered,
        ArchiveSequenceSolved,
        EndingStarted
    }

    /// <summary>Game-specific puzzle progression driven entirely by a ControlSContent asset.</summary>
    public sealed class ControlSState
    {
        public static ControlSState Current { get; private set; }

        public event Action Changed;
        public event Action<NarrationSO> NarrationRequested;
        public event Action<float> GlitchRequested;

        public ControlSContent Content { get; }
        public bool ClockInspected { get; private set; }
        public bool TimePasswordSolved { get; private set; }
        public bool KeycapCollected { get; private set; }
        public bool SaveFileRepaired { get; private set; }
        public bool PhotoClueRevealed { get; private set; }
        public bool DrawerOpened { get; private set; }
        public bool FamilyPhotoRestored { get; private set; }
        public bool PhotoSequenceDiscovered { get; private set; }
        public bool ArchiveSequenceSolved { get; private set; }
        public bool EndingStarted { get; private set; }

        public int CompletedPuzzleCount
        {
            get
            {
                var count = 0;
                if (SaveFileRepaired) count++;
                if (DrawerOpened) count++;
                if (FamilyPhotoRestored) count++;
                if (ArchiveSequenceSolved) count++;
                return count;
            }
        }

        public string Objective
        {
            get
            {
                var o = Content.objectives;
                if (EndingStarted) return o.ending;
                if (!ClockInspected) return o.inspectRoom;
                if (!TimePasswordSolved) return o.unlockRecovery;
                if (!KeycapCollected) return o.collectKeycap;
                if (!SaveFileRepaired) return o.repairFile;
                if (!PhotoClueRevealed) return o.analyzePhoto;
                if (!DrawerOpened) return o.openDrawer;
                if (!FamilyPhotoRestored) return o.restoreFamily;
                if (!PhotoSequenceDiscovered) return o.inspectRestoredPhoto;
                if (!ArchiveSequenceSolved) return o.solveArchive;
                return o.openRecoveredFile;
            }
        }

        public ControlSState(ControlSContent content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Current = this;
        }

        public bool GetFlag(ControlSFlag flag)
        {
            return flag switch
            {
                ControlSFlag.ClockInspected => ClockInspected,
                ControlSFlag.TimePasswordSolved => TimePasswordSolved,
                ControlSFlag.KeycapCollected => KeycapCollected,
                ControlSFlag.SaveFileRepaired => SaveFileRepaired,
                ControlSFlag.PhotoClueRevealed => PhotoClueRevealed,
                ControlSFlag.DrawerOpened => DrawerOpened,
                ControlSFlag.FamilyPhotoRestored => FamilyPhotoRestored,
                ControlSFlag.PhotoSequenceDiscovered => PhotoSequenceDiscovered,
                ControlSFlag.ArchiveSequenceSolved => ArchiveSequenceSolved,
                ControlSFlag.EndingStarted => EndingStarted,
                _ => false
            };
        }

        public void SetFlag(ControlSFlag flag, bool value = true)
        {
            if (GetFlag(flag) == value) return;
            switch (flag)
            {
                case ControlSFlag.ClockInspected: ClockInspected = value; break;
                case ControlSFlag.TimePasswordSolved: TimePasswordSolved = value; break;
                case ControlSFlag.KeycapCollected: KeycapCollected = value; break;
                case ControlSFlag.SaveFileRepaired: SaveFileRepaired = value; break;
                case ControlSFlag.PhotoClueRevealed: PhotoClueRevealed = value; break;
                case ControlSFlag.DrawerOpened: DrawerOpened = value; break;
                case ControlSFlag.FamilyPhotoRestored: FamilyPhotoRestored = value; break;
                case ControlSFlag.PhotoSequenceDiscovered: PhotoSequenceDiscovered = value; break;
                case ControlSFlag.ArchiveSequenceSolved: ArchiveSequenceSolved = value; break;
                case ControlSFlag.EndingStarted: EndingStarted = value; break;
            }
            Notify();
        }

        public bool TryTimePassword(string value)
        {
            if (!ClockInspected)
            {
                Say(Content.story.inspectClockFirst);
                return false;
            }

            if (Normalize(value) != Normalize(Content.puzzles.timePassword))
            {
                Say(Content.story.timeDenied);
                Glitch(.12f);
                return false;
            }

            if (!TimePasswordSolved)
            {
                TimePasswordSolved = true;
                Say(Content.story.timeUnlocked);
                Notify();
            }
            return true;
        }

        public bool TryRepairSaveName(string value)
        {
            if (!KeycapCollected)
            {
                Say(Content.story.missingKeycap);
                return false;
            }

            var normalized = (value ?? string.Empty).Trim().Replace(" ", string.Empty);
            if (!normalized.Equals(Content.puzzles.repairedFileName, StringComparison.OrdinalIgnoreCase))
            {
                Say(Content.story.repairFailed);
                Glitch(.16f);
                return false;
            }

            if (!SaveFileRepaired)
            {
                SaveFileRepaired = true;
                Say(Content.story.repairSuccess);
                Glitch(.32f);
                Notify();
            }
            return true;
        }

        public void RevealPhotoClue()
        {
            if (PhotoClueRevealed) return;
            PhotoClueRevealed = true;
            Say(Content.story.photoRevealed);
            Notify();
        }

        public bool TryDrawerCode(string value)
        {
            if (!PhotoClueRevealed)
            {
                Say(Content.story.drawerNoClue);
                return false;
            }

            if (Normalize(value) != Normalize(Content.puzzles.drawerCode))
            {
                Say(Content.story.drawerWrong);
                return false;
            }

            if (!DrawerOpened)
            {
                DrawerOpened = true;
                Say(Content.story.drawerOpened);
                Glitch(.24f);
                Notify();
            }
            return true;
        }

        public bool TryRestoreFile(bool familyPhoto)
        {
            if (!DrawerOpened)
            {
                Say(Content.story.restoreNoClue);
                return false;
            }

            if (!familyPhoto)
            {
                Say(Content.story.restoreWrong);
                Glitch(.7f);
                return false;
            }

            if (!FamilyPhotoRestored)
            {
                FamilyPhotoRestored = true;
                Say(Content.story.restoreSuccess);
                Glitch(.35f);
                Notify();
            }
            return true;
        }

        public bool TryArchiveSequence(int[] sequence)
        {
            var target = Content.puzzles.archiveSequence;
            if (sequence == null || target == null || sequence.Length != target.Length) return false;
            for (var i = 0; i < target.Length; i++)
            {
                if (sequence[i] == target[i]) continue;
                Say(Content.story.archiveWrong);
                Glitch(.3f);
                return false;
            }

            if (!ArchiveSequenceSolved)
            {
                ArchiveSequenceSolved = true;
                Say(Content.story.archiveSuccess);
                Glitch(.55f);
                Notify();
            }
            return true;
        }

        public void RequestNarration(NarrationSO narration) => Say(narration);
        public void RequestGlitch(float strength = .25f) => Glitch(strength);

        private static string Normalize(string value) =>
            (value ?? string.Empty).Replace(":", string.Empty).Replace(" ", string.Empty).Trim();

        private void Say(NarrationSO value)
        {
            if (value != null) NarrationRequested?.Invoke(value);
        }
        private void Glitch(float strength) => GlitchRequested?.Invoke(Mathf.Clamp01(strength));
        private void Notify() => Changed?.Invoke();
    }
}
