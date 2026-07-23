#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 스테이지 1~5 전체 플레이 씬을 코드로 생성한다.
///
/// 방침:
///  - 스테이지 1~2는 이미 손으로 완성된 MainGameScene에 있으므로, 그 씬을 새 씬 파일로 복제해
///    확실히 동작하는 토대를 확보한 뒤 증분으로 4·5단계를 붙인다. (방 배경/가구/조각은 프리팹화되어
///    있지 않아 맨바닥에서 재현하면 깨지기 쉽다. 검증된 씬을 재사용하는 편이 안전하다.)
///  - 4단계(Workspace Recovery)는 "사진 퍼즐과 동일"하므로 PicturePuzzleCanvas 프리팹을 한 벌 더
///    인스턴스화해 재사용한다.
///  - 5단계(User Verification)는 전신거울(MirrorPlayer 아트) + 코드 입력 창을 코드로 만든다.
///  - SoundManager가 씬에 없으면 추가한다(효과음 통합 라우팅에 필요).
///  - 3단계 버전 정렬/문자 잠금 UI는 프리팹이 없어 코드로 만들 수 없다. 씬에 자리만 잡고
///    콘솔에 "Unity에서 마무리해야 할 것"을 남긴다.
///
/// 실행: 상단 메뉴 → Tools/ControlS/Build Full Game Scene (Stages 1-5)
/// 결과: Assets/Scenes/FullGameScene.unity (원본 MainGameScene은 건드리지 않는다)
/// </summary>
public static class FullGameSceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/MainGameScene.unity";
    private const string OutputScenePath = "Assets/Scenes/FullGameScene.unity";

    private const string PicturePuzzlePrefabPath =
        "Assets/ControlS/Resources/Pefabs/PictureSystem/PicturePuzzleCanvas.prefab";
    private const string MirrorSpritePath =
        "Assets/ControlS/Resources/Arts/MirrorPlayer.png";
    private const string WorkspaceRecoveryClipPath =
        "Assets/ControlS/Resources/Audio/컴퓨터 Workspace Recovery 알림음.mp3";

    private static readonly List<string> Warnings = new();

    [MenuItem("Tools/ControlS/Build Full Game Scene (Stages 1-5)")]
    public static void Build()
    {
        Warnings.Clear();

        if (!EditorUtility.DisplayDialog(
                "Build Full Game Scene",
                "MainGameScene을 복제해 FullGameScene을 만들고 4·5단계를 붙입니다.\n" +
                "기존 씬은 그대로 둡니다. 계속할까요?",
                "빌드", "취소"))
        {
            return;
        }

        Scene scene = PrepareOutputScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[FullGameSceneBuilder] 출력 씬을 준비하지 못했습니다.");
            return;
        }

        // 공유 UI/시스템 참조는 완성된 StageOneFlowController에서 읽어 재사용한다.
        StageOneFlowController stageOne = Object.FindAnyObjectByType<StageOneFlowController>(FindObjectsInactive.Include);
        if (stageOne == null)
            Warn("StageOneFlowController를 찾지 못했습니다. 공유 UI 참조 없이 진행합니다.");

        GameObject gameController = ResolveGameController(stageOne);

        EnsureSoundManager(gameController);

        SharedRefs shared = ReadSharedRefs(stageOne);

        StageFourFlowController stage4 = BuildStageFour(gameController, shared);
        StageFiveFlowController stage5 = BuildStageFive(gameController, shared);

        // 흐름 연결: 4→5, 그리고 (있다면) 3→4.
        if (stage4 != null && stage5 != null)
            SetObj(stage4, "stageFiveFlow", stage5);

        StageThreeFlowController stage3 = Object.FindAnyObjectByType<StageThreeFlowController>(FindObjectsInactive.Include);
        if (stage3 != null && stage4 != null)
            SetObj(stage3, "stageFourFlow", stage4);
        else if (stage3 == null)
            Warn("3단계(StageThreeFlowController)가 씬에 없습니다. 버전 정렬/문자 잠금 UI는 Unity에서 " +
                 "직접 구성한 뒤 StageThree→StageFour 연결과 StageTwo.stageThreeFlow 연결을 마무리하세요.");

        MarkDirtyAndSave(scene);

        Debug.Log($"[FullGameSceneBuilder] 완료: {OutputScenePath}");
        if (Warnings.Count > 0)
            Debug.LogWarning("[FullGameSceneBuilder] Unity에서 마무리해야 할 것:\n - " + string.Join("\n - ", Warnings));
    }

    // ── 씬 준비 ────────────────────────────────────────────────────────────
    private static Scene PrepareOutputScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) != null)
        {
            AssetDatabase.DeleteAsset(OutputScenePath);
            if (!AssetDatabase.CopyAsset(SourceScenePath, OutputScenePath))
            {
                Warn("MainGameScene 복제 실패. 빈 씬으로 시작합니다.");
                return NewEmptyOutputScene();
            }
            AssetDatabase.Refresh();
            return EditorSceneManager.OpenScene(OutputScenePath, OpenSceneMode.Single);
        }

        Warn("MainGameScene을 찾지 못했습니다. 빈 씬으로 시작합니다.");
        return NewEmptyOutputScene();
    }

    private static Scene NewEmptyOutputScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, OutputScenePath);
        return scene;
    }

    private static GameObject ResolveGameController(StageOneFlowController stageOne)
    {
        if (stageOne != null)
            return stageOne.gameObject;

        GameObject byName = GameObject.Find("Game Controller");
        if (byName != null)
            return byName;

        GameObject created = new GameObject("Game Controller");
        Warn("Game Controller를 찾지 못해 새로 만들었습니다. 매니저 구성은 Unity에서 확인하세요.");
        return created;
    }

    // ── SoundManager ───────────────────────────────────────────────────────
    private static void EnsureSoundManager(GameObject host)
    {
        if (Object.FindAnyObjectByType<SoundManager>(FindObjectsInactive.Include) != null)
            return;

        SoundManager sound = host.AddComponent<SoundManager>();
        // 원곡 클립이 없으면 Start에서 에러 로그가 나므로 자동 재생을 끈다. SFX API는 그대로 동작한다.
        SetBool(sound, "playOnStart", false);
        Debug.Log("[FullGameSceneBuilder] SoundManager를 Game Controller에 추가했습니다. (playOnStart=false)");
    }

    // ── 공유 참조 ──────────────────────────────────────────────────────────
    private sealed class SharedRefs
    {
        public Object ObjectiveText;
        public Object RecoveryBodyText;
        public Object RecoveryProgressText;
        public Object RecoveryWindow;
        public Object ComputerInteractable;
        public Transform RoomRoot;
    }

    private static SharedRefs ReadSharedRefs(StageOneFlowController stageOne)
    {
        SharedRefs refs = new();
        if (stageOne == null)
            return refs;

        refs.ObjectiveText = GetObj(stageOne, "objectiveText");
        refs.RecoveryBodyText = GetObj(stageOne, "recoveryBodyText");
        refs.RecoveryProgressText = GetObj(stageOne, "recoveryProgressText");
        refs.RecoveryWindow = GetObj(stageOne, "recoveryWindow");
        refs.ComputerInteractable = GetObj(stageOne, "computerInteractable");
        refs.RoomRoot = stageOne.transform;
        return refs;
    }

    // ── 4단계: Workspace Recovery ────────────────────────────────────────────
    private static StageFourFlowController BuildStageFour(GameObject host, SharedRefs shared)
    {
        StageFourFlowController stage4 = host.AddComponent<StageFourFlowController>();

        GameObject puzzlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PicturePuzzlePrefabPath);
        if (puzzlePrefab != null)
        {
            GameObject puzzle = (GameObject)PrefabUtility.InstantiatePrefab(puzzlePrefab);
            puzzle.name = "Stage4 Workspace Puzzle";
            puzzle.SetActive(false);

            PictureCollector collector = puzzle.GetComponentInChildren<PictureCollector>(true);
            PuzzleWindowedUI window = puzzle.GetComponentInChildren<PuzzleWindowedUI>(true);

            SetObj(stage4, "workspaceCollector", collector);
            SetObj(stage4, "workspaceWindow", window);
        }
        else
        {
            Warn($"PicturePuzzleCanvas 프리팹을 찾지 못했습니다({PicturePuzzlePrefabPath}). " +
                 "4단계 워크스페이스 퍼즐 창을 수동으로 연결하세요.");
        }

        SetObj(stage4, "computerInteractable", shared.ComputerInteractable);
        SetObj(stage4, "objectiveText", shared.ObjectiveText);
        SetObj(stage4, "recoveryProgressText", shared.RecoveryProgressText);
        SetObj(stage4, "recoveryWindow", shared.RecoveryWindow);
        SetObj(stage4, "recoveryBodyText", shared.RecoveryBodyText);

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(WorkspaceRecoveryClipPath);
        if (clip != null)
            SetObj(stage4, "workspaceRecoveryClip", clip);
        else
            Warn("Workspace Recovery 알림음 클립을 찾지 못했습니다.");

        return stage4;
    }

    // ── 5단계: User Verification ─────────────────────────────────────────────
    private static StageFiveFlowController BuildStageFive(GameObject host, SharedRefs shared)
    {
        StageFiveFlowController stage5 = host.AddComponent<StageFiveFlowController>();

        RoomInteractable mirror = BuildMirror(shared);
        StageFiveCodeInputWindow inputWindow = BuildCodeInputWindow(stage5, out Text mirrorCodeText);

        SetObj(stage5, "mirrorInteractable", mirror);
        SetObj(stage5, "computerInteractable", shared.ComputerInteractable);
        SetObj(stage5, "codeInputWindow", inputWindow);
        SetObj(stage5, "mirrorCodeText", mirrorCodeText);
        SetObj(stage5, "objectiveText", shared.ObjectiveText);
        SetObj(stage5, "recoveryProgressText", shared.RecoveryProgressText);
        SetObj(stage5, "recoveryWindow", shared.RecoveryWindow);
        SetObj(stage5, "recoveryBodyText", shared.RecoveryBodyText);

        return stage5;
    }

    private static RoomInteractable BuildMirror(SharedRefs shared)
    {
        GameObject mirror = new GameObject("Stage5 Full-Body Mirror");
        if (shared.RoomRoot != null)
            mirror.transform.position = shared.RoomRoot.position + new Vector3(3f, 0f, 0f);

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MirrorSpritePath);
        SpriteRenderer renderer = mirror.AddComponent<SpriteRenderer>();
        if (sprite != null)
            renderer.sprite = sprite;
        else
            Warn($"MirrorPlayer 스프라이트를 찾지 못했습니다({MirrorSpritePath}).");

        BoxCollider2D collider = mirror.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2f, 3f);

        RoomInteractable interactable = mirror.AddComponent<RoomInteractable>();
        interactable.Configure("stage5_mirror", "[E] 전신거울 확인", false);
        // 조건 없이 통과하는 PuzzleAction. 리스트가 null이면 OnAction에서 NRE가 나므로 모두 비어 있게 채운다.
        interactable.puzzleAction = new PuzzleAction
        {
            Conditions = new List<GameCondition>(),
            NarrationID = new List<string>(),
            ChagingConditions = new List<GameCondition>(),
        };
        // 5단계 시작 전까지는 꺼둔다. StageFive.BeginStage가 켠다.
        interactable.enabled = false;
        collider.enabled = false;

        return interactable;
    }

    private static StageFiveCodeInputWindow BuildCodeInputWindow(StageFiveFlowController stage5, out Text mirrorCodeText)
    {
        // 독립 Overlay 캔버스에 코드 입력 창을 만든다.
        GameObject canvasGo = new GameObject("Stage5 Verification Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject panel = CreateUIChild(canvasGo.transform, "Panel", new Vector2(520f, 320f), Vector2.zero);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.07f, 0.10f, 0.96f);

        Text title = CreateText(panel.transform, "Title", "USER VERIFICATION", 26, new Vector2(0f, 120f), new Vector2(480f, 44f));
        Text feedback = CreateText(panel.transform, "Feedback", string.Empty, 20, new Vector2(0f, -60f), new Vector2(480f, 40f));
        Text hint = CreateText(panel.transform, "Hint", string.Empty, 16, new Vector2(0f, -110f), new Vector2(480f, 36f));

        // 거울에서 읽은 코드를 참고로 보여줄 텍스트(입력창 상단).
        mirrorCodeText = CreateText(panel.transform, "Mirror Code", string.Empty, 30, new Vector2(0f, 60f), new Vector2(480f, 48f));

        GameObject inputGo = CreateUIChild(panel.transform, "Code Input", new Vector2(320f, 48f), new Vector2(0f, 0f));
        Image inputBg = inputGo.AddComponent<Image>();
        inputBg.color = new Color(1f, 1f, 1f, 0.12f);
        InputField input = inputGo.AddComponent<InputField>();
        Text inputText = CreateText(inputGo.transform, "Text", string.Empty, 24, Vector2.zero, new Vector2(300f, 40f));
        Text placeholder = CreateText(inputGo.transform, "Placeholder", "인증코드 입력", 24, Vector2.zero, new Vector2(300f, 40f));
        placeholder.color = new Color(1f, 1f, 1f, 0.4f);
        input.textComponent = inputText;
        input.placeholder = placeholder;

        GameObject buttonGo = CreateUIChild(panel.transform, "Submit", new Vector2(160f, 44f), new Vector2(0f, -160f));
        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.5f, 0.9f, 1f);
        Button submit = buttonGo.AddComponent<Button>();
        CreateText(buttonGo.transform, "Text", "인증", 20, Vector2.zero, new Vector2(150f, 40f));

        StageFiveCodeInputWindow window = canvasGo.AddComponent<StageFiveCodeInputWindow>();
        SetObj(window, "codeInput", input);
        SetObj(window, "submitButton", submit);
        SetObj(window, "feedbackText", feedback);
        SetObj(window, "hintText", hint);
        SetObj(window, "flowController", stage5);

        // 이 텍스트들이 실제로 쓰인다는 것을 컴파일러/린터에 알린다.
        if (title == null)
            Warn("코드 입력 창 타이틀 생성 실패.");

        canvasGo.SetActive(false);
        return window;
    }

    // ── uGUI 헬퍼 ────────────────────────────────────────────────────────────
    private static GameObject CreateUIChild(Transform parent, string name, Vector2 size, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        return go;
    }

    private static Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = CreateUIChild(parent, name, size, anchoredPos);
        Text text = go.AddComponent<Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        // 최신 Unity의 기본 런타임 폰트. 없으면 폰트는 Unity에서 지정한다.
        Font builtin = AssetDatabase.GetBuiltinExtraResource<Font>("LegacyRuntime.ttf");
        if (builtin != null)
            text.font = builtin;
        return text;
    }

    // ── SerializedObject 유틸 (private [SerializeField]도 안전하게 세팅) ───────
    private static void SetObj(Component comp, string prop, Object value)
    {
        if (comp == null)
            return;
        SerializedObject so = new SerializedObject(comp);
        SerializedProperty p = so.FindProperty(prop);
        if (p == null)
        {
            Warn($"{comp.GetType().Name}.{prop} 필드를 찾지 못했습니다.");
            return;
        }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Object GetObj(Component comp, string prop)
    {
        if (comp == null)
            return null;
        SerializedObject so = new SerializedObject(comp);
        SerializedProperty p = so.FindProperty(prop);
        return p != null ? p.objectReferenceValue : null;
    }

    private static void SetBool(Component comp, string prop, bool value)
    {
        if (comp == null)
            return;
        SerializedObject so = new SerializedObject(comp);
        SerializedProperty p = so.FindProperty(prop);
        if (p == null)
            return;
        p.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void MarkDirtyAndSave(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, OutputScenePath);
        AssetDatabase.SaveAssets();
    }

    private static void Warn(string message)
    {
        Warnings.Add(message);
    }
}
#endif
