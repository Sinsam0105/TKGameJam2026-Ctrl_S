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
            SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
            var started = SessionState.GetFloat(StartedKey, 0f);
            if (EditorApplication.timeSinceStartup - started < 2.5d) return;

            var controller = UnityEngine.Object.FindFirstObjectByType<ControlSSceneController>();
            var player = GameObject.Find("Player");
            var desktop = GameObject.Find("Virtual Desktop Canvas");
            var hud = GameObject.Find("Room HUD Canvas");
            var sceneRoot = GameObject.Find("CONTROL S - Scene Root");
            var room = GameObject.Find("Room");
            var valid = controller != null && sceneRoot != null && room != null && player != null && desktop != null && hud != null &&
                        player.GetComponent<Rigidbody2D>() != null;
            if (!valid)
            {
                SessionState.SetBool(FailedKey, true);
                Debug.LogError("[CONTROL_S_VALIDATION] Runtime objects were not created correctly.");
            }
            else
            {
                Debug.Log("[CONTROL_S_VALIDATION] PASS — room, player, HUD and virtual desktop initialized.");
            }

            var commandLine = Environment.GetCommandLineArgs();
            if (Array.Exists(commandLine, argument => argument == "-controlSCapture") &&
                !SessionState.GetBool(CapturedKey, false))
            {
                var captureDesktop = Array.Exists(commandLine, argument => argument == "-controlSDesktop");
                if (captureDesktop) controller.OpenDesktop();
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
