using System;
using UnityEngine;

namespace ControlS
{
    /// <summary>
    /// Small, scene-independent state store for the game-jam vertical slice.
    /// Room interactions and desktop files both talk to this object.
    /// </summary>
    public sealed class ControlSState
    {
        public static ControlSState Current { get; private set; }

        public event Action Changed;
        public event Action<string, float> MessageRequested;
        public event Action<float> GlitchRequested;

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
                if (EndingStarted) return "RECOVERY COMPLETE";
                if (!ClockInspected) return "방을 조사하고 저장 실패의 흔적을 찾자.";
                if (!TimePasswordSolved) return "컴퓨터의 RECOVERY 폴더를 잠금 해제하자.";
                if (!KeycapCollected) return "시계 아래를 다시 확인하자.";
                if (!SaveFileRepaired) return "손상된 임시 파일의 이름을 복원하자.";
                if (!PhotoClueRevealed) return "dark_photo.img를 분석하자.";
                if (!DrawerOpened) return "사진의 단서로 오른쪽 서랍을 열자.";
                if (!FamilyPhotoRestored) return "휴지통에서 올바른 파일만 복원하자.";
                if (!PhotoSequenceDiscovered) return "방에 되돌아온 사진을 확인하자.";
                if (!ArchiveSequenceSolved) return "사진의 순서대로 ARCHIVE 조각을 실행하자.";
                return "바탕화면에 나타난 마지막 복구 파일을 열자.";
            }
        }

        public ControlSState()
        {
            Current = this;
        }

        public void InspectClock()
        {
            if (!ClockInspected)
            {
                ClockInspected = true;
                Say("멈춘 시계. 02:17에서 초침까지 굳어 있다.\n뒤쪽에는 뭔가 끼어 있지만 지금은 빠지지 않는다.", 5f);
                Notify();
                return;
            }

            if (TimePasswordSolved && !KeycapCollected)
            {
                KeycapCollected = true;
                Say("시계 아래에서 빠진 키캡을 찾았다. 글자는 'S'.\n누가 일부러 여기 숨겨 둔 것 같다.", 5f);
                Glitch(0.22f);
                Notify();
                return;
            }

            Say(KeycapCollected
                ? "시계는 여전히 02:17이다. 조금 전보다 째깍거리는 소리가 가까워졌다."
                : "02:17. 컴퓨터의 저장 실패 시각과 관련이 있을까?", 3.5f);
        }

        public bool TryTimePassword(string value)
        {
            if (!ClockInspected)
            {
                Say("숫자는 맞는 것 같지만 확신할 근거가 없다. 방을 직접 확인해야 한다.", 3.5f);
                return false;
            }

            if (Normalize(value) != "0217")
            {
                Say("ACCESS DENIED — 생성 시각 불일치", 2.5f);
                Glitch(0.12f);
                return false;
            }

            if (!TimePasswordSolved)
            {
                TimePasswordSolved = true;
                Say("RECOVERY 잠금이 풀렸다.\nSAVE_?.tmp — 파일명 한 글자가 손상되어 있다.", 4.5f);
                Notify();
            }
            return true;
        }

        public bool TryRepairSaveName(string value)
        {
            if (!KeycapCollected)
            {
                Say("누락된 글자를 먼저 찾아야 한다.", 2.5f);
                return false;
            }

            var normalized = (value ?? string.Empty).Trim().Replace(" ", string.Empty);
            if (!normalized.Equals("SAVE_S.tmp", StringComparison.OrdinalIgnoreCase))
            {
                Say("복구 실패 — 체크섬과 파일명이 일치하지 않는다.", 2.5f);
                Glitch(0.16f);
                return false;
            }

            if (!SaveFileRepaired)
            {
                SaveFileRepaired = true;
                Say("SAVE_S.tmp 복원 완료.\n그 순간 방의 전등이 한 번 꺼졌다.", 4f);
                Glitch(0.32f);
                Notify();
            }
            return true;
        }

        public void RevealPhotoClue()
        {
            if (PhotoClueRevealed) return;
            PhotoClueRevealed = true;
            Say("밝기를 올리자 사진 속 서랍에 숫자가 드러났다. 4312.", 4f);
            Notify();
        }

        public bool TryDrawerCode(string value)
        {
            if (!PhotoClueRevealed)
            {
                Say("네 자리 잠금이다. 아직 단서가 없다.", 2.5f);
                return false;
            }

            if (Normalize(value) != "4312")
            {
                Say("서랍 안쪽에서 금속이 걸리는 소리가 났다.", 2f);
                return false;
            }

            if (!DrawerOpened)
            {
                DrawerOpened = true;
                Say("서랍 속 메모: '휴지통에서 FAMILY.PNG만 복원할 것.'\n문장 아래에는 낯선 필체로 '나를 복원하지 마'라고 쓰여 있다.", 6f);
                Glitch(0.24f);
                Notify();
            }
            return true;
        }

        public bool TryRestoreFile(bool familyPhoto)
        {
            if (!DrawerOpened)
            {
                Say("무엇을 복원해야 할지 판단할 단서가 없다.", 2.5f);
                return false;
            }

            if (!familyPhoto)
            {
                Say("복원 실패. 파일 안쪽에서 누군가 문을 두드리는 소리가 난다.", 3f);
                Glitch(0.7f);
                return false;
            }

            if (!FamilyPhotoRestored)
            {
                FamilyPhotoRestored = true;
                Say("FAMILY.PNG 복원 완료.\n컴퓨터 밖, 비어 있던 벽에 액자가 생겼다.", 4.5f);
                Glitch(0.35f);
                Notify();
            }
            return true;
        }

        public void InspectRestoredPhoto()
        {
            if (!FamilyPhotoRestored) return;
            if (!PhotoSequenceDiscovered)
            {
                PhotoSequenceDiscovered = true;
                Say("사진 속 가족들의 얼굴은 모두 지워져 있다.\n뒷면에 적힌 실행 순서: 3 → 1 → 4 → 2", 5.5f);
                Notify();
                return;
            }
            Say("액자 유리에 비친 방에는… 플레이어가 없다.\n뒷면: 3 → 1 → 4 → 2", 4f);
        }

        public bool TryArchiveSequence(int[] sequence)
        {
            var target = new[] { 3, 1, 4, 2 };
            if (sequence == null || sequence.Length != target.Length) return false;
            for (var i = 0; i < target.Length; i++)
            {
                if (sequence[i] != target[i])
                {
                    Say("조각 순서가 틀렸다. 열린 로그가 스스로 닫혔다.", 2.5f);
                    Glitch(0.3f);
                    return false;
                }
            }

            if (!ArchiveSequenceSolved)
            {
                ArchiveSequenceSolved = true;
                Say("조각 결합 완료: RECOVERED.save\n파일 크기: 0 KB / 수정한 사람: YOU", 5f);
                Glitch(0.55f);
                Notify();
            }
            return true;
        }

        public void StartEnding()
        {
            if (EndingStarted) return;
            EndingStarted = true;
            Notify();
        }

        public void RequestMessage(string text, float duration = 3.5f) => Say(text, duration);
        public void RequestGlitch(float strength = 0.25f) => Glitch(strength);

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Replace(":", string.Empty).Replace(" ", string.Empty).Trim();
        }

        private void Say(string text, float duration) => MessageRequested?.Invoke(text, duration);
        private void Glitch(float strength) => GlitchRequested?.Invoke(Mathf.Clamp01(strength));
        private void Notify() => Changed?.Invoke();
    }
}
