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
    private const string StageTwoFolder = "Assets/ControlS/Resources/Pefabs/StageTwo";
    private const string StageThreeFolder = "Assets/ControlS/Resources/Pefabs/StageThree";

    // 씬 오브젝트 이름은 선배가 "_test" 접미사(일부는 공백 포함)로 두었다.
    private static readonly (string objectName, string prefabName, string folder)[] Targets =
    {
        ("Microwave _test", "MicrowaveDirection", StageTwoFolder),
        ("WashingMachine _test", "WashingMachineDirection", StageTwoFolder),
        ("Computer _test", "Version07Direction", StageThreeFolder),
        ("FrontDoor_test", "KnockDirection", StageThreeFolder),
    };

    [MenuItem("Control S/Setup/Extract Jeongmin Direction Prefabs")]
    public static void Extract()
    {
        EnsureFolder(StageTwoFolder);
        EnsureFolder(StageThreeFolder);

        Scene scene = SceneManager.GetSceneByPath(SourceScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

        try
        {
            foreach ((string objectName, string prefabName, string folder) in Targets)
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
                    string path = $"{folder}/{prefabName}.prefab";
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
}
