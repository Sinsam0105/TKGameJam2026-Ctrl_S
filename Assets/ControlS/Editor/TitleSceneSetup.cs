using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 타이틀 씬을 생성하고 빌드 세팅의 첫 씬으로 등록한다.
/// 흰 박스 UI라 아트가 배경/로고를 얹으면 된다.
/// </summary>
public static class TitleSceneSetup
{
    private const string TitleScenePath = "Assets/Scenes/Title.unity";
    private const string GameScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Control S/Setup/Configure Title Scene")]
    public static void ConfigureTitleScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        // 카메라
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
        camera.orthographic = true;
        cameraObject.tag = "MainCamera";

        // EventSystem
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, scene);

        // Canvas
        GameObject canvasObject = new GameObject("Title Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 배경 (아트 자리)
        Image bg = CreateImage(canvasObject.transform, "Background", Vector2.zero, Vector2.one,
            new Color(0.05f, 0.06f, 0.09f, 1f));
        bg.raycastTarget = false;

        // 타이틀 로고 (텍스트 자리)
        CreateText(canvasObject.transform, "Title", new Vector2(0.2f, 0.58f), new Vector2(0.8f, 0.78f),
            "Ctrl + S", 96, TextAnchor.MiddleCenter, new Color(0.92f, 0.94f, 0.98f, 1f));
        CreateText(canvasObject.transform, "Subtitle", new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.57f),
            "— 03:05, 저장되지 않은 —", 26, TextAnchor.MiddleCenter, new Color(0.6f, 0.65f, 0.75f, 1f));

        // 버튼
        Button start = CreateButton(canvasObject.transform, "StartButton",
            new Vector2(0.4f, 0.34f), new Vector2(0.6f, 0.41f), "시작", new Color(0.18f, 0.42f, 0.6f, 1f));
        Button quit = CreateButton(canvasObject.transform, "QuitButton",
            new Vector2(0.4f, 0.24f), new Vector2(0.6f, 0.31f), "종료", new Color(0.4f, 0.2f, 0.26f, 1f));

        // 컨트롤러
        GameObject controller = new GameObject("Title Controller");
        SceneManager.MoveGameObjectToScene(controller, scene);
        TitleScreen title = controller.AddComponent<TitleScreen>();
        SerializedObject so = new SerializedObject(title);
        so.FindProperty("gameSceneName").stringValue = "SampleScene";
        so.FindProperty("startButton").objectReferenceValue = start;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, TitleScenePath);
        EditorSceneManager.CloseScene(scene, true);

        RegisterBuildScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[Control S] Title scene created and registered as the first build scene.");
    }

    // 타이틀을 0번, 본편을 1번으로 등록한다.
    private static void RegisterBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TitleScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
    }

    // ── 헬퍼 ────────────────────────────────────────────────
    private static Font _koreanFont;
    private static Font KoreanFont
    {
        get
        {
            if (_koreanFont != null)
                return _koreanFont;
            foreach (string n in new[] { "Malgun Gothic", "맑은 고딕", "NanumGothic", "Gulim", "Dotum", "AppleGothic" })
            {
                Font f = Font.CreateDynamicFontFromOSFont(n, 48);
                if (f != null) { _koreanFont = f; return f; }
            }
            _koreanFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _koreanFont;
        }
    }

    private static Image CreateImage(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        RectTransform rect = CreateRect(parent, name, min, max);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(Transform parent, string name, Vector2 min, Vector2 max,
        string value, int fontSize, TextAnchor alignment, Color color)
    {
        RectTransform rect = CreateRect(parent, name, min, max);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = KoreanFont;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 min, Vector2 max,
        string label, Color color)
    {
        Image image = CreateImage(parent, name, min, max, color);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        CreateText(image.transform, "Label", Vector2.zero, Vector2.one, label, 28,
            TextAnchor.MiddleCenter, Color.white);
        return button;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return rect;
    }
}
