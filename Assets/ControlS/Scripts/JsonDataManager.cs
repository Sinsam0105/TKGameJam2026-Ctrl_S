using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Sinsam.SingletonSystem;
using ScriptData;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

public sealed class JsonDataManager : Singleton<JsonDataManager>
{
    public Dictionary<string, ScriptData.ScriptData> ScriptData { get; private set; } = new Dictionary<string, ScriptData.ScriptData>();

    public void Init()
    {
        Debug.Log("JsonDataManager Init");
        ScriptData = LoadAllScripts();
    }

    public Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Resources.Load<TextAsset>($"Data/{path}");
        //return JsonUtility.FromJson<Loader>(textAsset.text);  // JsonUtility는 답답해서 못 쓰겠습니다
        return JsonConvert.DeserializeObject<Loader>(textAsset.text);
    }

    public Dictionary<string, ScriptData.ScriptData> LoadAllScripts()
    {
        Dictionary<string, ScriptData.ScriptData> dict = new Dictionary<string, ScriptData.ScriptData>();
        TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>($"Data/Scripts/");

        foreach (TextAsset f in jsonFiles)
        {
            ScriptData.ScriptData data = JsonConvert.DeserializeObject<ScriptData.ScriptData>(f.text);
            if (data == null)
            {
                Debug.LogError($"대본 JSON 변환 실패: {f.name}");
                continue;
            }

            if (dict.ContainsKey(f.name))
            {
                Debug.LogError($"중복된 대본 ID입니다: {f.name}");
                continue;
            }

            dict.Add(f.name, data);
        }

        return dict;
    }
}
