#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ControlS.Editor
{
    public static class ControlSSceneValidation
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/CONTROL S/Validate SampleScene %#v")]
        public static void ValidateMenu()
        {
            var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Additive);
            var valid = Validate(scene, out var report);
            EditorSceneManager.CloseScene(scene, true);
            if (valid) Debug.Log("[CONTROL S VALIDATION] PASS\n" + report);
            else Debug.LogError("[CONTROL S VALIDATION] FAIL\n" + report);
        }

        public static bool Validate(Scene scene, out string report)
        {
            var errors = new List<string>();
            var runners = FindInScene<PuzzleRunner>(scene);
            var inputs = FindInScene<BaseEnter>(scene);
            var contents = FindInScene<ContentManager>(scene);
            var ui = FindInScene<UIManager>(scene);
            var desktop = FindInScene<VirtualDesktop>(scene);
            var atmosphere = FindInScene<AtmosphereManager>(scene);
            var sound = FindInScene<SoundManager>(scene);

            if (contents.Count != 1 || contents[0].Current == null || contents[0].Current.progress == null)
                errors.Add("ContentManager, GameContentSetSO, or ProgressSetSO is missing.");
            if (ui.Count != 1 || !ui[0].ValidateReferences()) errors.Add("UIManager references are incomplete.");
            if (desktop.Count != 1 || !desktop[0].ValidateReferences()) errors.Add("VirtualDesktop references are incomplete.");
            if (atmosphere.Count != 1 || !atmosphere[0].ValidateReferences()) errors.Add("AtmosphereManager references are incomplete.");
            if (sound.Count != 1 || !sound[0].ValidateReferences()) errors.Add("SoundManager references are incomplete.");
            if (runners.Count == 0) errors.Add("No PuzzleRunner exists.");
            foreach (var runner in runners)
                if (runner.Definition == null || runner.Definition.Matcher == null)
                    errors.Add($"PuzzleRunner '{runner.name}' has no valid definition.");
            foreach (var input in inputs)
                if (input.TargetPuzzle == null) errors.Add($"BaseEnter '{input.name}' has no target puzzle.");
            foreach (var room in FindInScene<RoomInteractable>(scene))
                if (room.Rules == null || room.Rules.Count == 0) errors.Add($"RoomInteractable '{room.name}' has no rule.");
            foreach (var root in scene.GetRootGameObjects())
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root) > 0)
                    errors.Add($"'{root.name}' contains a missing script.");

            report = errors.Count == 0 ? $"Validated {runners.Count} puzzles and {inputs.Count} inputs." :
                string.Join("\n", errors);
            return errors.Count == 0;
        }

        private static List<T> FindInScene<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (var root in scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<T>(true));
            return result;
        }
    }
}
#endif
