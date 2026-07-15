# CONTROL S 콘텐츠 제작 구조

`Assets/Scenes/SampleScene.unity`가 실제 방·HUD·가상 데스크톱 구성의 원본입니다. SceneBuilder나 런타임 Bootstrap은 사용하지 않습니다.

## 새 퍼즐 만들기

1. `Create > CONTROL S > Progress > Flag`로 완료 Flag를 만듭니다.
2. `Create > CONTROL S > Puzzles > Definition`으로 `PuzzleDefinitionSO`를 만듭니다.
3. Puzzle 에셋에서 정답 Matcher, 선행 Flag 조건, 완료 Flag를 지정합니다.
4. 씬 오브젝트에 `PuzzleRunner`를 추가하고 Puzzle 에셋을 연결합니다.
5. 입력 형태에 따라 `TextEnter`, `SliderEnter`, `BoolEnter`, `IntSequenceEnter`를 추가합니다.
6. 입력 컴포넌트의 Target Puzzle에 `PuzzleRunner`를 연결하고 버튼이나 Slider UnityEvent를 입력 함수에 연결합니다.
7. `PuzzleRunner`의 Success/Incorrect/Conditions Not Met/Already Solved에 Action과 UnityEvent를 설정합니다.

새 정답 타입이나 비교 방식이 필요한 경우에만 `AnswerMatcher`를 상속합니다. 새로운 UI 입력 방식이 필요할 때만 `BaseEnter`를 상속합니다.

## 진행도와 상호작용

- `ProgressManager`는 `Dictionary<ProgressFlagSO, bool>`로 진행 상태를 관리하며 씬 전환 뒤에도 유지됩니다.
- 새 게임 재시작은 `GameSessionManager.RestartCurrentGame()`을 사용합니다.
- 방 상호작용과 DesktopShortcut은 Inspector의 Condition/Action Rule로 편집합니다.
- Flag 조건은 `ProgressFlagCondition`, 변경은 `SetProgressFlagAction`을 사용합니다.
- Narration, UI, Desktop, Glitch는 각 Action이 `UIManager`, `VirtualDesktop`, `AtmosphereManager`를 호출합니다.

## 콘텐츠와 아트 바꾸기

기본 콘텐츠 세트는 `Content/Config/DefaultGameContentSet.asset`입니다.

- `Objectives.asset`: 진행도 표시와 목표 순서
- `Hud.asset`: 조작법, 서랍 UI, 엔딩 문구
- `RoomArt.asset`: 방과 플레이어 Sprite/색상
- `DesktopTheme.asset`: 배경 Sprite/색상과 공통 문구
- `Readme`, `Recovery`, `Photo`, `Recycle`, `Archive`, `Recovered`: 창별 문구와 리소스
- `Content/Puzzles`: 정답, 비교 방식, 선행 조건
- `Content/Progress`: 모든 진행 Flag
- `Narrations`: 표시 문구와 표시 시간

씬의 `ContentManager`에서 다른 `GameContentSetSO`를 지정하면 같은 시스템과 퍼즐 입력을 유지한 채 문구와 아트 세트를 교체할 수 있습니다.
