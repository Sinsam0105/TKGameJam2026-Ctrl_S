#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;

namespace ControlS.Editor
{
    /// <summary>CLI smoke test used by the project README and automated verification.</summary>
    [InitializeOnLoad]
    public static class ControlSPlayModeValidation
    {
        private const string ActiveKey = "ControlS.Validation.Active";
        private const string FailedKey = "ControlS.Validation.Failed";
        private const string StartedKey = "ControlS.Validation.Started";
        private const string CapturedKey = "ControlS.Validation.Captured";
        private const string PhaseKey = "ControlS.Validation.Phase";

        static ControlSPlayModeValidation()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= CaptureLog;
            Application.logMessageReceived += CaptureLog;
        }

        public static void Run()
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetBool(CapturedKey, false);
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
            var started = SessionState.GetFloat(StartedKey, 0f);
            var phase = SessionState.GetInt(PhaseKey, 0);
            if (EditorApplication.timeSinceStartup - started < (phase == 0 ? 2.5d : .2d)) return;

            var controller = UnityEngine.Object.FindFirstObjectByType<ControlSSceneController>();
            var hudController = UnityEngine.Object.FindFirstObjectByType<ControlSHudController>();
            var atmosphere = UnityEngine.Object.FindFirstObjectByType<ControlSAtmosphereController>();
            var drawer = UnityEngine.Object.FindFirstObjectByType<DrawerKeypadContent>();
            var virtualDesktop = UnityEngine.Object.FindFirstObjectByType<VirtualDesktop>();
            var sequences = UnityEngine.Object.FindObjectsByType<InteractionSequence>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var endingRoot = FindObjectIncludingInactive("Ending UI");
            var player = GameObject.Find("Player");
            var desktop = GameObject.Find("Virtual Desktop Canvas");
            var hud = GameObject.Find("Room HUD Canvas");
            var sceneRoot = GameObject.Find("CONTROL S - Scene Root");
            var room = GameObject.Find("Room");
            var interactionBindingsValid = ValidateInteractionBindings();
            var valid = controller != null && controller.enabled && controller.Content != null &&
                        hudController != null && hudController.ValidateReferences() &&
                        atmosphere != null && atmosphere.ValidateReferences() &&
                        virtualDesktop != null && ValidateDesktop(virtualDesktop) &&
                        drawer != null && drawer.ValidateReferences() && drawer.Root != null && !drawer.Root.activeSelf &&
                        sequences.Length == 2 && Array.TrueForAll(sequences, sequence => sequence.Actions.Count > 0) &&
                        endingRoot != null && !endingRoot.activeSelf && ValidateContentButtons() &&
                        sceneRoot != null && room != null && player != null && desktop != null && hud != null &&
                        player.GetComponent<Rigidbody2D>() != null && interactionBindingsValid;
            if (!valid)
            {
                SessionState.SetBool(FailedKey, true);
                Debug.LogError("[CONTROL_S_VALIDATION] Runtime objects were not created correctly.");
            }
            else if (phase == 0)
            {
                RoomInteractable clock = null;
                var interactions = UnityEngine.Object.FindObjectsByType<RoomInteractable>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                foreach (var interaction in interactions)
                    if (interaction.Kind == RoomInteractionKind.Clock) clock = interaction;
                clock?.Interact(player);
                SessionState.SetInt(PhaseKey, 1);
                SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup);
                return;
            }
            else if (phase == 1 && !controller.State.ClockInspected)
            {
                SessionState.SetBool(FailedKey, true);
                Debug.LogError("[CONTROL_S_VALIDATION] Serialized clock rule did not execute.");
            }
            else if (phase == 1)
            {
                virtualDesktop.Open();
                virtualDesktop.Shortcuts[0].Execute();
                SessionState.SetInt(PhaseKey, 2);
                SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup);
                return;
            }
            else if (!virtualDesktop.Windows[0].IsOpen)
            {
                SessionState.SetBool(FailedKey, true);
                Debug.LogError("[CONTROL_S_VALIDATION] Serialized desktop shortcut rule did not open its window.");
            }
            else
            {
                virtualDesktop.Windows[0].Close();
                virtualDesktop.Close();
                Debug.Log("[CONTROL_S_VALIDATION] PASS — room and desktop serialized rules initialized and executed.");
            }

            var commandLine = Environment.GetCommandLineArgs();
            if (Array.Exists(commandLine, argument => argument == "-controlSCapture") &&
                !SessionState.GetBool(CapturedKey, false))
            {
                var captureDesktop = Array.Exists(commandLine, argument => argument == "-controlSDesktop");
                if (captureDesktop) UnityEngine.Object.FindFirstObjectByType<VirtualDesktop>()?.Open();
                var path = Path.GetFullPath(captureDesktop
                    ? "Logs/control-s-desktop.png"
                    : "Logs/control-s-room.png");
                CaptureFrame(path);
                SessionState.SetBool(CapturedKey, true);
                SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup);
                Debug.Log("[CONTROL_S_VALIDATION] Capturing " + path);
                return;
            }

            SessionState.SetBool(ActiveKey, false);
            var failed = SessionState.GetBool(FailedKey, false);
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(failed ? 1 : 0);
        }

        private static bool ValidateInteractionBindings()
        {
            var interactions = UnityEngine.Object.FindObjectsByType<RoomInteractable>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (interactions.Length != 5) return false;
            foreach (var interaction in interactions)
            {
                if (interaction.Rules == null || interaction.Rules.Count == 0) return false;
                var expectedRuleCount = interaction.Kind switch
                {
                    RoomInteractionKind.Computer => 1,
                    RoomInteractionKind.Clock => 4,
                    RoomInteractionKind.Drawer => 2,
                    RoomInteractionKind.Photo => 2,
                    RoomInteractionKind.Door => 2,
                    _ => 0
                };
                if (interaction.Rules.Count != expectedRuleCount) return false;
                foreach (var rule in interaction.Rules)
                    if (rule == null || rule.Actions.Count == 0) return false;
                if (interaction.OnInteract.GetPersistentEventCount() != 0 ||
                    interaction.OnInteractNarration.GetPersistentEventCount() != 0 ||
                    interaction.OnInteractGameObject.GetPersistentEventCount() != 0) return false;
            }
            return true;
        }

        private static GameObject FindObjectIncludingInactive(string objectName)
        {
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var item in transforms)
                if (item.name == objectName) return item.gameObject;
            return null;
        }

        private static bool ValidateContentButtons()
        {
            UnityEngine.UI.Button submit = null;
            UnityEngine.UI.Button cancel = null;
            UnityEngine.UI.Button restart = null;
            var buttons = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (button.name == "Submit") submit = button;
                else if (button.name == "Cancel") cancel = button;
                else if (button.name == "Restart") restart = button;
            }
            return submit != null && submit.onClick.GetPersistentEventCount() > 0 &&
                   cancel != null && cancel.onClick.GetPersistentEventCount() > 0 &&
                   restart != null && restart.onClick.GetPersistentEventCount() > 0;
        }

        private static bool ValidateDesktop(VirtualDesktop desktop)
        {
            if (!desktop.ValidateReferences() || desktop.Shortcuts.Length != 6 || desktop.Windows.Length != 6)
                return false;
            foreach (var shortcut in desktop.Shortcuts)
                if (shortcut == null || shortcut.Rules.Count == 0) return false;
            foreach (var window in desktop.Windows)
                if (window == null) return false;
            return desktop.Shortcuts[5].VisibilityConditions.Count > 0;
        }

        private static void CaptureFrame(string path)
        {
            var camera = Camera.main;
            if (camera == null) return;

            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var previousModes = new RenderMode[canvases.Length];
            var previousCameras = new Camera[canvases.Length];
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            for (var i = 0; i < canvases.Length; i++)
            {
                previousModes[i] = canvases[i].renderMode;
                previousCameras[i] = canvases[i].worldCamera;
                if (canvases[i].renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = 1f;
            }
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());

            camera.targetTexture = previousTarget;
            for (var i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = previousModes[i];
                canvases[i].worldCamera = previousCameras[i];
            }
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }

        private static void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (type == LogType.Exception || type == LogType.Assert || type == LogType.Error)
            {
                if (!condition.Contains("CONTROL_S_VALIDATION"))
                    SessionState.SetBool(FailedKey, true);
            }
        }
    }
}
#endif
