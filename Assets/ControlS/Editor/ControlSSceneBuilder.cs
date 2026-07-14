#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
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
        private const string ContentPath = "Assets/ControlS/Content/DefaultControlSContent.asset";
        private const string NarrationDirectory = "Assets/ControlS/Narrations";

        [MenuItem("Tools/CONTROL S/Rebuild SampleScene")]
        public static void RebuildSampleScene()
        {
            EnsureWhitePixelAsset();
            var content = EnsureContentAsset();
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
            var atmosphere = gameObject.AddComponent<ControlSAtmosphereController>();
            var drawerContent = gameObject.AddComponent<DrawerKeypadContent>();
            var introSequence = gameObject.AddComponent<InteractionSequence>();
            var endingSequence = gameObject.AddComponent<InteractionSequence>();
            var drone = gameObject.AddComponent<AudioSource>();
            var uiAudio = gameObject.AddComponent<AudioSource>();

            var room = BuildRoom(root.transform);
            var hud = BuildHud(root.transform, content);

            var desktopObject = new GameObject("Virtual Desktop System");
            desktopObject.transform.SetParent(root.transform, false);
            var desktop = desktopObject.AddComponent<VirtualDesktop>();
            desktop.BuildSceneLayout(content);

            BuildEventSystem(root.transform);
            hud.Controller.Configure(hud.Canvas, hud.Objective, hud.Prompt, hud.Narration,
                hud.NarrationPanel, hud.GlitchOverlay, hud.GlitchStripes);
            atmosphere.Configure(camera, drone, uiAudio);
            drawerContent.Configure(hud.DrawerRoot, hud.DrawerInput, hud.DrawerHint);
            introSequence.Configure("Intro", true, new InteractionAction[]
            {
                new DelayAction(.35f),
                new ShowNarrationAction(content.story.intro)
            });
            endingSequence.Configure("Ending", false, new InteractionAction[]
            {
                new SetControlSFlagAction(ControlSFlag.EndingStarted),
                new CloseDesktopAction(),
                new HideCurrentUIAction(),
                new SetPlayerInputAction(false),
                new RequestGlitchAction(1f),
                new DelayAction(1.05f),
                new ShowNarrationAction(content.story.ending25, true, .3f),
                new RequestGlitchAction(.8f),
                new ShowNarrationAction(content.story.ending74, true, .3f),
                new RequestGlitchAction(1f),
                new DelayAction(.7f),
                new ShowInteractionUIAction(hud.EndingRoot)
            });
            desktop.ConfigureEndingSequence(endingSequence);
            controller.ConfigureSceneReferences(content, room, desktop, hud.Controller, atmosphere,
                drawerContent, new[] { introSequence, endingSequence });
            BindRoomInteractionRules(room, drawerContent, desktop, hud, content);
            UnityEventTools.AddPersistentListener(hud.DrawerSubmit.onClick, drawerContent.Submit);
            UnityEventTools.AddPersistentListener(hud.DrawerCancel.onClick, drawerContent.Close);
            UnityEventTools.AddPersistentListener(hud.EndingRestart.onClick, controller.RestartScene);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(hud.Controller);
            EditorUtility.SetDirty(atmosphere);
            EditorUtility.SetDirty(drawerContent);
            EditorUtility.SetDirty(introSequence);
            EditorUtility.SetDirty(endingSequence);
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

        private static HudReferences BuildHud(Transform parent, ControlSContent content)
        {
            var canvasObject = new GameObject("Room HUD Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            var hudController = canvasObject.AddComponent<ControlSHudController>();
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

            var drawerRoot = RuntimeUI.Panel("Drawer Keypad UI", canvas.transform, new Color(0, 0, 0, .72f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var keypad = RuntimeUI.Panel("Keypad", drawerRoot.transform, new Color(.045f, .052f, .058f, 1f),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-330, -220), new Vector2(330, 220));
            var keypadOutline = keypad.gameObject.AddComponent<Outline>();
            keypadOutline.effectColor = new Color(.4f, .25f, .17f, 1f);
            keypadOutline.effectDistance = new Vector2(3, -3);
            var keypadTitle = RuntimeUI.Text("Title", keypad.transform, content.hud.keypadTitle, 28,
                new Color(.88f, .82f, .72f), TextAnchor.UpperCenter);
            keypadTitle.rectTransform.offsetMin = new Vector2(20, 300);
            keypadTitle.rectTransform.offsetMax = new Vector2(-20, -24);
            var drawerHint = RuntimeUI.Text("Hint", keypad.transform, content.hud.keypadHintMissing, 22,
                new Color(.62f, .65f, .63f), TextAnchor.MiddleCenter);
            drawerHint.rectTransform.offsetMin = new Vector2(20, 175);
            drawerHint.rectTransform.offsetMax = new Vector2(-20, -105);
            var drawerInput = RuntimeUI.Input("Code", keypad.transform, content.hud.keypadPlaceholder);
            RuntimeUI.Place(drawerInput.GetComponent<RectTransform>(), 0, 8, 360, 68);
            var drawerSubmit = RuntimeUI.Button("Submit", keypad.transform, content.hud.keypadSubmit, null,
                new Color(.31f, .2f, .12f, 1f), new Color(.55f, .34f, .17f, 1f), 23);
            RuntimeUI.Place(drawerSubmit.GetComponent<RectTransform>(), -105, -105, 250, 60);
            var drawerCancel = RuntimeUI.Button("Cancel", keypad.transform, content.hud.close, null,
                new Color(.16f, .17f, .18f, 1f), new Color(.28f, .3f, .31f, 1f), 22);
            RuntimeUI.Place(drawerCancel.GetComponent<RectTransform>(), 190, -105, 170, 60);
            drawerRoot.gameObject.SetActive(false);

            var endingRoot = RuntimeUI.Panel("Ending UI", canvas.transform,
                new Color(.005f, .006f, .008f, .985f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var endingTitle = RuntimeUI.Text("Title", endingRoot.transform, content.hud.endingTitle, 82,
                new Color(.68f, .08f, .065f), TextAnchor.MiddleCenter);
            endingTitle.fontStyle = FontStyle.Bold;
            endingTitle.rectTransform.anchorMin = new Vector2(.15f, .55f);
            endingTitle.rectTransform.anchorMax = new Vector2(.85f, .82f);
            endingTitle.rectTransform.offsetMin = endingTitle.rectTransform.offsetMax = Vector2.zero;
            var endingBody = RuntimeUI.Text("Body", endingRoot.transform, content.hud.endingBody, 30,
                new Color(.78f, .78f, .74f), TextAnchor.MiddleCenter);
            endingBody.rectTransform.anchorMin = new Vector2(.12f, .25f);
            endingBody.rectTransform.anchorMax = new Vector2(.88f, .58f);
            endingBody.rectTransform.offsetMin = endingBody.rectTransform.offsetMax = Vector2.zero;
            var endingRestart = RuntimeUI.Button("Restart", endingRoot.transform, content.hud.restart, null,
                new Color(.18f, .035f, .035f, 1f), new Color(.48f, .07f, .055f, 1f), 23);
            var restartRect = endingRestart.GetComponent<RectTransform>();
            restartRect.anchorMin = restartRect.anchorMax = new Vector2(.5f, .14f);
            restartRect.sizeDelta = new Vector2(340, 64);
            restartRect.anchoredPosition = Vector2.zero;
            endingRoot.gameObject.SetActive(false);
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
                Controller = hudController,
                Canvas = canvas,
                Objective = objective,
                Prompt = prompt,
                Narration = narration,
                NarrationPanel = narrationPanel,
                DrawerRoot = drawerRoot.gameObject,
                DrawerInput = drawerInput,
                DrawerHint = drawerHint,
                DrawerSubmit = drawerSubmit,
                DrawerCancel = drawerCancel,
                EndingRoot = endingRoot.gameObject,
                EndingTitle = endingTitle,
                EndingBody = endingBody,
                EndingRestart = endingRestart,
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

        private static void BindRoomInteractionRules(RoomSceneView room, DrawerKeypadContent drawerContent,
            VirtualDesktop desktop, HudReferences hud, ControlSContent content)
        {
            foreach (var interactable in room.GetComponentsInChildren<RoomInteractable>(true))
            {
                switch (interactable.Kind)
                {
                    case RoomInteractionKind.Computer:
                        interactable.ConfigurePayload(null, desktop.DesktopUI);
                        interactable.ConfigureRules(new[]
                        {
                            new InteractionRule("컴퓨터 열기", null,
                                new InteractionAction[] { new OpenDesktopAction() })
                        });
                        break;
                    case RoomInteractionKind.Clock:
                        interactable.ConfigurePayload(content.story.clockFirst, null);
                        interactable.ConfigureRules(new[]
                        {
                            new InteractionRule("처음 조사",
                                new InteractionCondition[]
                                {
                                    new ControlSFlagCondition(ControlSFlag.ClockInspected, false)
                                },
                                new InteractionAction[]
                                {
                                    new ShowNarrationAction(content.story.clockFirst),
                                    new SetControlSFlagAction(ControlSFlag.ClockInspected)
                                }),
                            new InteractionRule("비밀번호 해제 후 키캡 회수",
                                new InteractionCondition[]
                                {
                                    new ControlSFlagCondition(ControlSFlag.TimePasswordSolved, true),
                                    new ControlSFlagCondition(ControlSFlag.KeycapCollected, false)
                                },
                                new InteractionAction[]
                                {
                                    new SetControlSFlagAction(ControlSFlag.KeycapCollected),
                                    new ShowNarrationAction(content.story.keycapFound),
                                    new RequestGlitchAction(.22f)
                                }),
                            new InteractionRule("키캡 회수 후",
                                new InteractionCondition[]
                                {
                                    new ControlSFlagCondition(ControlSFlag.KeycapCollected, true)
                                },
                                new InteractionAction[] { new ShowNarrationAction(content.story.clockAfterKeycap) }),
                            new InteractionRule("비밀번호 힌트", null,
                                new InteractionAction[] { new ShowNarrationAction(content.story.clockHint) })
                        });
                        break;
                    case RoomInteractionKind.Drawer:
                        interactable.ConfigurePayload(null, hud.DrawerRoot);
                        interactable.ConfigureRules(new[]
                        {
                            new InteractionRule("이미 열린 서랍",
                                new InteractionCondition[]
                                {
                                    new ControlSFlagCondition(ControlSFlag.DrawerOpened, true)
                                },
                                new InteractionAction[]
                                {
                                    new ShowNarrationAction(content.story.drawerAlreadyOpened)
                                }),
                            new InteractionRule("키패드 열기", null,
                                new InteractionAction[]
                                {
                                    new OpenDrawerKeypadAction(drawerContent, hud.DrawerRoot)
                                })
                        });
                        break;
                    case RoomInteractionKind.Photo:
                        interactable.ConfigurePayload(content.story.photoFirst, null);
                        interactable.ConfigureRules(new[]
                        {
                            new InteractionRule("복원 후 처음 조사",
                                new InteractionCondition[]
                                {
                                    new ControlSFlagCondition(ControlSFlag.FamilyPhotoRestored, true),
                                    new ControlSFlagCondition(ControlSFlag.PhotoSequenceDiscovered, false)
                                },
                                new InteractionAction[]
                                {
                                    new SetControlSFlagAction(ControlSFlag.PhotoSequenceDiscovered),
                                    new ShowNarrationAction(content.story.photoFirst)
                                }),
                            new InteractionRule("다시 조사",
                                new InteractionCondition[]
                                {
                                    new ControlSFlagCondition(ControlSFlag.FamilyPhotoRestored, true)
                                },
                                new InteractionAction[] { new ShowNarrationAction(content.story.photoAgain) })
                        });
                        break;
                    case RoomInteractionKind.Door:
                        interactable.ConfigurePayload(content.story.doorLocked, null);
                        interactable.ConfigureRules(new[]
                        {
                            new InteractionRule("퍼즐 진행 후",
                                new InteractionCondition[] { new CompletedPuzzleCountCondition(3) },
                                new InteractionAction[] { new ShowNarrationAction(content.story.doorLate) }),
                            new InteractionRule("잠긴 문", null,
                                new InteractionAction[] { new ShowNarrationAction(content.story.doorLocked) })
                        });
                        break;
                }
                EditorUtility.SetDirty(interactable);
            }
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

        private static ControlSContent EnsureContentAsset()
        {
            var content = AssetDatabase.LoadAssetAtPath<ControlSContent>(ContentPath);
            if (content == null)
            {
                var directory = Path.GetDirectoryName(ContentPath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                content = ScriptableObject.CreateInstance<ControlSContent>();
                AssetDatabase.CreateAsset(content, ContentPath);
            }
            EnsureNarrationAssets(content);
            AssetDatabase.SaveAssets();
            return content;
        }

        private static void EnsureNarrationAssets(ControlSContent content)
        {
            if (!Directory.Exists(NarrationDirectory)) Directory.CreateDirectory(NarrationDirectory);
            if (content.story == null) content.story = new ControlSContent.StoryContent();
            var story = content.story;
            story.intro = EnsureNarration("Intro", "intro",
                "CTRL+S\n\n저장하지 못한 파일을 찾기 위해, 마지막으로 작업하던 방에 돌아왔다.", 5.5f);
            story.clockFirst = EnsureNarration("Clock First", "clock_first",
                "멈춘 시계. 02:17에서 초침까지 굳어 있다.\n뒤쪽에는 뭔가 끼어 있지만 지금은 빠지지 않는다.", 5f);
            story.keycapFound = EnsureNarration("Keycap Found", "keycap_found",
                "시계 아래에서 빠진 키캡을 찾았다. 글자는 'S'.\n누가 일부러 여기 숨겨 둔 것 같다.", 5f);
            story.clockAfterKeycap = EnsureNarration("Clock After Keycap", "clock_after_keycap",
                "시계는 여전히 02:17이다. 조금 전보다 째깍거리는 소리가 가까워졌다.", 3.5f);
            story.clockHint = EnsureNarration("Clock Hint", "clock_hint",
                "02:17. 컴퓨터의 저장 실패 시각과 관련이 있을까?", 3.5f);
            story.inspectClockFirst = EnsureNarration("Inspect Clock First", "inspect_clock_first",
                "숫자는 맞는 것 같지만 확신할 근거가 없다. 방을 직접 확인해야 한다.", 3.5f);
            story.timeDenied = EnsureNarration("Time Denied", "time_denied",
                "ACCESS DENIED — 생성 시각 불일치", 2.5f);
            story.timeUnlocked = EnsureNarration("Time Unlocked", "time_unlocked",
                "RECOVERY 잠금이 풀렸다.\nSAVE_?.tmp — 파일명 한 글자가 손상되어 있다.", 4.5f);
            story.missingKeycap = EnsureNarration("Missing Keycap", "missing_keycap",
                "누락된 글자를 먼저 찾아야 한다.", 2.5f);
            story.repairFailed = EnsureNarration("Repair Failed", "repair_failed",
                "복구 실패 — 체크섬과 파일명이 일치하지 않는다.", 2.5f);
            story.repairSuccess = EnsureNarration("Repair Success", "repair_success",
                "SAVE_S.tmp 복원 완료.\n그 순간 방의 전등이 한 번 꺼졌다.", 4f);
            story.photoRevealed = EnsureNarration("Photo Revealed", "photo_revealed",
                "밝기를 올리자 사진 속 서랍에 숫자가 드러났다. 4312.", 4f);
            story.drawerNoClue = EnsureNarration("Drawer No Clue", "drawer_no_clue",
                "네 자리 잠금이다. 아직 단서가 없다.", 2.5f);
            story.drawerWrong = EnsureNarration("Drawer Wrong", "drawer_wrong",
                "서랍 안쪽에서 금속이 걸리는 소리가 났다.", 2f);
            story.drawerOpened = EnsureNarration("Drawer Opened", "drawer_opened",
                "서랍 속 메모: '휴지통에서 FAMILY.PNG만 복원할 것.'\n문장 아래에는 낯선 필체로 '나를 복원하지 마'라고 쓰여 있다.", 6f);
            story.drawerAlreadyOpened = EnsureNarration("Drawer Already Opened", "drawer_already_opened",
                "서랍 안에는 메모 자국만 남아 있다.\nFAMILY.PNG만 복원할 것.", 3.5f);
            story.restoreNoClue = EnsureNarration("Restore No Clue", "restore_no_clue",
                "무엇을 복원해야 할지 판단할 단서가 없다.", 2.5f);
            story.restoreWrong = EnsureNarration("Restore Wrong", "restore_wrong",
                "복원 실패. 파일 안쪽에서 누군가 문을 두드리는 소리가 난다.", 3f);
            story.restoreSuccess = EnsureNarration("Restore Success", "restore_success",
                "FAMILY.PNG 복원 완료.\n컴퓨터 밖, 비어 있던 벽에 액자가 생겼다.", 4.5f);
            story.photoFirst = EnsureNarration("Photo First", "photo_first",
                "사진 속 가족들의 얼굴은 모두 지워져 있다.\n뒷면에 적힌 실행 순서: 3 → 1 → 4 → 2", 5.5f);
            story.photoAgain = EnsureNarration("Photo Again", "photo_again",
                "액자 유리에 비친 방에는… 플레이어가 없다.\n뒷면: 3 → 1 → 4 → 2", 4f);
            story.archiveWrong = EnsureNarration("Archive Wrong", "archive_wrong",
                "조각 순서가 틀렸다. 열린 로그가 스스로 닫혔다.", 2.5f);
            story.archiveSuccess = EnsureNarration("Archive Success", "archive_success",
                "조각 결합 완료: RECOVERED.save\n파일 크기: 0 KB / 수정한 사람: YOU", 5f);
            story.doorLocked = EnsureNarration("Door Locked", "door_locked",
                "문고리가 움직이지 않는다. 잠긴 게 아니라, 문 반대편에서 잡고 있는 것 같다.", 4f);
            story.doorLate = EnsureNarration("Door Late", "door_late",
                "문 아래 틈으로 모니터와 같은 푸른빛이 새어 나온다.", 4f);
            story.ending25 = EnsureNarration("Ending 25", "ending_25",
                "저장 중… 25%\n방의 벽과 가구가 현재 파일로 덮어쓰이는 중", 2.6f);
            story.ending74 = EnsureNarration("Ending 74", "ending_74",
                "저장 중… 74%\nOCCUPANT_00 복원.\n플레이어 프로세스를 종료합니다.", 3.4f);
            EditorUtility.SetDirty(content);
        }

        private static NarrationSO EnsureNarration(string fileName, string id, string text, float duration)
        {
            var path = $"{NarrationDirectory}/{fileName}.asset";
            var narration = AssetDatabase.LoadAssetAtPath<NarrationSO>(path);
            if (narration != null) return narration;
            narration = ScriptableObject.CreateInstance<NarrationSO>();
            narration.Configure(id, text, duration);
            AssetDatabase.CreateAsset(narration, path);
            return narration;
        }

        private sealed class HudReferences
        {
            public ControlSHudController Controller;
            public Canvas Canvas;
            public Text Objective;
            public Text Prompt;
            public Text Narration;
            public Image NarrationPanel;
            public GameObject DrawerRoot;
            public InputField DrawerInput;
            public Text DrawerHint;
            public Button DrawerSubmit;
            public Button DrawerCancel;
            public GameObject EndingRoot;
            public Text EndingTitle;
            public Text EndingBody;
            public Button EndingRestart;
            public Image GlitchOverlay;
            public RectTransform GlitchStripes;
        }
    }
}
#endif
