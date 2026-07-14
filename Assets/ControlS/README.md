# CONTROL S 게임잼 버티컬 슬라이스

`Assets/Scenes/SampleScene.unity` 안에 방, 플레이어, 상호작용 지점, HUD, 가상 데스크톱, 키패드와 엔딩 UI가 실제 씬 오브젝트로 구성되어 있습니다. 씬을 열고 Play하면 바로 실행됩니다.

## 조작

- 이동: `WASD` 또는 방향키
- 조사/사용: `E` 또는 `Enter`
- 컴퓨터 닫기: `Esc` 또는 작업 표시줄의 `방으로 돌아가기`
- 데스크톱 창: 제목 표시줄 드래그, 오른쪽 `×`로 닫기

## 콘텐츠 수정 위치

- `Content/DefaultControlSContent.asset`: 목표, 퍼즐 정답, HUD 문구, 가상 파일 내용을 수정합니다.
- `Narrations/*.asset`: 모든 내레이션 문구와 표시 시간을 수정합니다.
- `SampleScene/Room HUD Canvas/Drawer Keypad UI`: 서랍 키패드 화면을 직접 편집합니다.
- `SampleScene/Room HUD Canvas/Ending UI`: 엔딩 화면을 직접 편집합니다.

`Drawer Keypad UI`와 `Ending UI`는 Hierarchy에 미리 배치되고 시작 시 비활성화됩니다. 런타임에 UI를 새로 조립하지 않습니다.

## 코드 역할

시스템 코드는 `Assets/ControlS/Scripts`에 있습니다.

- `ControlSSceneController`: 상태와 씬 시스템 초기화 및 공용 UnityEvent 함수만 담당합니다.
- `ControlSHudController`: NarrationSO 표시와 임의 UI GameObject의 Show/Hide/Toggle을 담당합니다.
- `ControlSAtmosphereController`: 카메라, 조명, 배경음, UI 효과음을 담당합니다.
- `InteractionRuleSystem`: Context, Condition, Action, Rule과 순차 실행기를 제공합니다.
- `RoomSceneView`, `TopDownPlayer`, `RoomInteractable`: 방 탐색과 규칙 기반 상호작용을 담당합니다.
- `VirtualDesktop`: 가상 데스크톱 시스템입니다.

게임 전용 동작은 `Assets/ControlS/Content/Scripts`에 분리되어 있습니다.

- `DrawerKeypadContent`: 서랍 키패드 열기, 입력 확인, 닫기를 담당합니다.
- `ControlSInteractionNodes`: CONTROL S 전용 Flag 조건과 Narration/UI/연출 액션을 제공합니다.
- `ControlSState`: 퍼즐 Flag와 입력 정답 검증만 보관합니다. 조사 분기는 보관하지 않습니다.

`ControlSStoryFlow`는 제거되었습니다. 조사 분기와 인트로·엔딩 순서는 특정 Manager 메서드가 아니라 씬에 직렬화된 Rule/Sequence 데이터로 결정됩니다.

## RoomInteractable 규칙 편집

각 `RoomInteractable`의 `Conditional Rules`에서 규칙을 위에서 아래로 검사합니다. 처음 조건을 모두 만족한 규칙의 액션이 순서대로 실행됩니다.

- `Add Rule`: 우선순위 규칙을 추가합니다.
- `Add Condition`: `Control S Flag`, `Completed Puzzle Count` 등의 조건을 추가합니다. 한 규칙 안의 조건은 AND입니다.
- `Add Action`: Narration 표시, Flag 변경, UI 열기, 글리치, Delay 등의 액션을 추가합니다.
- `Stop After Execution`: 이 규칙 실행 후 아래 규칙을 더 검사하지 않습니다.
- `Execute Once`: 이 오브젝트 인스턴스에서 규칙을 한 번만 실행합니다.

SampleScene에는 다음 규칙이 각 오브젝트에 직접 저장되어 있습니다.

- 컴퓨터: 조건 없음 → 데스크톱 열기
- 시계: 첫 조사 / 비밀번호 해결 후 / 키캡 회수 후 / 기본 힌트
- 서랍: 이미 열림 / 키패드 열기
- 사진: 복원 후 첫 조사 / 다시 조사
- 문: 퍼즐 3개 이상 완료 / 잠긴 문
- `Intro`, `Ending` InteractionSequence: Delay와 Narration·Flag·UI 액션을 순차 실행
- 키패드 확인/취소 → `DrawerKeypadContent.Submit/Close`
- 엔딩 다시 시작 → `ControlSSceneController.RestartScene`

기존 Payload와 UnityEvent 필드는 호환을 위해 `Legacy Payload / UnityEvents`에 남아 있으며, Rules가 비어 있을 때만 실행됩니다.

## 씬 재생성 및 검증

개발용 씬을 다시 만들 때만 Unity 메뉴 `Tools > CONTROL S > Rebuild SampleScene`을 사용합니다. 이 빌더는 게임 실행 중 호출되지 않습니다.

자동 검증은 `ControlS.Editor.ControlSPlayModeValidation.Run`을 실행해 씬 참조, 초기 비활성 UI, 5개 오브젝트의 규칙 데이터, 인트로·엔딩 Sequence와 버튼 바인딩을 확인합니다.
