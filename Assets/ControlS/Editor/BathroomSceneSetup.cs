using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// 화장실 씬을 완성 형태로 생성한다: 배경 + 카메라 + 플레이어 + 방으로 돌아가는 문.
/// 그리고 방 씬(SampleScene)에 화장실로 들어가는 문을 추가한다.
/// </summary>
public static class BathroomSceneSetup
{
    private const string BathroomScenePath = "Assets/Scenes/Bathroom.unity";
    private const string RoomScenePath = "Assets/Scenes/SampleScene.unity";
    private const string BathroomMapPath = "Assets/ControlS/Resources/Arts/BathroomMap.png";
    private const string PlayerPrefabPath = "Assets/ControlS/Resources/Pefabs/Player.prefab";

    [MenuItem("Control S/Setup/Configure Bathroom Scene")]
    public static void ConfigureBathroomScene()
    {
        Sprite map = ImportAsSprite(BathroomMapPath);
        if (map == null)
            throw new InvalidOperationException($"화장실 배경 아트가 없다: {BathroomMapPath}");

        BuildBathroomScene(map);
        AddRoomEntrance();

        AssetDatabase.SaveAssets();
        Debug.Log("[Control S] Bathroom scene created (배경+문+플레이어), 방 씬에 입구 문 추가 완료.");
    }

    private static void BuildBathroomScene(Sprite map)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        // 카메라
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5.1f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.tag = "MainCamera";

        // EventSystem (입력)
        GameObject eventSystem = new GameObject("EventSystem",
            typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, scene);

        // 배경
        GameObject bg = new GameObject("Bathroom Background", typeof(SpriteRenderer));
        SceneManager.MoveGameObjectToScene(bg, scene);
        SpriteRenderer renderer = bg.GetComponent<SpriteRenderer>();
        renderer.sprite = map;
        renderer.sortingOrder = -100;
        float scale = 10.2f / Mathf.Max(map.bounds.size.y, 0.01f);
        bg.transform.localScale = Vector3.one * scale;

        // 플레이어
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab != null)
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = new Vector3(0f, -2.5f, 0f);
        }

        // 방으로 돌아가는 문 (화장실 아래쪽). 화장실 씬을 언로드하고 방 뷰를 켠다.
        CreateDoor(scene, "Door - To Room", new Vector2(0f, -4.2f), SceneDoor.DoorMode.CloseOverlay, "SampleScene");

        EditorSceneManager.SaveScene(scene, BathroomScenePath);
        EditorSceneManager.CloseScene(scene, true);
        RegisterBuildScene(BathroomScenePath);
    }

    // 방 씬에 화장실 입구 문을 추가한다.
    private static void AddRoomEntrance()
    {
        Scene scene = SceneManager.GetSceneByPath(RoomScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Additive);

        GameObject existing = FindByName(scene, "Door - To Bathroom");
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);

        // 현관/신발장 근처를 화장실 입구로 삼는다. 화장실을 겹쳐 올리고 방 뷰를 끈다.
        CreateDoor(scene, "Door - To Bathroom", new Vector2(-6.4f, -3.3f), SceneDoor.DoorMode.OpenOverlay, "Bathroom");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedHere)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static void CreateDoor(Scene scene, string name, Vector2 pos, SceneDoor.DoorMode mode, string targetScene)
    {
        GameObject door = new GameObject(name, typeof(BoxCollider2D), typeof(SceneDoor));
        SceneManager.MoveGameObjectToScene(door, scene);
        door.transform.position = new Vector3(pos.x, pos.y, 0f);

        BoxCollider2D box = door.GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.4f, 1.4f);

        door.GetComponent<SceneDoor>().EditorConfigure(mode, targetScene);
    }

    private static void RegisterBuildScene(string path)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == path))
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
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

    private static Sprite ImportAsSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return null;

        if (importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
