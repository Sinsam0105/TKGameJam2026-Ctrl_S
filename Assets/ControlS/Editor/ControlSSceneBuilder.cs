#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ControlS.Editor
{
    public static class ControlSSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string RootName = "CONTROL S - Scene Root";
        private const string WhitePixelPath = "Assets/ControlS/Resources/ControlS/WhitePixel.png";

        [MenuItem("Tools/CONTROL S/Rebuild SampleScene")]
        public static void RebuildSampleScene()
        {
            EnsureWhitePixelAsset();
            RuntimeUI.ResetCachedAssets();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);
            var oldEventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (oldEventSystem != null) Object.DestroyImmediate(oldEventSystem.gameObject);

            var camera = ConfigureCamera();
            var root = new GameObject(RootName);

            var gameObject = new GameObject("Game Controller");
            gameObject.transform.SetParent(root.transform, false);
            var controller = gameObject.AddComponent<ControlSSceneController>();
            var drone = gameObject.AddComponent<AudioSource>();
            var uiAudio = gameObject.AddComponent<AudioSource>();

            var room = BuildRoom(root.transform);
            var hud = BuildHud(root.transform);

            var desktopObject = new GameObject("Virtual Desktop System");
            desktopObject.transform.SetParent(root.transform, false);
            var desktop = desktopObject.AddComponent<VirtualDesktop>();
            desktop.BuildSceneLayout();

            BuildEventSystem(root.transform);
            controller.ConfigureSceneReferences(room, desktop, camera, hud.Canvas, hud.Objective,
                hud.Prompt, hud.Narration, hud.NarrationPanel, hud.ModalLayer,
                hud.GlitchOverlay, hud.GlitchStripes, drone, uiAudio);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(room);
            EditorUtility.SetDirty(desktop);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[CONTROL S] SampleScene rebuilt as a scene-authored hierarchy.");
        }

        private static Camera ConfigureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }
            camera.orthographic = true;
            camera.orthographicSize = 5.1f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.transform.rotation = Quaternion.identity;
            camera.backgroundColor = new Color(.012f, .016f, .021f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            EditorUtility.SetDirty(camera);
            return camera;
        }

        private static RoomSceneView BuildRoom(Transform parent)
        {
            var roomRoot = new GameObject("Room");
            roomRoot.transform.SetParent(parent, false);
            var view = roomRoot.AddComponent<RoomSceneView>();

            var floor = Box(roomRoot.transform, "Floor", Vector2.zero, new Vector2(15.8f, 8.8f),
                new Color(.075f, .09f, .105f), -20);
            Box(roomRoot.transform, "Rug", new Vector2(0, -.75f), new Vector2(6.4f, 3.8f),
                new Color(.12f, .135f, .145f), -18);
            AddWall(roomRoot.transform, new Vector2(0, 4.52f), new Vector2(16.4f, .45f));
            AddWall(roomRoot.transform, new Vector2(0, -4.52f), new Vector2(16.4f, .45f));
            AddWall(roomRoot.transform, new Vector2(-8.12f, 0), new Vector2(.45f, 9.4f));
            AddWall(roomRoot.transform, new Vector2(8.12f, 0), new Vector2(.45f, 9.4f));

            var deskGroup = Group(roomRoot.transform, "Desk Area");
            Box(deskGroup, "Desk", new Vector2(-4.65f, 2.75f), new Vector2(3.25f, 1.15f),
                new Color(.19f, .14f, .12f), -2, true);
            Box(deskGroup, "Desk shadow", new Vector2(-4.65f, 2.15f), new Vector2(3.15f, .25f),
                new Color(.025f, .03f, .035f, .7f), -3);
            var computer = Box(deskGroup, "Computer", new Vector2(-4.65f, 2.82f), new Vector2(1.65f, .82f),
                new Color(.08f, .12f, .135f), 1);
            Box(deskGroup, "Monitor screen", new Vector2(-4.65f, 2.86f), new Vector2(1.4f, .56f),
                new Color(.12f, .31f, .31f), 2);
            var pcInteraction = computer.gameObject.AddComponent<RoomInteractable>();
            pcInteraction.Configure(RoomInteractionKind.Computer, "컴퓨터 사용", 1.75f);

            var clockGroup = Group(roomRoot.transform, "Clock Area");
            var clock = Box(clockGroup, "Stopped Clock", new Vector2(.2f, 3.55f), new Vector2(1.18f, 1.18f),
                new Color(.62f, .6f, .52f), 0);
            Box(clockGroup, "Clock face", new Vector2(.2f, 3.55f), new Vector2(.96f, .96f),
                new Color(.13f, .145f, .15f), 1);
            var hand = Box(clockGroup, "Clock hand", new Vector2(.2f, 3.75f), new Vector2(.055f, .42f),
                new Color(.76f, .18f, .17f), 2);
            hand.transform.rotation = Quaternion.Euler(0, 0, -35f);
            var clockInteraction = clock.gameObject.AddComponent<RoomInteractable>();
            clockInteraction.Configure(RoomInteractionKind.Clock, "멈춘 시계 조사", 1.55f);

            var cabinetGroup = Group(roomRoot.transform, "Cabinet Area");
            Box(cabinetGroup, "Cabinet", new Vector2(5.05f, 1.75f), new Vector2(2.35f, 2.2f),
                new Color(.22f, .165f, .13f), -1, true);
            var drawer = Box(cabinetGroup, "Locked Drawer", new Vector2(5.05f, 1.85f), new Vector2(2.05f, .78f),
                new Color(.31f, .22f, .16f), 1);
            Box(cabinetGroup, "Drawer handle", new Vector2(5.05f, 1.8f), new Vector2(.55f, .08f),
                new Color(.68f, .62f, .49f), 2);
            var drawerInteraction = drawer.gameObject.AddComponent<RoomInteractable>();
            drawerInteraction.Configure(RoomInteractionKind.Drawer, "잠긴 서랍 확인", 1.7f);

            var bedGroup = Group(roomRoot.transform, "Bed Area");
            Box(bedGroup, "Bed", new Vector2(4.8f, -2.6f), new Vector2(4.55f, 1.9f),
                new Color(.22f, .24f, .25f), -1, true);
            Box(bedGroup, "Blanket", new Vector2(4.75f, -2.5f), new Vector2(4.15f, 1.48f),
                new Color(.17f, .255f, .27f), 0);
            Box(bedGroup, "Under-bed shadow", new Vector2(4.65f, -3.62f), new Vector2(3.3f, .24f),
                new Color(.005f, .005f, .008f, .95f), 1);

            var door = Box(roomRoot.transform, "Door", new Vector2(0, 3.95f), new Vector2(1.85f, 1f),
                new Color(.18f, .15f, .14f), -1);
            var doorInteraction = door.gameObject.AddComponent<RoomInteractable>();
            doorInteraction.Configure(RoomInteractionKind.Door, "문 열기", 1.35f);

            var lampGroup = Group(roomRoot.transform, "Lamp Area");
            Box(lampGroup, "Lamp pole", new Vector2(-6.55f, -.7f), new Vector2(.12f, 2.2f),
                new Color(.38f, .36f, .3f), 0);
            Box(lampGroup, "Lamp shade", new Vector2(-6.55f, .42f), new Vector2(1.05f, .52f),
                new Color(.64f, .55f, .35f), 2);
            var glow = Box(lampGroup, "Lamp glow", new Vector2(-6.55f, -.35f), new Vector2(2.5f, 2.65f),
                new Color(.58f, .5f, .28f, .62f), -8);

            var photo = new GameObject("Restored Family Photo");
            photo.transform.SetParent(roomRoot.transform, false);
            photo.transform.position = new Vector3(6.25f, 3.55f, 0);
            photo.transform.localScale = new Vector3(1.18f, 1f, 1f);
            var photoRenderer = photo.AddComponent<SpriteRenderer>();
            photoRenderer.sprite = RuntimeUI.WhiteSprite;
            photoRenderer.color = new Color(.41f, .34f, .28f);
            photoRenderer.sortingOrder = 2;
            var inner = new GameObject("Erased figures");
            inner.transform.SetParent(photo.transform, false);
            inner.transform.localScale = new Vector3(.76f, .72f, 1f);
            var innerRenderer = inner.AddComponent<SpriteRenderer>();
            innerRenderer.sprite = RuntimeUI.WhiteSprite;
            innerRenderer.color = new Color(.12f, .135f, .14f);
            innerRenderer.sortingOrder = 3;
            var photoInteraction = photo.AddComponent<RoomInteractable>();
            photoInteraction.Configure(RoomInteractionKind.Photo, "복원된 사진 조사", 1.55f);

            var player = BuildPlayer(roomRoot.transform);
            view.Configure(floor, new[] { glow }, photo, player);
            photo.SetActive(false);
            return view;
        }

        private static TopDownPlayer BuildPlayer(Transform parent)
        {
            var playerRoot = new GameObject("Player");
            playerRoot.transform.SetParent(parent, false);
            playerRoot.transform.position = new Vector3(0, -2.1f, 0);

            var shadow = Box(playerRoot.transform, "Shadow", new Vector2(0, -.42f), new Vector2(.7f, .24f),
                new Color(0, 0, 0, .45f), 4, false, true);
            shadow.transform.localPosition = new Vector3(0, -.42f, 0);
            var body = Box(playerRoot.transform, "Body", Vector2.zero, new Vector2(.56f, .8f),
                new Color(.54f, .64f, .61f), 6, false, true);
            body.transform.localPosition = Vector3.zero;
            var head = Box(playerRoot.transform, "Head", new Vector2(0, .48f), new Vector2(.48f, .4f),
                new Color(.68f, .61f, .54f), 7, false, true);
            head.transform.localPosition = new Vector3(0, .48f, 0);

            var rigidbody = playerRoot.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0;
            rigidbody.freezeRotation = true;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = playerRoot.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(.58f, .88f);
            collider.offset = new Vector2(0, -.04f);
            return playerRoot.AddComponent<TopDownPlayer>();
        }

        private static HudReferences BuildHud(Transform parent)
        {
            var canvasObject = new GameObject("Room HUD Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            var top = RuntimeUI.Panel("Top shade", canvas.transform, new Color(0, 0, 0, .42f),
                new Vector2(0, .88f), Vector2.one, Vector2.zero, Vector2.zero);
            top.raycastTarget = false;
            var bottom = RuntimeUI.Panel("Bottom shade", canvas.transform, new Color(0, 0, 0, .5f),
                Vector2.zero, new Vector2(1, .13f), Vector2.zero, Vector2.zero);
            bottom.raycastTarget = false;
            var left = RuntimeUI.Panel("Left shade", canvas.transform, new Color(0, 0, 0, .25f),
                Vector2.zero, new Vector2(.055f, 1), Vector2.zero, Vector2.zero);
            left.raycastTarget = false;
            var right = RuntimeUI.Panel("Right shade", canvas.transform, new Color(0, 0, 0, .25f),
                new Vector2(.945f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            right.raycastTarget = false;

            var objectiveBar = RuntimeUI.Panel("Objective", canvas.transform, new Color(.02f, .026f, .032f, .9f),
                new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-660, -90), new Vector2(660, -20));
            var objective = RuntimeUI.Text("Text", objectiveBar.transform, "복구 진행 0/4", 24,
                new Color(.72f, .86f, .82f), TextAnchor.MiddleCenter);
            var controls = RuntimeUI.Text("Controls", canvas.transform,
                "WASD / 방향키  이동    E / Enter  조사    ESC  컴퓨터 닫기", 18,
                new Color(.61f, .67f, .66f), TextAnchor.UpperLeft);
            controls.rectTransform.anchorMin = controls.rectTransform.anchorMax = new Vector2(0, 1);
            controls.rectTransform.pivot = new Vector2(0, 1);
            controls.rectTransform.anchoredPosition = new Vector2(28, -22);
            controls.rectTransform.sizeDelta = new Vector2(700, 40);

            var prompt = RuntimeUI.Text("Interaction Prompt", canvas.transform, string.Empty, 28,
                new Color(.91f, .87f, .67f), TextAnchor.MiddleCenter);
            prompt.fontStyle = FontStyle.Bold;
            prompt.rectTransform.anchorMin = prompt.rectTransform.anchorMax = new Vector2(.5f, 0);
            prompt.rectTransform.pivot = new Vector2(.5f, 0);
            prompt.rectTransform.anchoredPosition = new Vector2(0, 38);
            prompt.rectTransform.sizeDelta = new Vector2(900, 52);

            var narrationPanel = RuntimeUI.Panel("Narration", canvas.transform, new Color(.018f, .021f, .025f, .94f),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-650, -365), new Vector2(650, -190));
            var narration = RuntimeUI.Text("Narration Text", narrationPanel.transform, string.Empty, 27,
                new Color(.88f, .9f, .86f), TextAnchor.MiddleCenter);
            narration.rectTransform.offsetMin = new Vector2(28, 14);
            narration.rectTransform.offsetMax = new Vector2(-28, -14);
            narrationPanel.gameObject.SetActive(false);

            var modal = RuntimeUI.Rect("Modal Layer", canvas.transform);
            RuntimeUI.Stretch(modal);
            modal.gameObject.SetActive(false);
            var glitch = RuntimeUI.Panel("Glitch Overlay", canvas.transform, new Color(.32f, 0, 0, 0),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            glitch.raycastTarget = false;
            var stripes = RuntimeUI.Rect("Glitch Stripes", glitch.transform);
            RuntimeUI.Stretch(stripes);
            for (var i = 0; i < 13; i++)
            {
                var stripe = RuntimeUI.Panel("Noise " + i, stripes,
                    i % 3 == 0 ? new Color(.6f, .03f, .025f, .5f) : new Color(.72f, .78f, .75f, .16f),
                    Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 8));
                stripe.raycastTarget = false;
                stripe.gameObject.SetActive(false);
            }

            return new HudReferences
            {
                Canvas = canvas,
                Objective = objective,
                Prompt = prompt,
                Narration = narration,
                NarrationPanel = narrationPanel,
                ModalLayer = modal,
                GlitchOverlay = glitch,
                GlitchStripes = stripes
            };
        }

        private static void BuildEventSystem(Transform parent)
        {
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystemObject.transform.SetParent(parent, false);
            var inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static SpriteRenderer Box(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, int order, bool collider = false, bool localPosition = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (localPosition) go.transform.localPosition = new Vector3(position.x, position.y, 0);
            else go.transform.position = new Vector3(position.x, position.y, 0);
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeUI.WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (collider) go.AddComponent<BoxCollider2D>();
            return renderer;
        }

        private static void AddWall(Transform parent, Vector2 position, Vector2 size)
        {
            Box(parent, "Wall", position, size, new Color(.16f, .17f, .18f), -5, true);
        }

        private static void EnsureWhitePixelAsset()
        {
            var directory = Path.GetDirectoryName(WhitePixelPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (!File.Exists(WhitePixelPath))
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                File.WriteAllBytes(WhitePixelPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(WhitePixelPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(WhitePixelPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private sealed class HudReferences
        {
            public Canvas Canvas;
            public Text Objective;
            public Text Prompt;
            public Text Narration;
            public Image NarrationPanel;
            public RectTransform ModalLayer;
            public Image GlitchOverlay;
            public RectTransform GlitchStripes;
        }
    }
}
#endif
