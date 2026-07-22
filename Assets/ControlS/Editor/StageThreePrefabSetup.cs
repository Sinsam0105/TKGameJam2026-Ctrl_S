using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// 3단계 Version History Recovery의 씬/프리팹을 흰 박스로 셋업한다.
/// 정렬 퍼즐·문자 잠금·USB 정면샷·조사 지점을 만들고 StageThreeFlowController에 바인딩한다.
/// Version 07·현관 노크 공포 연출 프리팹은 정민 선배 몫이라, 있으면 심고 없으면 빈 자리만 둔다.
/// </summary>
public static class StageThreePrefabSetup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PrefabFolder = "Assets/ControlS/Resources/Pefabs/StageThree";
    private const string PrefabPath = PrefabFolder + "/StageThreeSystem.prefab";
    private const string Version07PrefabPath = PrefabFolder + "/Version07Direction.prefab";
    private const string KnockPrefabPath = PrefabFolder + "/KnockDirection.prefab";

    // 정답 순서(1~6) = D B F A E C
    private static readonly (string id, int order, string label)[] CardData =
    {
        ("D", 1, "제목"),
        ("B", 2, "제목 + 빈 사진 프레임"),
        ("F", 3, "제목 + 원본 사진"),
        ("A", 4, "사진 확대·크롭"),
        ("E", 5, "부제 추가"),
        ("C", 6, "페이지번호 + 효과"),
    };

    [MenuItem("Control S/Setup/Configure Stage 3")]
    public static void ConfigureStageThree()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EnsureFolder(PrefabFolder);
        GameObject prefab = BuildPrefab();
        PlacePrefabInScene(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log("[Control S] Stage 3 prefab and SampleScene connection completed.");
    }

    private static GameObject BuildPrefab()
    {
        GameObject root = new GameObject("StageThreeSystem");
        try
        {
            StageThreeFlowController flow = root.AddComponent<StageThreeFlowController>();
            AudioSource sfx = root.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            // 창들을 담을 자체 Overlay 캔버스.
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 92;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            VersionChainPuzzle puzzle = CreateVersionPuzzleWindow(root.transform, sfx);
            LetterLockWindow letterLock = CreateLetterLockWindow(root.transform, sfx);
            FurnitureViewWindow usbView = CreateUsbView(root.transform);

            // 월드 조사 지점 (컴퓨터 옆 상자, USB). 캔버스 밖.
            // 조건을 단계별로 걸어 순서를 강제한다. (컴퓨터→정렬→상자→USB)
            RoomInteractable computerSpot = CreateSpot(root.transform, "Interact - Version History",
                new Vector2(3.45f, 1.45f), "stage3_computer", "[E] 버전 기록 열기", null,
                GameCondition.Stage2TimeSolved);
            RoomInteractable boxSpot = CreateSpot(root.transform, "Interact - Backup Box",
                new Vector2(2.1f, 0.4f), "stage3_box", "[E] 백업 상자", letterLock,
                GameCondition.Stage3VersionSolved);
            RoomInteractable usbSpot = CreateSpot(root.transform, "Interact - USB",
                new Vector2(2.7f, 0.4f), "stage3_usb", "[E] USB 살펴보기", usbView,
                GameCondition.Stage3BoxOpened);
            // 현관 조사 지점. 노크(Stage3KnockHeard) 후에만 열린다. UI 없이 문 개방 이벤트만 발생시킨다.
            RoomInteractable frontDoorSpot = CreateSpot(root.transform, "Interact - Front Door",
                new Vector2(-6.0f, -3.3f), "stage3_frontdoor", "[E] 현관 확인", null,
                GameCondition.Stage3KnockHeard);

            // 컴퓨터 조사는 정렬 퍼즐 창을 연다. (창은 PuzzleWindowedUI)
            PuzzleWindowedUI puzzleWindow = puzzle.GetComponent<PuzzleWindowedUI>();
            computerSpot.puzzleAction.OpeningUI = puzzleWindow;

            // 공포 연출 프리팹 (선배 몫). 없으면 빈 placeholder.
            GameObject version07 = InstantiateOrPlaceholder(root.transform, Version07PrefabPath,
                "Version07 Direction (선배)", new Vector2(3.45f, 1.45f));
            GameObject knock = InstantiateOrPlaceholder(root.transform, KnockPrefabPath,
                "Knock Direction (선배)", new Vector2(-5.5f, -3.3f));
            BindFrontDoorSprites(knock);

            SerializedObject so = new SerializedObject(flow);
            SetReference(so, "versionPuzzle", puzzle);
            SetReference(so, "letterLock", letterLock);
            SetReference(so, "usbView", usbView);
            SetReference(so, "computerInteractable", computerSpot);
            SetReference(so, "backupBoxInteractable", boxSpot);
            SetReference(so, "usbInteractable", usbSpot);
            SetReference(so, "frontDoorInteractable", frontDoorSpot);
            SetReference(so, "version07Direction", version07);
            SetReference(so, "knockDirection", knock);
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return saved;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ── 정렬 퍼즐 창 ────────────────────────────────────────────────
    private static VersionChainPuzzle CreateVersionPuzzleWindow(Transform parent, AudioSource sfx)
    {
        Image window = CreateWindowRoot(parent, "Version Puzzle Window", new Vector2(1180f, 680f));
        window.gameObject.AddComponent<PuzzleWindowedUI>();
        VersionChainPuzzle puzzle = window.gameObject.AddComponent<VersionChainPuzzle>();

        CreateText(window.transform, "Title", new Vector2(0.04f, 0.9f), new Vector2(0.7f, 0.98f),
            "버전 기록 정렬 — 편집이 일어난 순서대로 놓으세요", 26, TextAnchor.MiddleLeft, Color.white);

        // 6개 슬롯 (가로)
        List<RectTransform> slots = new List<RectTransform>();
        for (int i = 0; i < 6; i++)
        {
            float x0 = 0.04f + i * 0.155f;
            RectTransform slot = CreateRect(window.transform, $"Slot {i + 1}",
                new Vector2(x0, 0.5f), new Vector2(x0 + 0.14f, 0.86f));
            Image slotBg = slot.gameObject.AddComponent<Image>();
            slotBg.color = new Color(1f, 1f, 1f, 0.06f);
            slotBg.raycastTarget = false;
            CreateText(slot, "SlotNo", new Vector2(0f, -0.16f), new Vector2(1f, 0f),
                (i + 1).ToString(), 20, TextAnchor.MiddleCenter, new Color(0.6f, 0.7f, 0.8f, 1f));
            slots.Add(slot);
        }

        // 6개 카드 (창 아래쪽에 임시 생성, ResetPuzzle이 슬롯에 배치)
        List<VersionCard> cards = new List<VersionCard>();
        for (int i = 0; i < CardData.Length; i++)
        {
            var data = CardData[i];
            RectTransform cardRect = CreateRect(window.transform, $"Card {data.id}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            cardRect.sizeDelta = new Vector2(150f, 190f);
            Image cardBg = cardRect.gameObject.AddComponent<Image>();
            cardBg.color = new Color(0.9f, 0.88f, 0.8f, 1f);
            cardRect.gameObject.AddComponent<CanvasGroup>();

            CreateText(cardRect, "FileId", new Vector2(0f, 0.82f), new Vector2(1f, 1f),
                $"파일 {data.id}", 18, TextAnchor.MiddleCenter, new Color(0.25f, 0.22f, 0.18f, 1f));
            Text label = CreateText(cardRect, "Label", new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.8f),
                data.label, 16, TextAnchor.MiddleCenter, new Color(0.15f, 0.13f, 0.1f, 1f));

            Image selFrame = CreateImage(cardRect, "SelectionFrame", Vector2.zero, Vector2.one,
                new Color(0.3f, 0.7f, 1f, 0.35f));
            selFrame.raycastTarget = false;
            selFrame.enabled = false;
            Image hlFrame = CreateImage(cardRect, "HighlightFrame", Vector2.zero, Vector2.one,
                new Color(1f, 0.8f, 0.1f, 0.4f));
            hlFrame.raycastTarget = false;
            hlFrame.enabled = false;

            VersionCard card = cardRect.gameObject.AddComponent<VersionCard>();
            SerializedObject cardSo = new SerializedObject(card);
            SetReference(cardSo, "labelText", label);
            SetReference(cardSo, "selectionFrame", selFrame);
            SetReference(cardSo, "highlightFrame", hlFrame);
            cardSo.FindProperty("fileId").stringValue = data.id;
            cardSo.FindProperty("answerIndex").intValue = data.order;
            cardSo.ApplyModifiedPropertiesWithoutUndo();
            cards.Add(card);
        }

        Text compareResult = CreateText(window.transform, "CompareResult",
            new Vector2(0.04f, 0.32f), new Vector2(0.6f, 0.42f),
            string.Empty, 20, TextAnchor.MiddleLeft, new Color(0.5f, 0.9f, 1f, 1f));
        Text hint = CreateText(window.transform, "Hint",
            new Vector2(0.04f, 0.2f), new Vector2(0.96f, 0.3f),
            string.Empty, 18, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.5f, 1f));
        Text code = CreateText(window.transform, "Code",
            new Vector2(0.04f, 0.08f), new Vector2(0.6f, 0.18f),
            string.Empty, 24, TextAnchor.MiddleLeft, new Color(0.5f, 1f, 0.7f, 1f));

        Button compareBtn = CreateButton(window.transform, "Compare",
            new Vector2(0.64f, 0.32f), new Vector2(0.8f, 0.42f), "COMPARE", new Color(0.2f, 0.4f, 0.6f, 1f));
        Button submitBtn = CreateButton(window.transform, "Submit",
            new Vector2(0.82f, 0.08f), new Vector2(0.96f, 0.2f), "정렬 확인", new Color(0.2f, 0.55f, 0.4f, 1f));
        Button closeBtn = CreateButton(window.transform, "Close",
            new Vector2(0.9f, 0.9f), new Vector2(0.98f, 0.98f), "X", new Color(0.5f, 0.2f, 0.28f, 1f));

        SerializedObject so = new SerializedObject(puzzle);
        SetObjectList(so, "slots", slots.Cast<Object>());
        SetObjectList(so, "cards", cards.Cast<Object>());
        SetReference(so, "compareButton", compareBtn);
        SetReference(so, "submitButton", submitBtn);
        SetReference(so, "compareResultText", compareResult);
        SetReference(so, "hintText", hint);
        SetReference(so, "codeText", code);
        SetReference(so, "sfxSource", sfx);
        so.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject winSo = new SerializedObject(window.GetComponent<PuzzleWindowedUI>());
        SetReference(winSo, "closeButton", closeBtn);
        winSo.ApplyModifiedPropertiesWithoutUndo();

        // 카드끼리 겹치지 않게 초기 위치를 흩어둔다 (ResetPuzzle이 다시 슬롯에 배치).
        for (int i = 0; i < cards.Count; i++)
            cards[i].Rect.anchoredPosition = new Vector2(-500f + i * 200f, -260f);

        window.gameObject.SetActive(false);
        return puzzle;
    }

    // ── 문자 잠금 창 ────────────────────────────────────────────────
    private static LetterLockWindow CreateLetterLockWindow(Transform parent, AudioSource sfx)
    {
        Image window = CreateWindowRoot(parent, "Letter Lock Window", new Vector2(560f, 520f));
        LetterLockWindow lockWindow = window.gameObject.AddComponent<LetterLockWindow>();

        CreateText(window.transform, "Title", new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.96f),
            "백업 상자 — 접근 코드 입력", 26, TextAnchor.MiddleCenter, Color.white);
        Text entry = CreateText(window.transform, "Entry", new Vector2(0.1f, 0.68f), new Vector2(0.9f, 0.82f),
            string.Empty, 40, TextAnchor.MiddleCenter, new Color(0.5f, 1f, 0.8f, 1f));
        Text feedback = CreateText(window.transform, "Feedback", new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.66f),
            string.Empty, 18, TextAnchor.MiddleCenter, new Color(1f, 0.7f, 0.5f, 1f));

        // A~F 버튼 (2행 3열)
        string[] letters = { "A", "B", "C", "D", "E", "F" };
        List<Button> letterButtons = new List<Button>();
        for (int i = 0; i < 6; i++)
        {
            int col = i % 3;
            int row = i / 3;
            float x0 = 0.14f + col * 0.26f;
            float y1 = 0.46f - row * 0.18f;
            Button b = CreateButton(window.transform, $"Letter {letters[i]}",
                new Vector2(x0, y1 - 0.14f), new Vector2(x0 + 0.2f, y1),
                letters[i], new Color(0.2f, 0.28f, 0.36f, 1f));
            letterButtons.Add(b);
        }

        Button clear = CreateButton(window.transform, "Clear",
            new Vector2(0.14f, 0.04f), new Vector2(0.44f, 0.13f), "지우기", new Color(0.4f, 0.3f, 0.2f, 1f));
        Button close = CreateButton(window.transform, "Close",
            new Vector2(0.56f, 0.04f), new Vector2(0.86f, 0.13f), "닫기", new Color(0.5f, 0.2f, 0.28f, 1f));

        SerializedObject so = new SerializedObject(lockWindow);
        SetObjectList(so, "letterButtons", letterButtons.Cast<Object>());
        SetReference(so, "entryText", entry);
        SetReference(so, "feedbackText", feedback);
        SetReference(so, "clearButton", clear);
        SetReference(so, "closeButton", close);
        SetReference(so, "sfxSource", sfx);
        so.ApplyModifiedPropertiesWithoutUndo();

        window.gameObject.SetActive(false);
        return lockWindow;
    }

    // ── USB 정면샷 ──────────────────────────────────────────────────
    private static FurnitureViewWindow CreateUsbView(Transform parent)
    {
        Image window = CreateWindowRoot(parent, "USB View", new Vector2(860f, 560f));
        window.color = new Color(0f, 0f, 0f, 0.86f);

        Image bg = CreateImage(window.transform, "USB Shot", new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.82f),
            new Color(0.9f, 0.9f, 0.92f, 1f));
        bg.raycastTarget = false;
        CreateText(window.transform, "Title", new Vector2(0.08f, 0.85f), new Vector2(0.7f, 0.96f),
            "백업 상자 안", 26, TextAnchor.MiddleLeft, Color.white);
        Text hint = CreateText(window.transform, "Hint", new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.15f),
            "USB와 메모가 들어 있다.", 18, TextAnchor.MiddleCenter, new Color(0.85f, 0.9f, 0.95f, 1f));

        Button connect = CreateButton(window.transform, "Action - usb_connect",
            new Vector2(0.4f, 0.42f), new Vector2(0.6f, 0.56f), "USB 연결", new Color(0.2f, 0.6f, 0.5f, 1f));
        Button close = CreateButton(window.transform, "Close",
            new Vector2(0.85f, 0.86f), new Vector2(0.94f, 0.96f), "닫기", new Color(0.5f, 0.2f, 0.28f, 1f));

        FurnitureViewWindow view = window.gameObject.AddComponent<FurnitureViewWindow>();
        view.EditorBind(bg, null, hint, new List<FurnitureViewWindow.PieceSlot>(),
            new List<FurnitureViewWindow.ActionSlot>
            {
                new FurnitureViewWindow.ActionSlot { ActionId = "usb_connect", Button = connect, RequiredCondition = GameCondition.Stage3BoxOpened },
            });

        SerializedObject so = new SerializedObject(view);
        SetReference(so, "closeButton", close);
        so.ApplyModifiedPropertiesWithoutUndo();

        window.gameObject.SetActive(false);
        return view;
    }

    // ── 월드 조사 지점 ──────────────────────────────────────────────
    private static RoomInteractable CreateSpot(Transform parent, string name, Vector2 pos,
        string id, string prompt, BaseWindowedUI openingUI, GameCondition condition)
    {
        GameObject spot = new GameObject(name);
        spot.transform.SetParent(parent, false);
        spot.transform.localPosition = new Vector3(pos.x, pos.y, 0f);

        CircleCollider2D trigger = spot.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.7f;
        trigger.enabled = false;

        RoomInteractable interactable = spot.AddComponent<RoomInteractable>();
        interactable.Configure(id, prompt, false);
        interactable.puzzleAction = new PuzzleAction
        {
            Conditions = new List<GameCondition> { condition },
            OpeningUI = openingUI,
            NarrationID = new List<string>(),
            ChagingConditions = new List<GameCondition>(),
            CollectCollectionType = CollectionType.None,
            StartCollectionType = CollectionType.None,
        };
        interactable.enabled = false;
        return interactable;
    }

    // 선배 현관 연출(FrontDoorDirection)에 현관 배경 3종을 꽂는다. 선배 코드는 필드만 늘렸고,
    // 배경 SpriteRenderer가 없으면 여기서 자식으로 만들어 붙인다.
    private static void BindFrontDoorSprites(GameObject knock)
    {
        if (knock == null)
            return;
        FrontDoorDirection door = knock.GetComponent<FrontDoorDirection>();
        if (door == null)
            return; // 아직 프리팹 추출 전(placeholder)

        // 배경 렌더러 확보. 이름이 "Door Background"인 자식을 재사용하거나 새로 만든다.
        Transform bgTransform = knock.transform.Cast<Transform>()
            .FirstOrDefault(t => t.name == "Door Background");
        SpriteRenderer renderer;
        if (bgTransform != null)
        {
            renderer = bgTransform.GetComponent<SpriteRenderer>();
        }
        else
        {
            GameObject bg = new GameObject("Door Background", typeof(SpriteRenderer));
            bg.transform.SetParent(knock.transform, false);
            renderer = bg.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = -20; // 인터폰 노이즈보다 뒤
        }

        SerializedObject so = new SerializedObject(door);
        SetOptional(so, "_doorRenderer", renderer);
        SetOptional(so, "_normalSprite", LoadArtSprite("FrontDoor_Normal.png"));
        SetOptional(so, "_horrorSprite", LoadArtSprite("FrontDoor_Horror.png"));
        SetOptional(so, "_corridorSprite", LoadArtSprite("Corridor.png"));
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 필드가 있으면 할당, 없으면 조용히 넘어간다(선배 코드 버전 차이 대비).
    private static void SetOptional(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty p = so.FindProperty(propertyName);
        if (p != null)
            p.objectReferenceValue = value;
    }

    // Arts 폴더의 png를 단일 스프라이트로 임포트하고 로드한다.
    private static Sprite LoadArtSprite(string fileName)
    {
        string path = "Assets/ControlS/Resources/Arts/" + fileName;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[Control S] 아트가 아직 없다: {fileName}");
            return null;
        }
        if (importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject InstantiateOrPlaceholder(Transform parent, string prefabPath, string name, Vector2 pos)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
        }
        else
        {
            Debug.LogWarning($"[Control S] 연출 프리팹이 아직 없다: {prefabPath}. 빈 자리로 둔다.");
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
        }
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.SetActive(false);
        return go;
    }

    // ── 씬 배치 + 연결 ──────────────────────────────────────────────
    private static void PlacePrefabInScene(GameObject prefab)
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        GameObject existing = FindByName(scene, "Stage 3 System");
        if (existing != null)
            Object.DestroyImmediate(existing);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "Stage 3 System";

        StageThreeFlowController stageThree = instance.GetComponent<StageThreeFlowController>();
        StageTwoFlowController stageTwo = FindComponent<StageTwoFlowController>(scene);
        if (stageTwo != null)
        {
            SerializedObject twoSo = new SerializedObject(stageTwo);
            SetReference(twoSo, "stageThreeFlow", stageThree);
            twoSo.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[Control S] 씬에 StageTwoFlowController가 없어 3단계 연결을 건너뛴다.");
        }

        // 공유 HUD 텍스트 연결 (있으면).
        StageOneFlowController stageOne = FindComponent<StageOneFlowController>(scene);
        if (stageOne != null)
        {
            SerializedObject oneSo = new SerializedObject(stageOne);
            SerializedObject threeSo = new SerializedObject(stageThree);
            BindShared(threeSo, "objectiveText", oneSo, "objectiveText");
            BindShared(threeSo, "recoveryProgressText", oneSo, "recoveryProgressText");
            BindShared(threeSo, "recoveryWindow", oneSo, "recoveryWindow");
            BindShared(threeSo, "recoveryBodyText", oneSo, "recoveryBodyText");
            threeSo.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (openedHere)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static void BindShared(SerializedObject target, string targetProp, SerializedObject source, string sourceProp)
    {
        SerializedProperty tp = target.FindProperty(targetProp);
        SerializedProperty sp = source.FindProperty(sourceProp);
        if (tp != null && sp != null)
            tp.objectReferenceValue = sp.objectReferenceValue;
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────
    private static Font _koreanFont;
    private static Font KoreanFont
    {
        get
        {
            if (_koreanFont != null)
                return _koreanFont;
            foreach (string n in new[] { "Malgun Gothic", "맑은 고딕", "NanumGothic", "Gulim", "Dotum", "AppleGothic" })
            {
                Font f = Font.CreateDynamicFontFromOSFont(n, 32);
                if (f != null) { _koreanFont = f; return f; }
            }
            _koreanFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _koreanFont;
        }
    }

    private static Image CreateWindowRoot(Transform parent, string name, Vector2 size)
    {
        RectTransform rect = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.035f, 0.055f, 0.075f, 0.985f);
        return image;
    }

    private static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        string value, int fontSize, TextAnchor alignment, Color color)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = KoreanFont;
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
        CreateText(image.transform, "Label", Vector2.zero, Vector2.one, label, 20,
            TextAnchor.MiddleCenter, Color.white);
        return button;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return rect;
    }

    private static void SetReference(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {serialized.targetObject.name}.");
        property.objectReferenceValue = value;
    }

    private static void SetObjectList(SerializedObject serialized, string propertyName, IEnumerable<Object> values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"Serialized list '{propertyName}' was not found on {serialized.targetObject.name}.");
        Object[] array = values.ToArray();
        property.arraySize = array.Length;
        for (int i = 0; i < array.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static GameObject FindByName(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
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
