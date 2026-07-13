# CONTROL S — game-jam vertical slice

`Assets/Scenes/SampleScene.unity`를 Play하면 런타임 부트스트랩이 프로토타입 방과 UI를 자동 생성합니다.

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

- `ControlSBootstrap.cs`: 자동 시작, HUD, 입력 UI, 오디오, 글리치와 엔딩
- `RoomWorld.cs`: 2D 탑뷰 방, 플레이어 이동/충돌, 근접 상호작용
- `VirtualDesktop.cs`: 파일/폴더 아이콘, 드래그 가능한 창, 데스크톱 퍼즐
- `ControlSState.cs`: 현실 공간과 컴퓨터 공간을 잇는 진행 상태
- `RuntimeUI.cs`: 외부 프리팹 없이 UI를 생성하는 공용 빌더

임시 도형은 모두 런타임 생성이므로, 이후 아트가 준비되면 `RoomWorld`의 각 오브젝트를 프리팹/타일맵으로 바꾸고 동일한 상태·상호작용 코드를 그대로 연결할 수 있습니다.

## 자동 스모크 테스트

Unity CLI에서 `-executeMethod ControlS.Editor.ControlSPlayModeValidation.Run`을 호출하면 SampleScene을 Play하고 방, 플레이어, HUD, 가상 데스크톱 초기화 및 런타임 예외를 검사합니다.
