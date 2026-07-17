using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Sinsam.SingletonSystem;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

public sealed class JsonDataManager : Singleton<JsonDataManager>
{
    public Dictionary<int, SpeechBubbleData> SpeechBubbleDataDic { get; private set; } = new Dictionary<int, SpeechBubbleData>();

    public void Init()
    {
        SpeechBubbleDataDic = LoadJson<SpeechBubbleDataLoader, int, SpeechBubbleData>("SpeechBubbleData").MakeDict();
    }

    public Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Resources.Load<TextAsset>($"Data/{path}");
        //return JsonUtility.FromJson<Loader>(textAsset.text);  // JsonUtility는 답답해서 못 쓰겠습니다
        return JsonConvert.DeserializeObject<Loader>(textAsset.text);
    }
}
