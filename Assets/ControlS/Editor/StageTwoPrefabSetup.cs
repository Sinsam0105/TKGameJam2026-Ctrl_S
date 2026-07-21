using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class StageTwoPrefabSetup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PrefabFolder = "Assets/ControlS/Resources/Pefabs/StageTwo";
    private const string PrefabPath = PrefabFolder + "/StageTwoSystem.prefab";
    private const string WhiteSpritePath = "Assets/ControlS/Resources/ControlS/WhitePixel.png";
    private const string AudioFolder = "Assets/ControlS/Resources/Audio/PrologueStage1/";
    private const string AutoRunKey = "ControlS.StageTwo.SerializedPrefab.0718.v2";

    static StageTwoPrefabSetup()
    {
        EditorApplication.delayCall += AutoConfigure;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += AutoConfigure;
    }

    private static void AutoConfigure()
    {
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += AutoConfigure;
            return;
        }

        // 플레이 중에는 셋업을 건너뛴다. 에디트 모드로 돌아오면
        // OnPlayModeStateChanged가 다시 등록해 준다.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (SessionState.GetBool(AutoRunKey, false))
            return;

        try
        {
            ConfigureStageTwoPrefabAndScene();
            SessionState.SetBool(AutoRunKey, true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Control S/Setup/Configure Stage 2 Prefab")]
    public static void ConfigureStageTwoPrefabAndScene()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EnsureFolder(PrefabFolder);
        BuildPrefab();
        PlacePrefabInScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[Control S] Stage 2 prefab and SampleScene connection completed.");
    }

    [MenuItem("Control S/Setup/Validate Stage 2 Prefab")]
    public static void ValidateStageTwoPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            throw new InvalidOperationException("StageTwoSystem.prefab is missing.");

        StageTwoFlowController flow = prefab.GetComponent<StageTwoFlowController>();
        StageTwoClueInteractable[] clues = prefab.GetComponentsInChildren<StageTwoClueInteractable>(true);
        FurnitureViewWindow[] clueViews = prefab.GetComponentsInChildren<FurnitureViewWindow>(true);
        StageTwoTimeInputWindow input = prefab.GetComponentInChildren<StageTwoTimeInputWindow>(true);
        int missingScripts = prefab.GetComponentsInChildren<Transform>(true)
            .Sum(child => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject));

        if (flow == null || clues.Length != 3 || clueViews.Length != 3 || input == null || missingScripts != 0)
            throw new InvalidOperationException("StageTwoSystem.prefab has an incomplete serialized hierarchy.");

        SerializedObject flowSerialized = new SerializedObject(flow);
        string[] requiredFlowReferences =
        {
            "microwaveClue", "postItClue", "outsideClockClue",
            "timeInputWindow", "effectsSource",
            "inputFailedClip", "inputSucceededClip",
        };
        if (requiredFlowReferences.Any(propertyName =>
                flowSerialized.FindProperty(propertyName)?.objectReferenceValue == null))
        {
            throw new InvalidOperationException("StageTwoSystem.prefab has a missing serialized reference.");
        }

        if (clues.Select(clue => clue.ClueType).Distinct().Count() != 3)
            throw new InvalidOperationException("StageTwoSystem.prefab must contain one interactable per clue type.");

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        StageTwoFlowController sceneFlow = FindComponent<StageTwoFlowController>(scene);
        StageOneFlowController stageOne = FindComponent<StageOneFlowController>(scene);
        bool connected = stageOne != null
                         && new SerializedObject(stageOne).FindProperty("stageTwoFlow")?.objectReferenceValue == sceneFlow;

        if (openedHere)
            EditorSceneManager.CloseScene(scene, true);

        if (sceneFlow == null || !connected)
            throw new InvalidOperationException("SampleScene does not reference the Stage 2 prefab instance.");

        Debug.Log("[Control S] Stage 2 prefab validation passed.");
    }

    private static void BuildPrefab()
    {
        Sprite whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        if (whiteSprite == null)
            throw new InvalidOperationException("WhitePixel sprite is missing.");

        GameObject root = new GameObject("StageTwoSystem");
        try
        {
            StageTwoFlowController flow = root.AddComponent<StageTwoFlowController>();
            AudioSource effects = root.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.loop = false;
            effects.spatialBlend = 0f;
            effects.volume = 0.8f;

            FurnitureViewWindow microwaveView = CreateClueView(root.transform,
                "Clue View - Microwave", "전자레인지",
                StageTwoFlowController.MicrowaveActionId, "디스플레이 확인");
            FurnitureViewWindow postItView = CreateClueView(root.transform,
                "Clue View - PostIt", "포스트잇",
                StageTwoFlowController.PostItActionId, "메모 읽기");
            FurnitureViewWindow outsideClockView = CreateClueView(root.transform,
                "Clue View - Outside Clock", "베란다 밖 디지털 시계",
                StageTwoFlowController.OutsideClockActionId, "시계 확인");

            StageTwoClueInteractable microwave = CreateClue(root.transform, whiteSprite,
                "Clue - Microwave 03-12", StageTwoClueType.Microwave,
                new Vector2(-4.7f, 1.28f), "microwave_stage2", "[E] 전자레인지 조사", microwaveView);
            StageTwoClueInteractable postIt = CreateClue(root.transform, whiteSprite,
                "Clue - PostIt", StageTwoClueType.PostIt,
                new Vector2(0.15f, 1.28f), "postit_stage2", "[E] 포스트잇 조사", postItView);
            StageTwoClueInteractable outsideClock = CreateClue(root.transform, whiteSprite,
                "Clue - Outside Clock 03-05", StageTwoClueType.OutsideClock,
                new Vector2(7.62f, 1.15f), "outside_clock_stage2", "[E] 외부 디지털 시계 조사", outsideClockView);
            StageTwoTimeInputWindow timeInputWindow = CreateTimeInputWindow(root.transform, flow);

            SerializedObject serialized = new SerializedObject(flow);
            SetReference(serialized, "microwaveClue", microwave);
            SetReference(serialized, "postItClue", postIt);
            SetReference(serialized, "outsideClockClue", outsideClock);
            SetReference(serialized, "timeInputWindow", timeInputWindow);

            SerializedProperty views = serialized.FindProperty("clueViews");
            FurnitureViewWindow[] viewObjects = { microwaveView, postItView, outsideClockView };
            views.arraySize = viewObjects.Length;
            for (int index = 0; index < viewObjects.Length; index++)
                views.GetArrayElementAtIndex(index).objectReferenceValue = viewObjects[index];
            SetReference(serialized, "effectsSource", effects);
            SetReference(serialized, "inputFailedClip", LoadAudio("PuzzleFail.mp3"));
            SetReference(serialized, "inputSucceededClip", LoadAudio("PuzzleSuccess.mp3"));

            SerializedProperty highlights = serialized.FindProperty("clueHighlights");
            GameObject[] highlightObjects =
            {
                microwave.transform.Find("Hint Highlight").gameObject,
                postIt.transform.Find("Hint Highlight").gameObject,
                outsideClock.transform.Find("Hint Highlight").gameObject,
            };
            highlights.arraySize = highlightObjects.Length;
            for (int index = 0; index < highlightObjects.Length; index++)
                highlights.GetArrayElementAtIndex(index).objectReferenceValue = highlightObjects[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static StageTwoClueInteractable CreateClue(Transform parent, Sprite whiteSprite, string name,
        StageTwoClueType clueType, Vector2 position, string interactionId, string prompt,
        FurnitureViewWindow clueView)
    {
        GameObject clueObject = new GameObject(name);
        clueObject.transform.SetParent(parent, false);
        clueObject.transform.localPosition = new Vector3(position.x, position.y, 0f);

        CircleCollider2D trigger = clueObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = clueType == StageTwoClueType.OutsideClock ? 0.72f : 0.9f;
        trigger.enabled = false;

        RoomInteractable roomInteractable = clueObject.AddComponent<RoomInteractable>();
        roomInteractable.Configure(interactionId, prompt, false);
        roomInteractable.puzzleAction = new PuzzleAction
        {
            Conditions = new List<GameCondition> { GameCondition.PrologueEnded },
            OpeningUI = clueView,
            NarrationID = new List<string>(),
            ChagingConditions = new List<GameCondition>(),
            CollectCollectionType = CollectionType.None,
            StartCollectionType = CollectionType.None,
        };
        roomInteractable.enabled = false;

        StageTwoClueInteractable clue = clueObject.AddComponent<StageTwoClueInteractable>();
        SerializedObject clueSerialized = new SerializedObject(clue);
        clueSerialized.FindProperty("clueType").enumValueIndex = (int)clueType;
        SetReference(clueSerialized, "roomInteractable", roomInteractable);
        SetReference(clueSerialized, "interactionTrigger", trigger);
        clueSerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject highlight = new GameObject("Hint Highlight");
        highlight.transform.SetParent(clueObject.transform, false);
        highlight.transform.localPosition = Vector3.zero;
        SpriteRenderer renderer = highlight.AddComponent<SpriteRenderer>();
        renderer.sprite = whiteSprite;
        renderer.color = new Color(1f, 0.78f, 0.08f, 0.42f);
        renderer.sortingOrder = 90;
        Vector2 targetSize = clueType == StageTwoClueType.OutsideClock
            ? new Vector2(1.2f, 1.2f)
            : new Vector2(1.55f, 1.15f);
        Vector2 spriteSize = whiteSprite.bounds.size;
        highlight.transform.localScale = new Vector3(
            targetSize.x / Mathf.Max(spriteSize.x, 0.01f),
            targetSize.y / Mathf.Max(spriteSize.y, 0.01f), 1f);
        highlight.SetActive(false);
        return clue;
    }

    /// <summary>
    /// 2단계 단서도 1단계 가구와 같은 정면샷 창으로 만든다. 창을 연 뒤 안에서 단서를 클릭하면 조사가 된다.
    /// 아트가 들어오기 전에는 흰 박스가 정면샷 자리를 대신한다.
    /// </summary>
    private static FurnitureViewWindow CreateClueView(Transform parent, string name, string title,
        string actionId, string actionLabel)
    {
        Image rootImage = CreateWindowRoot(parent, name, new Vector2(860f, 560f));
        rootImage.color = new Color(0f, 0f, 0f, 0.86f);

        Image background = CreateImage(rootImage.transform, "Clue Shot", new Vector2(0.08f, 0.16f),
            new Vector2(0.92f, 0.82f), new Color(0.92f, 0.92f, 0.94f, 1f));
        background.raycastTarget = false;

        Text titleText = CreateText(rootImage.transform, "Title", new Vector2(0.07f, 0.84f),
            new Vector2(0.78f, 0.96f), title, 34, TextAnchor.MiddleLeft, Color.white);
        Text hintText = CreateText(rootImage.transform, "Hint", new Vector2(0.07f, 0.04f),
            new Vector2(0.93f, 0.14f), "수상한 곳을 클릭해 살펴본다.", 22, TextAnchor.MiddleCenter,
            new Color(0.85f, 0.9f, 0.95f, 1f));
        Button close = CreateButton(rootImage.transform, "Close", new Vector2(0.82f, 0.86f),
            new Vector2(0.93f, 0.96f), "닫기", new Color(0.36f, 0.18f, 0.28f, 1f));

        Button action = CreateButton(rootImage.transform, $"Action - {actionId}",
            new Vector2(0.38f, 0.4f), new Vector2(0.62f, 0.58f),
            actionLabel, new Color(0.2f, 0.6f, 0.75f, 0.9f));

        FurnitureViewWindow window = rootImage.gameObject.AddComponent<FurnitureViewWindow>();
        window.EditorBind(background, titleText, hintText,
            new List<FurnitureViewWindow.PieceSlot>(),
            new List<FurnitureViewWindow.ActionSlot>
            {
                new FurnitureViewWindow.ActionSlot
                {
                    ActionId = actionId,
                    Button = action,
                    RequiredCondition = GameCondition.None,
                },
            });

        SerializedObject serialized = new SerializedObject(window);
        SetReference(serialized, "closeButton", close);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        rootImage.gameObject.SetActive(false);
        return window;
    }

    private static StageTwoTimeInputWindow CreateTimeInputWindow(Transform parent, StageTwoFlowController flow)
    {
        Image rootImage = CreateWindowRoot(parent, "Stage 2 Time Input Window", new Vector2(820f, 520f));
        StageTwoTimeInputWindow window = rootImage.gameObject.AddComponent<StageTwoTimeInputWindow>();

        CreateText(rootImage.transform, "Title", new Vector2(0.07f, 0.82f), new Vector2(0.75f, 0.95f),
            "SECURITY VERIFICATION", 32, TextAnchor.MiddleLeft, new Color(0.55f, 1f, 0.88f, 1f));
        CreateText(rootImage.transform, "Instruction", new Vector2(0.1f, 0.67f), new Vector2(0.9f, 0.8f),
            "마지막으로 유효했던 시간을 입력하세요.  (HH : MM)", 24, TextAnchor.MiddleCenter, Color.white);

        InputField hour = CreateInputField(rootImage.transform, "Hour Input", new Vector2(0.24f, 0.42f),
            new Vector2(0.43f, 0.64f), "HH");
        CreateText(rootImage.transform, "Colon", new Vector2(0.44f, 0.42f), new Vector2(0.56f, 0.64f),
            ":", 62, TextAnchor.MiddleCenter, Color.white);
        InputField minute = CreateInputField(rootImage.transform, "Minute Input", new Vector2(0.57f, 0.42f),
            new Vector2(0.76f, 0.64f), "MM");

        Text feedback = CreateText(rootImage.transform, "Feedback", new Vector2(0.09f, 0.27f),
            new Vector2(0.91f, 0.4f), string.Empty, 24, TextAnchor.MiddleCenter,
            new Color(1f, 0.56f, 0.48f, 1f));
        Text hint = CreateText(rootImage.transform, "Hint", new Vector2(0.09f, 0.15f),
            new Vector2(0.91f, 0.28f), string.Empty, 21, TextAnchor.MiddleCenter,
            new Color(1f, 0.84f, 0.28f, 1f));
        Button submit = CreateButton(rootImage.transform, "Submit", new Vector2(0.57f, 0.045f),
            new Vector2(0.78f, 0.15f), "VERIFY", new Color(0.08f, 0.52f, 0.46f, 1f));
        Button close = CreateButton(rootImage.transform, "Close", new Vector2(0.8f, 0.84f),
            new Vector2(0.93f, 0.94f), "닫기", new Color(0.36f, 0.18f, 0.28f, 1f));

        SerializedObject serialized = new SerializedObject(window);
        SetReference(serialized, "closeButton", close);
        SetReference(serialized, "hourInput", hour);
        SetReference(serialized, "minuteInput", minute);
        SetReference(serialized, "submitButton", submit);
        SetReference(serialized, "feedbackText", feedback);
        SetReference(serialized, "hintText", hint);
        SetReference(serialized, "flowController", flow);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        rootImage.gameObject.SetActive(false);
        return window;
    }

    private static Image CreateWindowRoot(Transform parent, string name, Vector2 size)
    {
        RectTransform rect = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.035f, 0.055f, 0.075f, 0.985f);
        image.raycastTarget = true;
        return image;
    }

    private static InputField CreateInputField(Transform parent, string name, Vector2 anchorMin,
        Vector2 anchorMax, string placeholderValue)
    {
        Image background = CreateImage(parent, name, anchorMin, anchorMax, new Color(0.08f, 0.1f, 0.13f, 1f));
        InputField input = background.gameObject.AddComponent<InputField>();
        input.targetGraphic = background;
        input.contentType = InputField.ContentType.IntegerNumber;
        input.characterLimit = 2;
        input.lineType = InputField.LineType.SingleLine;

        Text value = CreateText(background.transform, "Text", new Vector2(0.08f, 0.08f),
            new Vector2(0.92f, 0.92f), string.Empty, 56, TextAnchor.MiddleCenter, Color.white);
        value.raycastTarget = false;
        Text placeholder = CreateText(background.transform, "Placeholder", new Vector2(0.08f, 0.08f),
            new Vector2(0.92f, 0.92f), placeholderValue, 44, TextAnchor.MiddleCenter,
            new Color(1f, 1f, 1f, 0.2f));
        placeholder.fontStyle = FontStyle.Italic;
        input.textComponent = value;
        input.placeholder = placeholder;
        return input;
    }

    private static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        string value, int fontSize, TextAnchor alignment, Color color)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        string label, Color color)
    {
        Image image = CreateImage(parent, name, anchorMin, anchorMax, color);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        CreateText(image.transform, "Label", Vector2.zero, Vector2.one, label, 21,
            TextAnchor.MiddleCenter, Color.white);
        return button;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return rect;
    }

    private static void PlacePrefabInScene()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        GameObject existing = FindByName(scene, "Stage 2 System");
        if (existing != null)
            Object.DestroyImmediate(existing);

        foreach (string obsoleteName in new[]
                 {
                     "Microwave Investigation Marker", "Outside Clock Investigation Marker",
                     "Microwave (Stage 2 Preview)", "Outside Clock (Stage 2 Preview)",
                 })
        {
            GameObject obsolete = FindByName(scene, obsoleteName);
            if (obsolete != null)
                Object.DestroyImmediate(obsolete);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "Stage 2 System";

        StageTwoFlowController stageTwoFlow = instance.GetComponent<StageTwoFlowController>();
        StageOneFlowController stageOneFlow = FindComponent<StageOneFlowController>(scene);
        if (stageOneFlow == null)
            throw new InvalidOperationException("SampleScene is missing StageOneFlowController.");

        SerializedObject stageOneSerialized = new SerializedObject(stageOneFlow);
        SetReference(stageOneSerialized, "stageTwoFlow", stageTwoFlow);
        SerializedProperty oldMarkers = stageOneSerialized.FindProperty("stageTwoInvestigationMarkers");
        if (oldMarkers != null)
            oldMarkers.arraySize = 0;
        stageOneSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ValidateStageTwoPrefab();

        if (openedHere)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static AudioClip LoadAudio(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + fileName);
    }

    private static void SetReference(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {serialized.targetObject.name}.");
        property.objectReferenceValue = value;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }

    private static GameObject FindByName(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
            if (found != null)
                return found.gameObject;
        }
        return null;
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .FirstOrDefault();
    }
}
