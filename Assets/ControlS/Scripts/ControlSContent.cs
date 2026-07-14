using System;
using UnityEngine;

namespace ControlS
{
    [CreateAssetMenu(fileName = "DefaultControlSContent", menuName = "CONTROL S/Game Content")]
    public sealed class ControlSContent : ScriptableObject
    {
        [Serializable]
        public sealed class PuzzleSettings
        {
            public string timePassword = "0217";
            public string repairedFileName = "SAVE_S.tmp";
            public string drawerCode = "4312";
            public int[] archiveSequence = { 3, 1, 4, 2 };
            [Range(0f, 1f)] public float photoBrightnessThreshold = .72f;
        }

        [Serializable]
        public sealed class ObjectiveContent
        {
            public string ending = "RECOVERY COMPLETE";
            public string inspectRoom = "방을 조사하고 저장 실패의 흔적을 찾자.";
            public string unlockRecovery = "컴퓨터의 RECOVERY 폴더를 잠금 해제하자.";
            public string collectKeycap = "시계 아래를 다시 확인하자.";
            public string repairFile = "손상된 임시 파일의 이름을 복원하자.";
            public string analyzePhoto = "dark_photo.img를 분석하자.";
            public string openDrawer = "사진의 단서로 오른쪽 서랍을 열자.";
            public string restoreFamily = "휴지통에서 올바른 파일만 복원하자.";
            public string inspectRestoredPhoto = "방에 되돌아온 사진을 확인하자.";
            public string solveArchive = "사진의 순서대로 ARCHIVE 조각을 실행하자.";
            public string openRecoveredFile = "바탕화면에 나타난 마지막 복구 파일을 열자.";
            public string hudFormat = "복구 진행  {0}/4     |     {1}";
        }

        [Serializable]
        public sealed class StoryContent
        {
            public NarrationSO intro;
            public NarrationSO clockFirst;
            public NarrationSO keycapFound;
            public NarrationSO clockAfterKeycap;
            public NarrationSO clockHint;
            public NarrationSO inspectClockFirst;
            public NarrationSO timeDenied;
            public NarrationSO timeUnlocked;
            public NarrationSO missingKeycap;
            public NarrationSO repairFailed;
            public NarrationSO repairSuccess;
            public NarrationSO photoRevealed;
            public NarrationSO drawerNoClue;
            public NarrationSO drawerWrong;
            public NarrationSO drawerOpened;
            public NarrationSO drawerAlreadyOpened;
            public NarrationSO restoreNoClue;
            public NarrationSO restoreWrong;
            public NarrationSO restoreSuccess;
            public NarrationSO photoFirst;
            public NarrationSO photoAgain;
            public NarrationSO archiveWrong;
            public NarrationSO archiveSuccess;
            public NarrationSO doorLocked;
            public NarrationSO doorLate;
            public NarrationSO ending25;
            public NarrationSO ending74;
        }

        [Serializable]
        public sealed class HudContent
        {
            public string controls = "WASD / 방향키  이동    E / Enter  조사    ESC  컴퓨터 닫기";
            public string keypadTitle = "DRAWER LOCK / 4 DIGITS";
            public string keypadHintRevealed = "사진에서 드러난 네 자리 숫자";
            public string keypadHintMissing = "단서가 필요하다.";
            public string keypadPlaceholder = "0000";
            public string keypadSubmit = "서랍 열기";
            public string close = "닫기";
            public string endingTitle = "CTRL+S";
            [TextArea(4, 8)] public string endingBody = "당신은 잃어버린 파일을 찾으러 온 사람이 아닙니다.\n당신이 바로, 사람을 복구하기 위해 만들어진 자동 저장 파일이었습니다.\n\nSAVE COMPLETE — PLAYER PROCESS DELETED";
            public string restart = "처음부터 다시 시작";
        }

