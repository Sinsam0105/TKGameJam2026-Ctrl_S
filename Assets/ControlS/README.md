# CONTROL S — game-jam vertical slice

`Assets/Scenes/SampleScene.unity` 안에 방, 플레이어, 상호작용 지점, HUD, 가상 데스크톱 Canvas와 EventSystem이 실제 씬 계층으로 구성되어 있습니다. 씬을 열고 Play하면 바로 실행됩니다.

## 조작

- 이동: `WASD` 또는 방향키
- 조사/사용: `E` 또는 `Enter`
- 컴퓨터 닫기: `Esc` 또는 작업 표시줄의 `방으로 돌아가기`
- 데스크톱 창: 제목 표시줄 드래그, 우측 `×`로 닫기

## 구현된 플레이 흐름

1. 멈춘 시계의 시각으로 `RECOVERY` 폴더 잠금을 해제하고, 키캡 단서로 손상 파일명을 복원합니다.
2. `dark_photo.img`의 밝기를 조절해 서랍 암호를 찾고, 방의 서랍을 엽니다.
3. 서랍 메모에 따라 휴지통에서 올바른 파일만 복원하면 방에 액자가 생깁니다.
4. 액자의 실행 순서대로 `ARCHIVE` 조각을 열어 마지막 저장 파일과 엔딩 반전을 확인합니다.

## 코드 구조

- `ControlSSceneController.cs`: 씬 참조 연결, 입력 UI, 오디오, 글리치와 엔딩
- `RoomWorld.cs`: 씬에 배치된 방 참조, 플레이어 이동/충돌, 근접 상호작용
- `VirtualDesktop.cs`: 파일/폴더 아이콘, 드래그 가능한 창, 데스크톱 퍼즐
- `ControlSState.cs`: 현실 공간과 컴퓨터 공간을 잇는 진행 상태
- `RuntimeUI.cs`: 외부 프리팹 없이 UI를 생성하는 공용 빌더

임시 도형은 `SampleScene`의 `CONTROL S - Scene Root/Room` 아래에 저장되어 있습니다. 이후 아트가 준비되면 Hierarchy에서 각 오브젝트를 프리팹/타일맵으로 교체하고 `RoomInteractable` 및 `RoomSceneView` 참조를 그대로 유지하면 됩니다.

씬을 다시 생성해야 할 때만 Unity 메뉴 `Tools > CONTROL S > Rebuild SampleScene`을 사용합니다. 이 빌더는 개발 편의용이며 게임 실행 시 호출되지 않습니다.

## 자동 스모크 테스트

Unity CLI에서 `-executeMethod ControlS.Editor.ControlSPlayModeValidation.Run`을 호출하면 SampleScene을 Play하고 방, 플레이어, HUD, 가상 데스크톱 초기화 및 런타임 예외를 검사합니다.
