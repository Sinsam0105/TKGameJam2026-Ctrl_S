using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// 정민 작업물(전자레인지·세탁기 납량 연출)은 개인 씬 Jeongmin.unity 안에만 있고
/// 프리팹이 아니라서 SampleScene 셋업이 참조할 수 없다.
/// 이 스크립트가 두 오브젝트를 프리팹으로 뽑아낸다. 원본 씬은 저장하지 않는다.
/// </summary>
public static class JeongminDirectionExtractor
{
    private const string SourceScenePath = "Assets/Scenes/Jeongmin.unity";
    private const string PrefabFolder = "Assets/ControlS/Resources/Pefabs/StageTwo";

    private static readonly (string objectName, string prefabName)[] Targets =
    {
        ("Microwave", "MicrowaveDirection"),
        ("WashingMachine", "WashingMachineDirection"),
    };

    [MenuItem("Control S/Setup/Extract Jeongmin Direction Prefabs")]
    public static void Extract()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            throw new InvalidOperationException($"프리팹 폴더가 없다: {PrefabFolder}");

        Scene scene = SceneManager.GetSceneByPath(SourceScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

        try
        {
            foreach ((string objectName, string prefabName) in Targets)
            {
                GameObject source = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Select(item => item.gameObject)
                    .FirstOrDefault(item => item.name == objectName);

                if (source == null)
                {
                    Debug.LogWarning($"[Control S] {SourceScenePath}에서 '{objectName}'을 찾지 못했다. 건너뛴다.");
                    continue;
                }

                // 원본 씬을 건드리지 않도록 복제본으로 프리팹을 만든다.
                GameObject clone = Object.Instantiate(source);
                clone.name = prefabName;
                try
                {
                    string path = $"{PrefabFolder}/{prefabName}.prefab";
                    PrefabUtility.SaveAsPrefabAsset(clone, path);
                    Debug.Log($"[Control S] 프리팹 생성: {path}");
                }
                finally
                {
                    Object.DestroyImmediate(clone);
                }
            }
        }
        finally
        {
            // 저장하지 않고 닫는다. 정민 씬은 원본 그대로 남는다.
            if (openedHere)
                EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