        [Serializable]
        public sealed class DesktopContent
        {
            public string watermark = "SAFE MODE  /  USER: unknown  /  02:17";
            public string exitButton = "컴퓨터 닫기  [ESC]";
            public string readmeIcon = "README.txt";
            public string recoveryIcon = "RECOVERY";
            public string darkPhotoIcon = "dark_photo.img";
            public string recycleBinIcon = "휴지통";
            public string archiveIcon = "ARCHIVE";
            public string recoveredIcon = "RECOVERED.save";
            public string readmeTitle = "README.txt - 메모장";
            [TextArea(6, 12)] public string readmeBody = "복구 절차\n\n1. 생성 시각으로 RECOVERY 잠금 해제\n2. 손상된 저장 파일명 복원\n3. 이미지 밝기 분석\n4. 지시된 파일만 복원\n5. 조각을 올바른 순서로 실행\n\n주의: 복구 대상이 누구인지 확인하지 말 것.";
            public string recoveryTitle = "RECOVERY";
            public string recoveryLocked = "폴더가 잠겨 있습니다.\n파일 생성 시각 4자리를 입력하십시오.";
            public string passwordPlaceholder = "HHMM";
            public string unlockButton = "잠금 해제";
            public string recoveryWaiting = "키보드 입력 일부가 누락되었습니다.\n방에서 단서를 찾으십시오.";
            public string repairPrompt = "손상 파일: SAVE_?.tmp\n정상 파일명을 입력하십시오.";
            public string fileNamePlaceholder = "SAVE_?.tmp";
            public string repairButton = "파일 복구";
            public string repairComplete = "SAVE_S.tmp\n상태: 복구 완료\n연결된 이미지 캐시가 해제되었습니다.";
            public string photoTitle = "dark_photo.img - 이미지 분석기";
            public string photoHiddenClue = "DRAWER\n4 3 1 2";
            public string brightnessFormat = "밝기  {0}%";
            public string recycleTitle = "휴지통";
            public string recycleUnknown = "삭제된 파일 2개. 어떤 파일을 복원해야 할지 알 수 없다.";
            public string recycleKnown = "메모 지시: FAMILY.PNG만 복원";
            public string recycleDone = "휴지통이 비어 있습니다.\n복원된 파일이 현실 쪽 경로로 이동했습니다.";
            public string familyFile = "FAMILY.PNG";
            public string familyDetail = "가족 사진 / 2.1 MB";
            public string occupantFile = "OCCUPANT_00.dat";
            public string occupantDetail = "알 수 없는 점유자 / 0 KB";
            public string restoreButton = "복원";
            public string archiveTitle = "ARCHIVE - fragment executor";
            public string archiveLocked = "🔒 ARCHIVE\n\n의존 파일 SAVE_?.tmp가 손상되어 폴더를 열 수 없습니다.";
            public string archiveUnknown = "손상 로그 조각 4개가 발견되었습니다.\n실행 순서를 알 수 없습니다.\n\n복원된 현실 오브젝트에 인덱스가 기록되어 있습니다.";
            public string archiveSolved = "RECOVERED.save 생성 완료.\n바탕화면을 확인하십시오.";
            public string archiveSequencePrefix = "입력: ";
            public string archiveEmpty = "(없음)";
            public string recoveredTitle = "RECOVERED.save";
            [TextArea(4, 8)] public string recoveredWarning = "경고\n\n이 파일은 저장 데이터가 아닙니다.\n현재 실행 중인 복구 프로세스의 원본입니다.\n\n열면 현재 세션은 저장되고 종료됩니다.";
            public string recoveredButton = "CTRL+S — 저장하고 열기";
        }

        [Header("Puzzle Answers")]
        public PuzzleSettings puzzles = new PuzzleSettings();
        [Header("Objectives")]
        public ObjectiveContent objectives = new ObjectiveContent();
        [Header("Narration")]
        public StoryContent story = new StoryContent();
        [Header("HUD")]
        public HudContent hud = new HudContent();
        [Header("Virtual Desktop")]
        public DesktopContent desktop = new DesktopContent();
    }
}
