using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PrologueStageOneSetup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PhotoPath = "Assets/ControlS/Resources/Arts/PrologueStage1/Stage1Photo.png";
    private const string RoomBackgroundPath = "Assets/ControlS/Resources/Arts/PrologueStage1/RoomBackground.png";
    private const string PlayerFrontBackPath = "Assets/ControlS/Resources/Arts/PlayerFrontBack.png";
    private const string PlayerPrefabPath = "Assets/ControlS/Resources/Pefabs/Player.prefab";
    private const string PlayerLightPrefabPath = "Assets/ControlS/Resources/Pefabs/PlayerLight.prefab";
    private const string LightPrefabPath = "Assets/ControlS/Resources/Pefabs/Light.prefab";
    private const string PlayerAnimationDirectory = "Assets/ControlS/Resources/Animations/Player/";
    private const string PicturePrefabPath = "Assets/ControlS/Resources/Pefabs/PictureSystem/PictureInteractable.prefab";
    private const string PuzzlePrefabPath = "Assets/ControlS/Resources/Pefabs/PictureSystem/PicturePuzzleCanvas.prefab";
    private const string CollectionHudPrefabPath = "Assets/ControlS/Resources/Pefabs/PictureSystem/PictureCollectionHUD.prefab";
    private const string VirtualDesktopPrefabPath = "Assets/ControlS/Resources/Pefabs/UI/Virtual Desktop System.prefab";
    private const string DesktopSpeakerPath = "Assets/ControlS/Resources/Arts/PrologueStage1/DesktopSpeakerReference.jpg";
    private const string InputActionsGuid = "2bcd2660ca9b64942af0de543d8d7100";
    private const string AutoRunKey = "ControlS.StageOne.SerializedSetup.0718.v22";

    private static readonly string AudioDirectory = "Assets/ControlS/Resources/Audio/PrologueStage1/";

    // 사진 조립 퍼즐 격자. 원본 사진이 가로로 길어서 4열 x 3행이다.
    private const int PuzzleColumns = 4;
    private const int PuzzleRows = 3;

    // 책상 정면샷의 스캐너 버튼 id. StageOneFlowController가 같은 값을 본다.
    private const string ScannerActionId = "scanner";
    private const string ComputerActionId = "computer";

    // CreatePhotoPieces가 만든 정면샷 창들. 플로우 컨트롤러에 꽂기 위해 들고 있는다.
    private static readonly List<FurnitureViewWindow> CreatedFurnitureViews = new List<FurnitureViewWindow>();

    static PrologueStageOneSetup()
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
            return;
        // 플레이 중에는 셋업을 건너뛴다. 에디트 모드로 돌아오면
        // OnPlayModeStateChanged가 다시 등록해 준다.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (SessionState.GetBool(AutoRunKey, false))
            return;

        SessionState.SetBool(AutoRunKey, true);
        ConfigureProject();
    }

    [MenuItem("Control S/Setup/Configure Prologue and Stage 1")]
    public static void ConfigureProject()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigurePlayerLightPrefab();
            Dictionary<string, Sprite> directionalSprites = ConfigurePlayerDirectionalSprites();
            ConfigurePlayerDirectionalAnimations(directionalSprites);
            Dictionary<string, Sprite> photoSprites = ConfigurePhotoSprites();
            Sprite roomBackground = ConfigureRoomBackgroundSprite();
            ConfigurePictureInteractablePrefab(photoSprites);
            ConfigurePicturePuzzlePrefab(photoSprites);
            ConfigureVirtualDesktopPrefab();
            ConfigureSampleScene(photoSprites, roomBackground);
            EnsureBuildScene();
            AssetDatabase.SaveAssets();
            ValidateProject();
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isDirty && activeScene.path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[Control S] Prologue through Stage 1 serialized setup completed.");
        }
        catch (Exception exception)
        {
            SessionState.SetBool(AutoRunKey, false);
            Debug.LogException(exception);
            throw;
        }
    }

    [MenuItem("Control S/Setup/Validate Prologue and Stage 1")]
    public static void ValidateProject()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        int missingScripts = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
        }

        StageOneFlowController flow = FindComponent<StageOneFlowController>(scene);
        GameObject desktop = FindByName(scene, "Desktop");
        GameObject desktopCanvas = FindByName(scene, "Virtual Desktop Canvas");
        ComputerWindowedUI desktopWindow = desktop != null ? desktop.GetComponent<ComputerWindowedUI>() : null;
        bool desktopReady = desktop != null
                            && desktop.activeSelf
                            && desktopCanvas != null
                            && desktopCanvas.activeSelf
                            && desktopCanvas.transform.localScale.sqrMagnitude > 0.1f
                            && desktopWindow != null;
        SerializedObject desktopSerialized = desktopWindow != null ? new SerializedObject(desktopWindow) : null;
        GameObject desktopSpeaker = desktopSerialized?.FindProperty("desktopSpeaker")?.objectReferenceValue as GameObject;
        RawImage speakerPortrait = desktopSpeaker != null
            ? desktopSpeaker.GetComponentInChildren<RawImage>(true)
            : null;
        bool desktopSpeakerReady = desktopSpeaker != null
                                   && speakerPortrait != null
                                   && AssetDatabase.GetAssetPath(speakerPortrait.texture) == DesktopSpeakerPath;
        bool desktopFlowAssigned = flow != null
                                   && new SerializedObject(flow).FindProperty("computerDesktop")?.objectReferenceValue == desktopWindow;
        PlayerMove player = FindComponent<PlayerMove>(scene);
        SpriteRenderer playerRenderer = player != null ? player.GetComponent<SpriteRenderer>() : null;
        PlayerLightController playerLightController = player != null
            ? player.GetComponentInChildren<PlayerLightController>(true)
            : null;
        Light2D playerLight = playerLightController != null ? playerLightController.GetComponent<Light2D>() : null;
        bool directionalAnimationsReady = playerRenderer != null
                                           && playerRenderer.sprite != null
                                           && playerRenderer.sprite.name == "PlayerFrontIdle"
                                           && ClipUsesPlayerSprites(PlayerAnimationDirectory + "IdleDown.anim", 1)
                                           && ClipUsesPlayerSprites(PlayerAnimationDirectory + "WalkDown.anim", 3)
                                           && ClipUsesPlayerSprites(PlayerAnimationDirectory + "IdleUp.anim", 1)
                                           && ClipUsesPlayerSprites(PlayerAnimationDirectory + "WalkUp.anim", 3);
        bool playerPresentationReady = player != null
                                       && Mathf.Approximately(player.transform.localScale.x, 0.95f)
                                       && playerLight != null
                                       && Mathf.Approximately(playerLight.pointLightOuterRadius, 3.2f);
        GameObject computer = FindByName(scene, "Computer");
        LightController computerScreenLight = computer != null
            ? computer.GetComponentInChildren<LightController>(true)
            : null;
        Light2D computerLight = computerScreenLight != null ? computerScreenLight.GetComponent<Light2D>() : null;
        bool computerLightReady = computerScreenLight != null
                                  && computerScreenLight.gameObject.name == "Computer Screen Light"
                                  && computerLight != null
                                  && computerLight.pointLightOuterRadius <= 1.5f;
        bool computerLightAssigned = flow != null
                                     && new SerializedObject(flow).FindProperty("computerScreenLight")?.objectReferenceValue
                                        == computerScreenLight;
        RoomInteractable[] interactables = FindComponents<RoomInteractable>(scene);
        int photoCount = interactables.Count(item => item.InteractionId.StartsWith("bed", StringComparison.OrdinalIgnoreCase)
                                                    || item.InteractionId.StartsWith("drawer", StringComparison.OrdinalIgnoreCase)
                                                    || item.InteractionId.StartsWith("book", StringComparison.OrdinalIgnoreCase)
                                                    || item.InteractionId.StartsWith("cup", StringComparison.OrdinalIgnoreCase)
                                                    || item.InteractionId.StartsWith("frame", StringComparison.OrdinalIgnoreCase)
                                                    || item.InteractionId.StartsWith("computer_piece", StringComparison.OrdinalIgnoreCase)
                                                    || item.InteractionId.StartsWith("balcony", StringComparison.OrdinalIgnoreCase)
                                                    || item.InteractionId.StartsWith("floor", StringComparison.OrdinalIgnoreCase)
                                                    || item.InteractionId.StartsWith("bed_top", StringComparison.OrdinalIgnoreCase));
        bool hasLegacySceneRoot = FindByName(scene, "CONTROL S - Scene Root") != null;

        if (flow == null || photoCount != 12 || missingScripts != 0 || hasLegacySceneRoot || !desktopReady || !desktopSpeakerReady
            || !desktopFlowAssigned
            || !directionalAnimationsReady || !playerPresentationReady || !computerLightReady || !computerLightAssigned)
            Debug.LogError($"[Control S] Validation failed. Flow={flow != null}, Photo pieces={photoCount}/12, " +
                           $"Desktop ready={desktopReady}, Desktop speaker={desktopSpeakerReady}, " +
                           $"Desktop flow assigned={desktopFlowAssigned}, " +
                           $"Directional player art={directionalAnimationsReady}, Player scale/light={playerPresentationReady}, " +
                           $"Computer light={computerLightReady}, Computer light assigned={computerLightAssigned}, " +
                           $"Legacy scene root={hasLegacySceneRoot}, Missing scripts={missingScripts}");
        else
            Debug.Log("[Control S] Validation passed. Flow, ComputerWindow desktop, and seated speaker serialized, " +
                      "player scale/lights configured, front/back player art linked, " +
                      "12 unique photo pickups, no missing scripts.");

        if (openedHere)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static Dictionary<string, Sprite> ConfigurePhotoSprites()
    {
        TextureImporter importer = AssetImporter.GetAtPath(PhotoPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"Stage 1 photo was not imported: {PhotoPath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PhotoPath);
        if (texture == null)
            throw new InvalidOperationException("Unable to read Stage 1 photo dimensions.");

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        List<SpriteRect> rects = new List<SpriteRect>
        {
            NewSpriteRect("Stage1Photo_Full", new Rect(0, 0, texture.width, texture.height))
        };

        // 원본이 가로로 긴 사진이라 4열 x 3행으로 잘라야 조각이 정사각에 가깝다.
        for (int row = 0; row < PuzzleRows; row++)
        {
            int top = Mathf.RoundToInt(texture.height * (1f - (float)row / PuzzleRows));
            int bottom = Mathf.RoundToInt(texture.height * (1f - (row + 1f) / PuzzleRows));
            for (int column = 0; column < PuzzleColumns; column++)
            {
                int left = Mathf.RoundToInt(texture.width * ((float)column / PuzzleColumns));
                int right = Mathf.RoundToInt(texture.width * ((column + 1f) / PuzzleColumns));
                int id = row * PuzzleColumns + column;
                rects.Add(NewSpriteRect($"Stage1Photo_{id:00}",
                    new Rect(left, bottom, right - left, top - bottom)));
            }
        }

        provider.SetSpriteRects(rects.ToArray());
        provider.Apply();
        importer.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(PhotoPath)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name, sprite => sprite);
    }

    private static Sprite ConfigureRoomBackgroundSprite()
    {
        TextureImporter importer = AssetImporter.GetAtPath(RoomBackgroundPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"Room background was not imported: {RoomBackgroundPath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoomBackgroundPath);
        if (sprite == null)
            throw new InvalidOperationException($"Room background sprite could not be loaded: {RoomBackgroundPath}");
        return sprite;
    }

    private static void ConfigurePlayerLightPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerLightPrefabPath);
        try
        {
            Light2D light = root.GetComponent<Light2D>();
            PlayerLightController controller = root.GetComponent<PlayerLightController>();
            if (light == null || controller == null)
                throw new InvalidOperationException("PlayerLight prefab is missing Light2D or PlayerLightController.");

            light.color = new Color32(0xE0, 0xAB, 0x62, 0xFF);
            light.intensity = 1.5f;
            light.pointLightInnerRadius = 0.35f;
            light.pointLightOuterRadius = 3.2f;
            light.falloffIntensity = 0.5f;
            PrefabUtility.SaveAsPrefabAsset(root, PlayerLightPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Dictionary<string, Sprite> ConfigurePlayerDirectionalSprites()
    {
        TextureImporter importer = AssetImporter.GetAtPath(PlayerFrontBackPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"Front/back player art was not imported: {PlayerFrontBackPath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 1000f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 8192;
        importer.SaveAndReimport();

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PlayerFrontBackPath);
        if (texture == null || texture.width != 5978 || texture.height != 2480)
            throw new InvalidOperationException(
                $"PlayerFrontBack.png must be the 5978x2480 six-frame sheet. Current: " +
                $"{texture?.width ?? 0}x{texture?.height ?? 0}");

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        Dictionary<string, GUID> existingIds = provider.GetSpriteRects()
            .GroupBy(rect => rect.name)
            .ToDictionary(group => group.Key, group => group.First().spriteID);

        (string name, Rect rect)[] frames =
        {
            ("PlayerFrontIdle", new Rect(88f, 221f, 839f, 2082f)),
            ("PlayerFrontWalkLeft", new Rect(1035f, 426f, 756f, 1780f)),
            ("PlayerFrontWalkRight", new Rect(1886f, 414f, 756f, 1780f)),
            ("PlayerBackIdle", new Rect(2893f, 288f, 797f, 2020f)),
            ("PlayerBackWalkLeft", new Rect(3917f, 345f, 759f, 1865f)),
            ("PlayerBackWalkRight", new Rect(4877f, 347f, 759f, 1865f)),
        };

        SpriteRect[] rects = frames.Select(frame => new SpriteRect
        {
            name = frame.name,
            rect = frame.rect,
            alignment = SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f),
            spriteID = existingIds.TryGetValue(frame.name, out GUID id) ? id : GUID.Generate(),
        }).ToArray();

        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();

        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(PlayerFrontBackPath)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name, sprite => sprite);
        if (sprites.Count != frames.Length)
            throw new InvalidOperationException($"Expected six player sprites, imported {sprites.Count}.");
        return sprites;
    }

    private static void ConfigurePlayerDirectionalAnimations(Dictionary<string, Sprite> sprites)
    {
        ConfigureSpriteClip(PlayerAnimationDirectory + "IdleDown.anim", false,
            sprites["PlayerFrontIdle"]);
        ConfigureSpriteClip(PlayerAnimationDirectory + "WalkDown.anim", true,
            sprites["PlayerFrontWalkLeft"], sprites["PlayerFrontWalkRight"]);
        ConfigureSpriteClip(PlayerAnimationDirectory + "IdleUp.anim", false,
            sprites["PlayerBackIdle"]);
        ConfigureSpriteClip(PlayerAnimationDirectory + "WalkUp.anim", true,
            sprites["PlayerBackWalkLeft"], sprites["PlayerBackWalkRight"]);

        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            PlayerMove playerMove = root.GetComponent<PlayerMove>();
            if (renderer == null || playerMove == null)
                throw new InvalidOperationException("Player prefab is missing SpriteRenderer or PlayerMove.");

            renderer.sprite = sprites["PlayerFrontIdle"];
            renderer.flipX = false;
            root.transform.localScale = Vector3.one * 0.95f;
            SerializedObject serializedMove = new SerializedObject(playerMove);
            serializedMove.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            serializedMove.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureSpriteClip(string path, bool loop, params Sprite[] frames)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
            throw new InvalidOperationException($"Player animation clip was not found: {path}");

        List<ObjectReferenceKeyframe> keyframes = new List<ObjectReferenceKeyframe>();
        if (frames.Length == 1)
        {
            keyframes.Add(new ObjectReferenceKeyframe { time = 0f, value = frames[0] });
        }
        else
        {
            const float frameDuration = 0.15f;
            for (int index = 0; index < frames.Length; index++)
                keyframes.Add(new ObjectReferenceKeyframe { time = index * frameDuration, value = frames[index] });
            keyframes.Add(new ObjectReferenceKeyframe
            {
                time = frames.Length * frameDuration,
                value = frames[0],
            });
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty,
            typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());
        clip.frameRate = 10f;

        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        SerializedProperty loopTime = settings?.FindPropertyRelative("m_LoopTime");
        if (loopTime != null)
            loopTime.boolValue = loop;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clip);
    }

    private static bool ClipUsesPlayerSprites(string path, int minimumKeyframes)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
            return false;

        EditorCurveBinding binding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
            .FirstOrDefault(candidate => candidate.type == typeof(SpriteRenderer)
                                         && candidate.propertyName == "m_Sprite");
        ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        return keyframes != null
               && keyframes.Length >= minimumKeyframes
               && keyframes.All(keyframe => keyframe.value is Sprite sprite
                                            && AssetDatabase.GetAssetPath(sprite) == PlayerFrontBackPath);
    }

    private static SpriteRect NewSpriteRect(string name, Rect rect)
    {
        return new SpriteRect
        {
            name = name,
            rect = rect,
            alignment = SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f),
            spriteID = GUID.Generate(),
        };
    }

    private static void ConfigurePictureInteractablePrefab(Dictionary<string, Sprite> photoSprites)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PicturePrefabPath);
        try
        {
            RemoveMissingScripts(root);

            Transform obsoletePopup = FindChild(root.transform, "PicturePiecePopup");
            if (obsoletePopup != null)
                Object.DestroyImmediate(obsoletePopup.gameObject);

            foreach (MeshFilter component in root.GetComponents<MeshFilter>())
                Object.DestroyImmediate(component);
            foreach (MeshRenderer component in root.GetComponents<MeshRenderer>())
                Object.DestroyImmediate(component);
            foreach (Collider component in root.GetComponents<Collider>())
                Object.DestroyImmediate(component);

            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(root);
            renderer.sprite = photoSprites["Stage1Photo_00"];
            renderer.sortingOrder = 30;
            renderer.color = new Color(1f, 1f, 1f, 0.95f);
            root.transform.localScale = Vector3.one * 0.18f;

            CircleCollider2D trigger = GetOrAdd<CircleCollider2D>(root);
            trigger.isTrigger = true;
            trigger.radius = 2.3f;   // 실효 반경 약 0.41 (프리팹 스케일 0.18)

            RoomInteractable interactable = GetOrAdd<RoomInteractable>(root);
            interactable.Configure("photo_piece", "[E] 사진 조각 줍기", true);
            interactable.puzzleAction ??= new PuzzleAction();
            interactable.puzzleAction.Conditions = new List<GameCondition> { GameCondition.PrologueEnded };
            interactable.puzzleAction.OpeningUI = null;
            interactable.puzzleAction.NarrationID = new List<string>();
            interactable.puzzleAction.ChagingConditions = new List<GameCondition>();
            interactable.puzzleAction.CollectCollectionType = CollectionType.Picture;
            interactable.puzzleAction.StartCollectionType = CollectionType.None;
            interactable.puzzleAction.NeededCollectionCount = 0;

            PrefabUtility.SaveAsPrefabAsset(root, PicturePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigurePicturePuzzlePrefab(Dictionary<string, Sprite> photoSprites)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PuzzlePrefabPath);
        try
        {
            RemoveMissingScripts(root);
            root.name = "PicturePuzzle";
            root.SetActive(false);

            DestroyChildrenNamed(root.transform, "Completed Image", "Stage 1 Status", "Scan Button");

            PictureCollector collector = GetOrAdd<PictureCollector>(root);
            GetOrAdd<PuzzleWindowedUI>(root);
            AudioSource audioSource = GetOrAdd<AudioSource>(root);
            audioSource.playOnAwake = false;
            audioSource.loop = false;

            List<Transform> pictures = new List<Transform>();
            List<Transform> answers = new List<Transform>();
            int[] initialShuffle = { 7, 1, 10, 4, 0, 8, 2, 11, 6, 3, 9, 5 };
            Vector2 pieceSize = new Vector2(145f, 150f);

            for (int id = 0; id < 12; id++)
            {
                Transform answer = FindChild(root.transform, $"Answer {id + 1:00}");
                Transform picture = FindChild(root.transform, $"Picture {id + 1:00}");
                if (answer == null || picture == null)
                    throw new InvalidOperationException($"Picture puzzle prefab is missing piece or answer {id + 1:00}.");

                int row = id / PuzzleColumns;
                int column = id % PuzzleColumns;
                RectTransform answerRect = (RectTransform)answer;
                answerRect.anchorMin = answerRect.anchorMax = new Vector2(0.5f, 0.5f);
                answerRect.pivot = new Vector2(0.5f, 0.5f);
                answerRect.sizeDelta = pieceSize;
                // 4열이 되면서 오른쪽 트레이와 겹치지 않도록 시작점을 왼쪽으로 당긴다.
                answerRect.anchoredPosition = new Vector2(-430f + column * pieceSize.x, 150f - row * pieceSize.y);

                // 정답 미리보기를 깔아두면 퍼즐이 아니므로, 빈 슬롯만 표시한다.
                Image answerImage = GetOrAdd<Image>(answer.gameObject);
                answerImage.sprite = null;
                answerImage.color = new Color(1f, 1f, 1f, 0.06f);
                answerImage.raycastTarget = false;

                int trayIndex = initialShuffle[id];
                int trayRow = trayIndex / 3;
                int trayColumn = trayIndex % 3;
                RectTransform pictureRect = (RectTransform)picture;
                pictureRect.anchorMin = pictureRect.anchorMax = new Vector2(0.5f, 0.5f);
                pictureRect.pivot = new Vector2(0.5f, 0.5f);
                pictureRect.sizeDelta = pieceSize;
                pictureRect.anchoredPosition = new Vector2(170f + trayColumn * 160f, 225f - trayRow * pieceSize.y);

                Image pictureImage = GetOrAdd<Image>(picture.gameObject);
                pictureImage.sprite = photoSprites[$"Stage1Photo_{id:00}"];
                pictureImage.color = Color.white;
                pictureImage.raycastTarget = true;
                GetOrAdd<CanvasGroup>(picture.gameObject);

                DragHandler dragHandler = GetOrAdd<DragHandler>(picture.gameObject);
                dragHandler.Configure(id, collector);
                pictures.Add(picture);
                answers.Add(answer);
            }

            Image completedImage = CreateImage(root.transform, "Completed Image",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-215f, 0f), new Vector2(435f, 600f), Color.white);
            completedImage.sprite = photoSprites["Stage1Photo_Full"];
            completedImage.preserveAspect = true;
            completedImage.raycastTarget = false;
            completedImage.transform.SetSiblingIndex(root.transform.childCount - 1);
            completedImage.gameObject.SetActive(false);

            Text status = CreateText(root.transform, "Stage 1 Status",
                new Vector2(0.18f, 0.91f), new Vector2(0.82f, 0.98f),
                Vector2.zero, Vector2.zero, "RECONSTRUCT THE IMAGE  0/12", 28, TextAnchor.MiddleCenter);
            status.color = new Color(0.75f, 0.95f, 1f, 1f);

            Button scanButton = CreateButton(root.transform, "Scan Button",
                new Vector2(0.41f, 0.025f), new Vector2(0.59f, 0.095f),
                "SCAN", new Color(0.12f, 0.52f, 0.58f, 1f));
            scanButton.gameObject.SetActive(false);

            SerializedObject serialized = new SerializedObject(collector);
            SetObjectArray(serialized.FindProperty("Pictures"), pictures.Cast<Object>());
            SetObjectArray(serialized.FindProperty("Answers"), answers.Cast<Object>());
            serialized.FindProperty("completedImage").objectReferenceValue = completedImage;
            serialized.FindProperty("scanButton").objectReferenceValue = scanButton;
            serialized.FindProperty("statusText").objectReferenceValue = status;
            serialized.FindProperty("sfxSource").objectReferenceValue = audioSource;
            serialized.FindProperty("incorrectClip").objectReferenceValue = LoadAudio("PuzzleFail.mp3");
            serialized.FindProperty("successClip").objectReferenceValue = LoadAudio("PuzzleSuccess.mp3");
            serialized.FindProperty("shuffleTrayOnReset").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PuzzlePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureVirtualDesktopPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(VirtualDesktopPrefabPath);
        try
        {
            Transform canvasTransform = FindChild(root.transform, "Virtual Desktop Canvas");
            Transform desktopTransform = FindChild(root.transform, "Desktop");
            Transform leaveTransform = FindChild(root.transform, "Leave Computer");
            if (canvasTransform == null || desktopTransform == null || leaveTransform == null)
                throw new InvalidOperationException("Virtual Desktop prefab is missing Canvas, Desktop, or Leave Computer.");

            canvasTransform.gameObject.SetActive(true);
            canvasTransform.localScale = Vector3.one;
            Button leaveButton = leaveTransform.GetComponent<Button>();
            if (leaveButton == null)
                throw new InvalidOperationException("Virtual Desktop Leave Computer object is missing its Button.");

            ConfigureDesktopWindow(desktopTransform.gameObject, leaveButton, true);
            PrefabUtility.SaveAsPrefabAsset(root, VirtualDesktopPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureSampleScene(Dictionary<string, Sprite> photoSprites, Sprite roomBackground)
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        foreach (GameObject root in scene.GetRootGameObjects())
            RemoveMissingScripts(root);

        FlattenControlSSceneRoot(scene);

        GameObject controllerRoot = FindByName(scene, "Game Controller");
        if (controllerRoot != null)
            Object.DestroyImmediate(controllerRoot);
        controllerRoot = new GameObject("Game Controller");
        SceneManager.MoveGameObjectToScene(controllerRoot, scene);

        GameStateManager stateManager = FindComponent<GameStateManager>(scene) ?? controllerRoot.AddComponent<GameStateManager>();
        GameConditionManager conditionManager = FindComponent<GameConditionManager>(scene) ?? controllerRoot.AddComponent<GameConditionManager>();
        CollectionSystem collectionSystem = FindComponent<CollectionSystem>(scene) ?? controllerRoot.AddComponent<CollectionSystem>();
        ScriptManager scriptManager = FindComponent<ScriptManager>(scene) ?? controllerRoot.AddComponent<ScriptManager>();
        InteractionManager interactionManager = FindComponent<InteractionManager>(scene) ?? controllerRoot.AddComponent<InteractionManager>();

        SerializedObject stateSerialized = new SerializedObject(stateManager);
        stateSerialized.FindProperty("initialState").enumValueIndex = (int)GameState.UI;
        stateSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject conditionSerialized = new SerializedObject(conditionManager);
        conditionSerialized.FindProperty("recoveryStage").enumValueIndex = (int)RecoveryStage.None;
        conditionSerialized.ApplyModifiedPropertiesWithoutUndo();

        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(InputActionsGuid));
        if (inputActions == null)
            throw new InvalidOperationException("Default input actions could not be loaded.");

        GameObject virtualDesktop = FindByName(scene, "Virtual Desktop Canvas");
        if (virtualDesktop == null)
            throw new InvalidOperationException("SampleScene no longer contains the serialized Virtual Desktop System prefab instance.");
        virtualDesktop.SetActive(true);
        virtualDesktop.transform.localScale = Vector3.one;
        UIManager uiManager = FindComponent<UIManager>(scene) ?? virtualDesktop.AddComponent<UIManager>();
        SetObjectReference(uiManager, "inputActionsAsset", inputActions);
        SetObjectReference(interactionManager, "inputActions", inputActions);

        GameObject desktop = FindByName(scene, "Desktop");
        GameObject leaveComputer = FindByName(scene, "Leave Computer");
        if (desktop == null || leaveComputer == null)
            throw new InvalidOperationException("SampleScene Virtual Desktop is missing Desktop or Leave Computer.");
        Button leaveComputerButton = leaveComputer.GetComponent<Button>();
        if (leaveComputerButton == null)
            throw new InvalidOperationException("SampleScene Leave Computer object is missing its Button.");
        ComputerWindowedUI computerDesktop = ConfigureDesktopWindow(desktop, leaveComputerButton, true);

        GameObject player = FindByName(scene, "Player");
        string existingPlayerPrefabPath = player != null
            ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player)
            : string.Empty;
        if (player == null || player.GetComponent<PlayerMove>() == null || existingPlayerPrefabPath != PlayerPrefabPath)
        {
            Vector3 spawnPosition = player != null ? player.transform.position : new Vector3(0f, -2.1f, 0f);
            if (player != null)
                Object.DestroyImmediate(player);
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = spawnPosition;
        }
        player.tag = "Player";
        player.transform.position = new Vector3(-0.35f, -1.55f, 0f);
        Rigidbody2D playerBody = GetOrAdd<Rigidbody2D>(player);
        playerBody.gravityScale = 0f;
        playerBody.freezeRotation = true;
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;

        SpeechBubbleController speechBubble = player.GetComponentInChildren<SpeechBubbleController>(true);
        if (speechBubble == null)
        {
            GameObject speechBubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ControlS/Resources/Pefabs/UI/SpeechBubble.prefab");
            GameObject speechBubbleObject = (GameObject)PrefabUtility.InstantiatePrefab(speechBubblePrefab, scene);
            speechBubbleObject.transform.SetParent(player.transform, false);
            speechBubble = speechBubbleObject.GetComponent<SpeechBubbleController>();
        }
        speechBubble.gameObject.SetActive(true);
        ConfigureSpeechBubbleOverlay(speechBubble);

        AudioListener[] listeners = FindComponents<AudioListener>(scene);
        AudioListener primaryListener = listeners.FirstOrDefault(item => item.gameObject.name == "Main Camera")
                                        ?? listeners.FirstOrDefault();
        foreach (AudioListener listener in listeners)
            listener.enabled = listener == primaryListener;

        EventSystem[] eventSystems = FindComponents<EventSystem>(scene);
        EventSystem primaryEventSystem = eventSystems.FirstOrDefault(item => item.gameObject.name == "EventSystem")
                                         ?? eventSystems.FirstOrDefault();
        foreach (EventSystem eventSystem in eventSystems)
            eventSystem.enabled = eventSystem == primaryEventSystem;

        HideLegacyRoomPresentation(scene);
        ConfigureMainCamera(scene);

        GameObject roomAdditions = RecreateRoot(scene, "Stage 1 Room Additions");
        GameObject balconyDoor = ConfigureRoomAdditions(roomAdditions.transform, photoSprites, roomBackground);

        GameObject puzzleObject = FindComponent<PictureCollector>(scene)?.gameObject;
        if (puzzleObject == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PuzzlePrefabPath);
            puzzleObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        }
        puzzleObject.name = "PicturePuzzle";
        PictureCollector collector = puzzleObject.GetComponent<PictureCollector>();
        PuzzleWindowedUI puzzleWindow = puzzleObject.GetComponent<PuzzleWindowedUI>() ?? puzzleObject.AddComponent<PuzzleWindowedUI>();
        puzzleObject.SetActive(false);

        GameObject collectionHud = FindByName(scene, "PictureCollectionHUD - Stage 1");
        if (collectionHud == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionHudPrefabPath);
            collectionHud = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            collectionHud.name = "PictureCollectionHUD - Stage 1";
        }
        collectionHud.SetActive(false);

        GameObject hudCanvas = FindByName(scene, "Room HUD Canvas");
        if (hudCanvas == null)
            hudCanvas = CreateScreenCanvas(scene, "Room HUD Canvas", 80);
        else
            ConfigureScreenCanvas(hudCanvas, 80);

        Transform oldHud = FindChild(hudCanvas.transform, "Prologue Stage 1 HUD");
        if (oldHud != null)
            Object.DestroyImmediate(oldHud.gameObject);
        RectTransform hudRoot = CreateRect(hudCanvas.transform, "Prologue Stage 1 HUD",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);


        Text objectiveText = CreateText(hudRoot, "Objective",
            new Vector2(0.03f, 0.88f), new Vector2(0.55f, 0.97f),
            Vector2.zero, Vector2.zero, string.Empty, 26, TextAnchor.MiddleLeft);
        objectiveText.color = new Color(1f, 0.92f, 0.72f, 1f);

        Text actionText = CreateText(hudRoot, "Prologue Action",
            new Vector2(0.18f, 0.11f), new Vector2(0.82f, 0.19f),
            Vector2.zero, Vector2.zero, string.Empty, 25, TextAnchor.MiddleCenter);
        actionText.color = new Color(0.85f, 0.9f, 0.95f, 1f);

        Text promptText = CreateText(hudRoot, "Interaction Prompt",
            new Vector2(0.32f, 0.025f), new Vector2(0.68f, 0.095f),
            Vector2.zero, Vector2.zero, string.Empty, 28, TextAnchor.MiddleCenter);
        promptText.color = new Color(1f, 0.86f, 0.35f, 1f);
        promptText.gameObject.SetActive(false);
        interactionManager.SetPromptText(promptText);

        Image monitorBlackout = CreateImage(hudRoot, "Monitor Blackout",
            new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.86f),
            Vector2.zero, Vector2.zero, Color.black);
        monitorBlackout.raycastTarget = false;
        monitorBlackout.gameObject.SetActive(false);

        GameObject stageTwoPreview = CreatePanel(hudRoot, "Stage 2 Preview",
            new Vector2(0.68f, 0.69f), new Vector2(0.97f, 0.87f),
            new Color(0.04f, 0.08f, 0.1f, 0.92f)).gameObject;
        CreateText(stageTwoPreview.transform, "Preview Text", Vector2.zero, Vector2.one,
            new Vector2(18f, 12f), new Vector2(-18f, -12f),
            "STEP 2  ACTIVITY LOG\n전자레인지와 외부 시계를 조사하세요.", 22, TextAnchor.MiddleLeft);
        stageTwoPreview.SetActive(false);

        GameObject recoveryWindow = FindByName(scene, "Window - Recovery");
        if (recoveryWindow == null)
            throw new InvalidOperationException("Virtual Desktop System is missing Window - Recovery.");
        ConfigureRecoveryWindow(recoveryWindow, out Text recoveryBody, out Text recoveryProgress, out Button startRecovery);
        recoveryWindow.SetActive(false);

        GameObject photoRoot = RecreateRoot(scene, "Stage 1 Photo Pieces");
        List<RoomInteractable> photoPieces = CreatePhotoPieces(scene, photoRoot.transform, photoSprites);
        RoomInteractable computer = ConfigureComputer(scene, computerDesktop);
        LightController computerScreenLight = ConfigureComputerScreenLight(computer.gameObject);

        Transform oldAudio = FindChild(controllerRoot.transform, "Stage 1 Audio");
        if (oldAudio != null)
            Object.DestroyImmediate(oldAudio.gameObject);
        GameObject audioRoot = new GameObject("Stage 1 Audio");
        audioRoot.transform.SetParent(controllerRoot.transform, false);
        AudioSource fan = CreateAudioSource(audioRoot.transform, "Ambience - Computer Fan", LoadAudio("ComputerFan.mp3"), true, 0.18f);
        AudioSource fridge = CreateAudioSource(audioRoot.transform, "Ambience - Fridge", LoadAudio("FridgeAmbience.mp3"), true, 0.12f);
        AudioSource outdoor = CreateAudioSource(audioRoot.transform, "Ambience - Outside", LoadAudio("OutdoorAmbience.mp3"), true, 0.1f);
        AudioSource balcony = CreateAudioSource(audioRoot.transform, "Ambience - Balcony", LoadAudio("BalconyAmbience.mp3"), true, 0.22f);
        AudioSource keyboard = CreateAudioSource(audioRoot.transform, "SFX - Keyboard Loop", LoadAudio("KeyboardLoop.mp3"), true, 0.24f);
        AudioSource keyPress = CreateAudioSource(audioRoot.transform, "SFX - Key Press", null, false, 0.7f);
        AudioSource effects = CreateAudioSource(audioRoot.transform, "SFX - Stage One", null, false, 0.8f);

        StageOneFlowController flow = FindComponent<StageOneFlowController>(scene) ?? controllerRoot.AddComponent<StageOneFlowController>();
        SerializedObject flowSerialized = new SerializedObject(flow);
        flowSerialized.FindProperty("objectiveText").objectReferenceValue = objectiveText;
        flowSerialized.FindProperty("actionText").objectReferenceValue = actionText;
        flowSerialized.FindProperty("recoveryBodyText").objectReferenceValue = recoveryBody;
        flowSerialized.FindProperty("recoveryProgressText").objectReferenceValue = recoveryProgress;
        flowSerialized.FindProperty("monitorBlackout").objectReferenceValue = monitorBlackout;
        flowSerialized.FindProperty("recoveryWindow").objectReferenceValue = recoveryWindow;
        flowSerialized.FindProperty("collectionHud").objectReferenceValue = collectionHud;
        flowSerialized.FindProperty("stageTwoPreviewRoot").objectReferenceValue = stageTwoPreview;
        flowSerialized.FindProperty("startRecoveryButton").objectReferenceValue = startRecovery;
        flowSerialized.FindProperty("computerDesktop").objectReferenceValue = computerDesktop;
        flowSerialized.FindProperty("computerScreenLight").objectReferenceValue = computerScreenLight;
        flowSerialized.FindProperty("balconyDoor").objectReferenceValue = balconyDoor.transform;
        // 오른쪽 벽에 붙었으니 문은 가로가 아니라 세로로 열린다.
        flowSerialized.FindProperty("balconyDoorOpenOffset").vector3Value = new Vector3(0f, -1.9f, 0f);
        flowSerialized.FindProperty("balconyDoorOpenDuration").floatValue = 1.25f;
        // 전용 소스를 쓴다. outdoor를 그대로 쓰면 StartAmbience와 OpenBalconyDoor가
        // 같은 AudioSource를 서로 Stop/Play 하면서 실외음이 끊긴다.
        flowSerialized.FindProperty("balconyAmbienceSource").objectReferenceValue = balcony;
        flowSerialized.FindProperty("photoPuzzleWindow").objectReferenceValue = puzzleWindow;
        flowSerialized.FindProperty("pictureCollector").objectReferenceValue = collector;
        flowSerialized.FindProperty("computerInteractable").objectReferenceValue = computer;
        SetObjectArray(flowSerialized.FindProperty("photoPieces"), photoPieces.Cast<Object>());
        SetObjectArray(flowSerialized.FindProperty("furnitureViews"), CreatedFurnitureViews.Cast<Object>());
        SetObjectArray(flowSerialized.FindProperty("stageTwoInvestigationMarkers"), Array.Empty<Object>());
        SetObjectArray(flowSerialized.FindProperty("ambienceSources"), new Object[] { fan, fridge, outdoor });
        flowSerialized.FindProperty("keyboardLoopSource").objectReferenceValue = keyboard;
        flowSerialized.FindProperty("keyPressSource").objectReferenceValue = keyPress;
        flowSerialized.FindProperty("effectsSource").objectReferenceValue = effects;
        flowSerialized.FindProperty("keyPressClip").objectReferenceValue = LoadAudio("KeyPress.mp3");
        flowSerialized.FindProperty("computerCrashClip").objectReferenceValue = LoadAudio("ComputerCrash.mp3");
        flowSerialized.FindProperty("computerBootClip").objectReferenceValue = LoadAudio("ComputerBoot.mp3");
        flowSerialized.FindProperty("paperHintClip").objectReferenceValue = LoadAudio("PaperHint.mp3");
        flowSerialized.FindProperty("photoPickupClip").objectReferenceValue = LoadAudio("PhotoPickup.mp3");
        flowSerialized.FindProperty("scannerClip").objectReferenceValue = LoadAudio("Scanner.mp3");
        flowSerialized.FindProperty("hintDelay").floatValue = 20f;
        flowSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (openedHere)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static GameObject ConfigureRoomAdditions(Transform root, Dictionary<string, Sprite> photoSprites,
        Sprite roomBackground)
    {
        Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ControlS/Resources/ControlS/WhitePixel.png");
        if (white == null)
            white = photoSprites["Stage1Photo_00"];

        GameObject artwork = new GameObject("Room Artwork - Provided Layout");
        artwork.transform.SetParent(root, false);
        artwork.transform.localPosition = new Vector3(0f, 0f, 1f);
        SpriteRenderer artworkRenderer = artwork.AddComponent<SpriteRenderer>();
        artworkRenderer.sprite = roomBackground;
        artworkRenderer.color = Color.white;
        artworkRenderer.sortingOrder = -100;
        float artworkScale = 10.2f / Mathf.Max(roomBackground.bounds.size.y, 0.01f);
        artwork.transform.localScale = Vector3.one * artworkScale;

        // 발코니는 오른쪽 벽. 책상(y 1.5~4.9)과 침대(y -4.75~-0.65) 사이의 빈 구간에 놓는다.
        GameObject balconyDoor = new GameObject("Balcony Sliding Door - Closed");
        balconyDoor.transform.SetParent(root, false);
        balconyDoor.transform.localPosition = new Vector3(6.98f, 0.4f, 0f);
        CreateVisualRect(balconyDoor.transform, "Balcony Glass", white, Vector2.zero,
            new Vector2(0.62f, 2.05f), new Color(0.45f, 0.9f, 0.96f, 0.16f), -82);
        CreateVisualRect(balconyDoor.transform, "Balcony Door Edge", white, new Vector2(0f, 1.0f),
            new Vector2(0.66f, 0.05f), new Color(0.05f, 0.13f, 0.24f, 0.72f), -81);
        CreateVisualRect(balconyDoor.transform, "Balcony Door Handle", white, new Vector2(-0.22f, 0.35f),
            new Vector2(0.06f, 0.24f), new Color(0.95f, 0.3f, 0.75f, 0.9f), -80);

        CreateCollisionRect(root, "Collision - Top Wall", new Vector2(0f, 5.02f), new Vector2(15.2f, 0.28f));
        CreateCollisionRect(root, "Collision - Bottom Wall", new Vector2(0f, -5.02f), new Vector2(15.2f, 0.28f));
        CreateCollisionRect(root, "Collision - Left Wall", new Vector2(-7.55f, 0f), new Vector2(0.3f, 10f));
        CreateCollisionRect(root, "Collision - Right Wall", new Vector2(7.55f, 0f), new Vector2(0.3f, 10f));
        CreateCollisionRect(root, "Collision - Kitchen", new Vector2(-4.62f, 3.18f), new Vector2(5.85f, 3.35f));
        CreateCollisionRect(root, "Collision - Fridge", new Vector2(0.18f, 3.18f), new Vector2(2.15f, 3.35f));
        CreateCollisionRect(root, "Collision - Desk", new Vector2(4.53f, 3.2f), new Vector2(5.85f, 3.4f));
        CreateCollisionRect(root, "Collision - Bed", new Vector2(5.84f, -2.7f), new Vector2(3.35f, 4.1f));
        CreateCollisionRect(root, "Collision - Entrance Storage", new Vector2(-5.58f, -3.35f), new Vector2(3.9f, 2.95f));
        CreateCollisionRect(root, "Collision - Lower Shelf", new Vector2(0.35f, -4.42f), new Vector2(3.35f, 1.18f));
        CreateCollisionRect(root, "Collision - Bedside Table", new Vector2(3.25f, -4.3f), new Vector2(1.35f, 1.4f));

        return balconyDoor;
    }

    private static void FlattenControlSSceneRoot(Scene scene)
    {
        GameObject sceneRoot = FindByName(scene, "CONTROL S - Scene Root");
        GameObject roomObject = sceneRoot != null
            ? sceneRoot.transform.Find("Room")?.gameObject
            : FindByName(scene, "Room");
        Transform room = roomObject != null ? roomObject.transform : null;
        Transform computer = room != null ? FindChild(room, "Computer") : null;
        if (computer != null)
            computer.SetParent(null, true);
        if (roomObject != null)
            Object.DestroyImmediate(roomObject);

        GameObject hudCanvasObject = sceneRoot != null
            ? sceneRoot.transform.Find("Room HUD Canvas")?.gameObject
            : FindByName(scene, "Room HUD Canvas");
        Transform hudCanvas = hudCanvasObject != null ? hudCanvasObject.transform : null;
        if (hudCanvas != null)
        {
            Transform stageHud = FindChild(hudCanvas, "Prologue Stage 1 HUD");
            for (int index = hudCanvas.childCount - 1; index >= 0; index--)
            {
                Transform child = hudCanvas.GetChild(index);
                if (child != stageHud)
                    Object.DestroyImmediate(child.gameObject);
            }

            hudCanvas.SetParent(null, true);
            hudCanvas.localScale = Vector3.one;
        }

        Transform eventSystem = sceneRoot != null
            ? sceneRoot.transform.Find("EventSystem")
            : null;
        if (eventSystem != null)
            eventSystem.SetParent(null, true);

        Transform virtualDesktopSystem = sceneRoot != null
            ? sceneRoot.transform.Find("Virtual Desktop System")
            : null;
        if (virtualDesktopSystem != null)
        {
            virtualDesktopSystem.SetParent(null, true);
            virtualDesktopSystem.localScale = Vector3.one;
        }

        if (sceneRoot != null)
            Object.DestroyImmediate(sceneRoot);
    }

    private static void HideLegacyRoomPresentation(Scene scene)
    {
        GameObject legacyRoom = FindByName(scene, "Room");
        if (legacyRoom == null)
            return;

        foreach (Renderer renderer in legacyRoom.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
        foreach (Collider2D collider in legacyRoom.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;
        foreach (Collider collider in legacyRoom.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
    }

    private static void ConfigureMainCamera(Scene scene)
    {
        GameObject cameraObject = FindByName(scene, "Main Camera");
        Camera camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : FindComponent<Camera>(scene);
        if (camera == null)
            throw new InvalidOperationException("SampleScene is missing its Main Camera.");

        camera.orthographic = true;
        camera.orthographicSize = 5.1f;
        camera.backgroundColor = new Color(0.01f, 0.012f, 0.025f, 1f);
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;
    }

    private static GameObject CreateVisualRect(Transform parent, string name, Sprite sprite, Vector2 localPosition,
        Vector2 size, Color color, int sortingOrder)
    {
        GameObject visual = new GameObject(name);
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        Vector2 spriteSize = sprite != null ? sprite.bounds.size : Vector2.one;
        visual.transform.localScale = new Vector3(size.x / Mathf.Max(spriteSize.x, 0.01f),
            size.y / Mathf.Max(spriteSize.y, 0.01f), 1f);
        return visual;
    }

    private static GameObject CreateCollisionRect(Transform parent, string name, Vector2 localPosition, Vector2 size)
    {
        GameObject collision = new GameObject(name);
        collision.transform.SetParent(parent, false);
        collision.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        BoxCollider2D collider = collision.AddComponent<BoxCollider2D>();
        collider.size = size;
        return collision;
    }

    private static GameObject CreateAnchor(Transform parent, string name, Vector2 localPosition)
    {
        GameObject anchor = new GameObject(name);
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        return anchor;
    }

    // 가구 정면샷 4개. 조각 12개를 여기에 나눠 담는다.
    private static readonly (string key, string title, string prompt, Vector2 room, string[] pieceIds)[] FurnitureViews =
    {
        ("Bed", "침대", "[E] 침대 살펴보기", new Vector2(3.9f, -2.7f),
            new[] { "bed_01", "bed_02", "bed_top_01" }),
        ("Desk", "컴퓨터 책상", "[E] 책상 살펴보기", new Vector2(4.5f, 1.2f),
            new[] { "drawer_01", "drawer_02", "computer_piece_01", "computer_piece_02" }),
        ("Shelf", "책장과 선반", "[E] 선반 살펴보기", new Vector2(-5.2f, 1.2f),
            new[] { "book_01", "cup_01", "frame_01" }),
        ("Balcony", "베란다 문 앞", "[E] 베란다 문 앞 살펴보기", new Vector2(6.1f, 0.4f),
            new[] { "balcony_01", "floor_01" }),
    };

    private static List<RoomInteractable> CreatePhotoPieces(Scene scene, Transform parent, Dictionary<string, Sprite> sprites)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PicturePrefabPath);

        GameObject viewCanvas = FindByName(scene, "Furniture View Canvas");
        if (viewCanvas == null)
            viewCanvas = CreateScreenCanvas(scene, "Furniture View Canvas", 95);
        else
            ConfigureScreenCanvas(viewCanvas, 95);

        foreach (Transform child in viewCanvas.transform.Cast<Transform>().ToList())
            Object.DestroyImmediate(child.gameObject);

        List<RoomInteractable> result = new List<RoomInteractable>();
        CreatedFurnitureViews.Clear();
        int pieceIndex = 0;

        for (int index = 0; index < FurnitureViews.Length; index++)
        {
            var view = FurnitureViews[index];

            // 방 안의 조사 지점. 조각 스프라이트는 보이지 않고 트리거만 남는다.
            GameObject spot = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            spot.name = $"Furniture {index + 1:00} - {view.title}";
            spot.transform.SetParent(parent, false);
            spot.transform.position = new Vector3(view.room.x, view.room.y, -0.2f);
            spot.SetActive(false);

            SpriteRenderer renderer = spot.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            FurnitureViewWindow window = CreateFurnitureViewWindow(viewCanvas.transform, view, sprites, ref pieceIndex);
            CreatedFurnitureViews.Add(window);

            RoomInteractable interactable = spot.GetComponent<RoomInteractable>();
            interactable.Configure($"furniture_{view.key.ToLowerInvariant()}", view.prompt, false);
            interactable.puzzleAction.Conditions = new List<GameCondition> { GameCondition.PrologueEnded };
            interactable.puzzleAction.OpeningUI = window;
            interactable.puzzleAction.NarrationID = new List<string>();
            interactable.puzzleAction.ChagingConditions = new List<GameCondition>();
            interactable.puzzleAction.CollectCollectionType = CollectionType.None;
            result.Add(interactable);
        }

        return result;
    }

    private static FurnitureViewWindow CreateFurnitureViewWindow(Transform canvasRoot,
        (string key, string title, string prompt, Vector2 room, string[] pieceIds) view,
        Dictionary<string, Sprite> sprites, ref int pieceIndex)
    {
        RectTransform root = CreateRect(canvasRoot, $"Furniture View - {view.key}",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreatePanel(root, "Dim", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.82f));

        // 아트가 들어오기 전 플레이스홀더. SetBackground로 정면샷을 꽂으면 대체된다.
        Image background = CreateImage(root, "Furniture Shot", new Vector2(0.2f, 0.14f), new Vector2(0.8f, 0.86f),
            Vector2.zero, Vector2.zero, new Color(0.92f, 0.92f, 0.94f, 1f));
        background.raycastTarget = false;

        Text title = CreateText(root, "Title", new Vector2(0.2f, 0.87f), new Vector2(0.8f, 0.95f),
            Vector2.zero, Vector2.zero, view.title, 34, TextAnchor.MiddleLeft);
        Text hint = CreateText(root, "Hint", new Vector2(0.2f, 0.05f), new Vector2(0.8f, 0.13f),
            Vector2.zero, Vector2.zero, "수상한 곳을 클릭해 살펴본다.", 24, TextAnchor.MiddleCenter);

        Button closeButton = CreateButton(root, "Close", new Vector2(0.82f, 0.88f), new Vector2(0.88f, 0.94f),
            "X", new Color(0.55f, 0.13f, 0.2f, 0.95f));

        List<FurnitureViewWindow.PieceSlot> slots = new List<FurnitureViewWindow.PieceSlot>();
        int count = view.pieceIds.Length;
        for (int slot = 0; slot < count; slot++)
        {
            // 정면샷 위에 균등 분배. 아트 오면 실제 은신처 위치로 옮긴다.
            float centerX = (slot + 0.5f) / count;
            float minX = Mathf.Lerp(0.24f, 0.76f, Mathf.Clamp01(centerX - 0.5f / count + 0.06f));
            float maxX = Mathf.Lerp(0.24f, 0.76f, Mathf.Clamp01(centerX + 0.5f / count - 0.06f));
            float minY = slot % 2 == 0 ? 0.42f : 0.24f;

            Button pieceButton = CreateButton(root, $"Piece - {view.pieceIds[slot]}",
                new Vector2(minX, minY), new Vector2(maxX, minY + 0.16f),
                view.pieceIds[slot], new Color(1f, 1f, 1f, 0.5f));

            // 어떤 조각이 나올지 눈으로 확인할 수 있게 실제 조각 스프라이트를 미리보기로 깔아둔다.
            if (sprites.TryGetValue($"Stage1Photo_{pieceIndex:00}", out Sprite pieceSprite))
            {
                Image pieceImage = pieceButton.GetComponent<Image>();
                pieceImage.sprite = pieceSprite;
                pieceImage.color = new Color(1f, 1f, 1f, 0.85f);
                pieceImage.preserveAspect = true;
            }

            slots.Add(new FurnitureViewWindow.PieceSlot
            {
                CollectionId = view.pieceIds[slot],
                Button = pieceButton,
            });
            pieceIndex++;
        }

        // 책상 정면샷에는 스캐너를 놓는다. 사진 조립이 끝나야 눌린다.
        List<FurnitureViewWindow.ActionSlot> actions = new List<FurnitureViewWindow.ActionSlot>();
        if (view.key == "Desk")
        {
            Button computer = CreateButton(root, "Action - Computer",
                new Vector2(0.28f, 0.62f), new Vector2(0.46f, 0.8f),
                "컴퓨터", new Color(0.24f, 0.44f, 0.62f, 0.9f));
            actions.Add(new FurnitureViewWindow.ActionSlot
            {
                ActionId = ComputerActionId,
                Button = computer,
                RequiredCondition = GameCondition.PrologueEnded,
            });

            Button scanner = CreateButton(root, "Action - Scanner",
                new Vector2(0.6f, 0.62f), new Vector2(0.76f, 0.8f),
                "스캐너", new Color(0.2f, 0.6f, 0.75f, 0.9f));
            actions.Add(new FurnitureViewWindow.ActionSlot
            {
                ActionId = ScannerActionId,
                Button = scanner,
                RequiredCondition = GameCondition.ImagePuzzleCompleted,
            });
        }

        FurnitureViewWindow window = root.gameObject.AddComponent<FurnitureViewWindow>();
        window.EditorBind(background, title, hint, slots, actions);

        SerializedObject serialized = new SerializedObject(window);
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        root.gameObject.SetActive(false);
        return window;
    }

    private static RoomInteractable ConfigureComputer(Scene scene, ComputerWindowedUI computerDesktop)
    {
        GameObject computer = FindByName(scene, "Computer");
        if (computer == null)
            throw new InvalidOperationException("SampleScene is missing its existing Computer object.");

        foreach (ComputerInteractable oldInteractable in computer.GetComponents<ComputerInteractable>())
            oldInteractable.enabled = false;

        computer.transform.position = new Vector3(3.45f, 1.45f, 0f);

        CircleCollider2D trigger = computer.GetComponents<CircleCollider2D>().FirstOrDefault(item => item.isTrigger)
                                   ?? computer.AddComponent<CircleCollider2D>();
        trigger.enabled = true;
        trigger.isTrigger = true;
        trigger.radius = 0.8f;

        RoomInteractable interactable = computer.GetComponent<RoomInteractable>() ?? computer.AddComponent<RoomInteractable>();
        interactable.Configure("computer_desktop", "[E] 컴퓨터 사용", false);
        interactable.puzzleAction ??= new PuzzleAction();
        interactable.puzzleAction.Conditions = new List<GameCondition> { GameCondition.PrologueEnded };
        interactable.puzzleAction.OpeningUI = computerDesktop;
        interactable.puzzleAction.NarrationID = new List<string>();
        interactable.puzzleAction.ChagingConditions = new List<GameCondition>();
        interactable.puzzleAction.CollectCollectionType = CollectionType.None;
        interactable.puzzleAction.StartCollectionType = CollectionType.None;
        interactable.enabled = false;
        return interactable;
    }

    private static LightController ConfigureComputerScreenLight(GameObject computer)
    {
        Transform existing = FindChild(computer.transform, "Computer Screen Light");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LightPrefabPath);
        if (prefab == null)
            throw new InvalidOperationException($"Generic light prefab was not found: {LightPrefabPath}");

        GameObject screenLight = (GameObject)PrefabUtility.InstantiatePrefab(prefab, computer.transform);
        screenLight.name = "Computer Screen Light";
        screenLight.transform.localPosition = Vector3.zero;
        screenLight.transform.localRotation = Quaternion.identity;
        screenLight.transform.localScale = Vector3.one;
        screenLight.SetActive(true);

        Light2D light = screenLight.GetComponent<Light2D>();
        LightController controller = screenLight.GetComponent<LightController>();
        if (light == null || controller == null)
            throw new InvalidOperationException("Generic light prefab is missing Light2D or LightController.");

        Color screenColor = new Color(0.24f, 0.78f, 1f, 1f);
        light.color = screenColor;
        light.intensity = 0.65f;
        light.pointLightInnerRadius = 0.2f;
        light.pointLightOuterRadius = 1.45f;
        light.falloffIntensity = 0.8f;

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("_defaultColor").colorValue = screenColor;
        serialized.FindProperty("_defaultIntensity").floatValue = 0.65f;
        serialized.FindProperty("_defaultInnerRadius").floatValue = 0.2f;
        serialized.FindProperty("_defaultOuterRadius").floatValue = 1.45f;
        serialized.FindProperty("_defaultFalloff").floatValue = 0.8f;
        serialized.FindProperty("_noiseSpeed").floatValue = 0.8f;
        serialized.FindProperty("_smoothSpeed").floatValue = 2.5f;
        serialized.FindProperty("_radiusVariation").floatValue = 0.08f;
        serialized.FindProperty("_minIntensity").floatValue = 0.55f;
        serialized.FindProperty("_maxIntensity").floatValue = 0.7f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static ComputerWindowedUI ConfigureDesktopWindow(GameObject desktop, Button leaveButton, bool openOnStart)
    {
        desktop.SetActive(true);
        if (desktop.transform is RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        leaveButton.interactable = false;
        ComputerWindowedUI desktopWindow = GetOrAdd<ComputerWindowedUI>(desktop);
        ConfigureDesktopSpeaker(desktop, out GameObject speakerRoot);
        SerializedObject serialized = new SerializedObject(desktopWindow);
        serialized.FindProperty("openOnStart").boolValue = openOnStart;
        serialized.FindProperty("draggable").boolValue = false;
        serialized.FindProperty("leaveComputerButton").objectReferenceValue = leaveButton;
        serialized.FindProperty("desktopSpeaker").objectReferenceValue = speakerRoot;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return desktopWindow;
    }

    private static void ConfigureDesktopSpeaker(GameObject desktop, out GameObject speakerRoot)
    {
        Transform existing = FindChild(desktop.transform, "Desktop Speaker");
        if (existing != null)
        {
            speakerRoot = existing.gameObject;
            Transform existingPortrait = FindChild(existing, "Seated Character");
            if (existingPortrait is RectTransform existingPortraitRect)
            {
                existingPortraitRect.anchorMin = new Vector2(0.98f, 0.03f);
                existingPortraitRect.anchorMax = existingPortraitRect.anchorMin;
                existingPortraitRect.pivot = new Vector2(1f, 0f);
                existingPortraitRect.anchoredPosition = Vector2.zero;
                existingPortraitRect.sizeDelta = new Vector2(0f, 270f);

                AspectRatioFitter existingPortraitAspect = existingPortrait.GetComponent<AspectRatioFitter>();
                if (existingPortraitAspect != null)
                    existingPortraitAspect.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            }

            // 대사는 머리 위 말풍선으로 통일했다. 예전 하단 패널이 남아 있으면 제거한다.
            Transform staleDialogue = FindChild(existing, "Desktop Dialogue");
            if (staleDialogue != null)
                Object.DestroyImmediate(staleDialogue.gameObject);
            return;
        }

        Texture2D speakerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DesktopSpeakerPath);
        if (speakerTexture == null)
            throw new InvalidOperationException($"Desktop speaker art is missing: {DesktopSpeakerPath}");

        speakerRoot = new GameObject("Desktop Speaker", typeof(RectTransform));
        speakerRoot.transform.SetParent(desktop.transform, false);
        RectTransform rootRect = (RectTransform)speakerRoot.transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;
        rootRect.SetAsLastSibling();

        GameObject portraitObject = new GameObject("Seated Character", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
        portraitObject.transform.SetParent(rootRect, false);
        RectTransform portraitRect = (RectTransform)portraitObject.transform;
        portraitRect.anchorMin = new Vector2(0.98f, 0.03f);
        portraitRect.anchorMax = portraitRect.anchorMin;
        portraitRect.pivot = new Vector2(1f, 0f);
        portraitRect.anchoredPosition = Vector2.zero;
        portraitRect.sizeDelta = new Vector2(0f, 270f);

        RawImage portrait = portraitObject.GetComponent<RawImage>();
        portrait.texture = speakerTexture;
        portrait.uvRect = new Rect(0.64f, 0f, 0.32f, 1f);
        portrait.raycastTarget = false;

        AspectRatioFitter portraitAspect = portraitObject.GetComponent<AspectRatioFitter>();
        portraitAspect.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        portraitAspect.aspectRatio = speakerTexture.width * portrait.uvRect.width
                                     / (speakerTexture.height * portrait.uvRect.height);


    }

    private static void ConfigureRecoveryWindow(GameObject window, out Text body, out Text progress, out Button button)
    {
        Transform existing = FindChild(window.transform, "Stage One Recovery Content");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        Image panel = CreatePanel(window.transform, "Stage One Recovery Content",
            new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.88f),
            new Color(0.035f, 0.06f, 0.075f, 0.985f));
        body = CreateText(panel.transform, "Recovery Body",
            new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.9f),
            Vector2.zero, Vector2.zero,
            "Unsaved Project Detected.\n\nRecovery Wizard is available.", 25, TextAnchor.UpperLeft);
        body.color = new Color(0.82f, 0.94f, 0.96f, 1f);
        progress = CreateText(panel.transform, "Recovery Progress",
            new Vector2(0.08f, 0.1f), new Vector2(0.58f, 0.25f),
            Vector2.zero, Vector2.zero, "Recovery Progress: 0%", 23, TextAnchor.MiddleLeft);
        progress.color = new Color(0.42f, 0.94f, 0.82f, 1f);
        button = CreateButton(panel.transform, "Start Recovery",
            new Vector2(0.62f, 0.095f), new Vector2(0.92f, 0.245f),
            "START RECOVERY", new Color(0.1f, 0.56f, 0.54f, 1f));
    }

    private static GameObject CreateWorldMarker(Transform parent, string name, Vector3 position)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        TextMesh text = marker.AddComponent<TextMesh>();
        text.text = "!";
        text.fontSize = 80;
        text.characterSize = 0.12f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(1f, 0.82f, 0.18f, 1f);
        marker.GetComponent<MeshRenderer>().sortingOrder = 80;
        marker.SetActive(false);
        return marker;
    }

    private static GameObject CreateProp(Transform parent, string name, Sprite sprite, Vector2 position,
        Vector2 size, Color color, int sortingOrder, bool solid, string label)
    {
        GameObject prop = new GameObject(name);
        prop.transform.SetParent(parent, false);
        prop.transform.position = new Vector3(position.x, position.y, 0f);
        SpriteRenderer renderer = prop.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        Vector2 spriteSize = sprite != null ? sprite.bounds.size : Vector2.one;
        prop.transform.localScale = new Vector3(size.x / Mathf.Max(spriteSize.x, 0.01f),
            size.y / Mathf.Max(spriteSize.y, 0.01f), 1f);

        if (solid)
        {
            BoxCollider2D collider = prop.AddComponent<BoxCollider2D>();
            collider.size = spriteSize;
        }

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(prop.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        labelObject.transform.localScale = new Vector3(
            Mathf.Max(spriteSize.x, 0.01f) / size.x,
            Mathf.Max(spriteSize.y, 0.01f) / size.y,
            1f);
        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.text = label;
        text.fontSize = 38;
        text.characterSize = 0.055f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(0.92f, 0.88f, 0.78f, 0.95f);
        labelObject.GetComponent<MeshRenderer>().sortingOrder = sortingOrder + 1;
        return prop;
    }

    private static AudioSource CreateAudioSource(Transform parent, string name, AudioClip clip, bool loop, float volume)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.volume = volume;
        source.spatialBlend = 0f;
        return source;
    }

    private static AudioClip LoadAudio(string fileName)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDirectory + fileName);
        if (clip == null)
            Debug.LogWarning($"[Control S] Audio is not imported yet: {fileName}");
        return clip;
    }

    /// <summary>
    /// 말풍선을 Screen Space - Overlay로 올린다. 월드 공간 캔버스는 sortingOrder와 무관하게
    /// Overlay UI(데스크톱, 정면샷 창) 뒤에 깔려서 데스크톱 구간 대사가 안 보였다.
    /// </summary>
    private static void ConfigureSpeechBubbleOverlay(SpeechBubbleController speechBubble)
    {
        GameObject bubbleObject = speechBubble.gameObject;

        Canvas canvas = GetOrAdd<Canvas>(bubbleObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // HUD 80, 정면샷 95, 데스크톱보다 위
        canvas.overrideSorting = false;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(bubbleObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Overlay 루트는 화면 전체를 덮으므로 트랜스폼을 중립으로 되돌린다.
        bubbleObject.transform.localPosition = Vector3.zero;
        bubbleObject.transform.localRotation = Quaternion.identity;
        bubbleObject.transform.localScale = Vector3.one;

        foreach (Text bubbleText in speechBubble.GetComponentsInChildren<Text>(true))
            bubbleText.font = KoreanFont;

        RectTransform bubbleRoot = null;
        foreach (Image bubbleBackground in speechBubble.GetComponentsInChildren<Image>(true))
        {
            if (bubbleBackground.GetComponentInChildren<Text>(true) == null)
                continue;

            bubbleBackground.color = new Color(0.035f, 0.055f, 0.07f, 0.94f);
            bubbleBackground.raycastTarget = false;

            // 말풍선 본체는 화면 좌표로 직접 옮기므로 앵커를 한 점으로 고정한다.
            bubbleRoot = (RectTransform)bubbleBackground.transform;
            bubbleRoot.anchorMin = bubbleRoot.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRoot.pivot = new Vector2(0.5f, 0f);
            if (bubbleRoot.sizeDelta.x < 1f || bubbleRoot.sizeDelta.y < 1f)
                bubbleRoot.sizeDelta = new Vector2(460f, 150f);
            break;
        }

        SerializedObject serialized = new SerializedObject(speechBubble);
        serialized.FindProperty("_bubbleRoot").objectReferenceValue = bubbleRoot;
        serialized.FindProperty("_worldHeadOffset").vector2Value = new Vector2(0f, 1.5f);
        // 데스크톱이 열리면 우하단 일러(Seated Character: 앵커 0.98/0.03, 높이 270) 바로 위로 붙인다.
        serialized.FindProperty("_desktopViewportAnchor").vector2Value = new Vector2(0.965f, 0.29f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }


    // Unity 기본 LegacyRuntime.ttf(Arial)에는 한글 글리프가 없어서 네모로만 나온다.
    // OS 한글 폰트를 동적으로 받아 쓴다. 실패하면 기본 폰트로 되돌린다.
    private static Font _koreanFont;
    private static Font KoreanFont
    {
        get
        {
            if (_koreanFont != null)
                return _koreanFont;

            string[] candidates = { "Malgun Gothic", "맑은 고딕", "NanumGothic", "Gulim", "Dotum", "AppleGothic" };
            foreach (string name in candidates)
            {
                Font found = Font.CreateDynamicFontFromOSFont(name, 32);
                if (found != null)
                {
                    _koreanFont = found;
                    return _koreanFont;
                }
            }

            _koreanFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _koreanFont;
        }
    }

    private static GameObject CreateScreenCanvas(Scene scene, string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        ConfigureScreenCanvas(canvasObject, sortingOrder);
        return canvasObject;
    }

    private static void ConfigureScreenCanvas(GameObject canvasObject, int sortingOrder)
    {
        canvasObject.transform.localScale = Vector3.one;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static Image CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        Image image = CreateImage(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero, color);
        image.raycastTarget = false;
        return image;
    }

    private static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, string value, int fontSize, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = KoreanFont;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        string label, Color color)
    {
        Image image = CreateImage(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero, color);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text text = CreateText(image.transform, "Label", Vector2.zero, Vector2.one,
            new Vector2(8f, 4f), new Vector2(-8f, -4f), label, 22, TextAnchor.MiddleCenter);
        text.color = Color.white;
        return button;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private static GameObject RecreateRoot(Scene scene, string name)
    {
        GameObject existing = FindByName(scene, name);
        if (existing != null)
            Object.DestroyImmediate(existing);
        GameObject root = new GameObject(name);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static void RemoveMissingScripts(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T existing = gameObject.GetComponent<T>();
        return existing != null ? existing : gameObject.AddComponent<T>();
    }

    private static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }
        return null;
    }

    private static void DestroyChildrenNamed(Transform parent, params string[] names)
    {
        HashSet<string> targets = new HashSet<string>(names);
        List<GameObject> matches = parent.GetComponentsInChildren<Transform>(true)
            .Where(child => child != parent && targets.Contains(child.name))
            .Select(child => child.gameObject)
            .ToList();
        foreach (GameObject match in matches)
            Object.DestroyImmediate(match);
    }

    private static GameObject FindByName(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindChild(root.transform, name);
            if (result != null)
                return result.gameObject;
        }
        return null;
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        return FindComponents<T>(scene).FirstOrDefault();
    }

    private static T[] FindComponents<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .ToArray();
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {target.GetType().Name}.");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectArray(SerializedProperty property, IEnumerable<Object> values)
    {
        Object[] objects = values.ToArray();
        property.arraySize = objects.Length;
        for (int index = 0; index < objects.Length; index++)
            property.GetArrayElementAtIndex(index).objectReferenceValue = objects[index];
    }

    private static void EnsureBuildScene()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        int index = scenes.FindIndex(item => item.path == ScenePath);
        if (index < 0)
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        else
            scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
