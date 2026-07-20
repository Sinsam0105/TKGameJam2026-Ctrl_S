using ScriptData;
using Sinsam.SingletonSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 파일 이름(대본 이름)을 통해 컷씬/액션/상황에 맞는 ScriptData를 불러와, 화자(EObject)별 말풍선을 순서대로 띄운다.
/// </summary>
public class ScriptManager : MonoSingleton<ScriptManager>
{
    static bool s_init = false;

    ScriptData.ScriptData _current;
    int _index;
    readonly Queue<string> _pending = new();

    public bool IsPlaying => _current != null;

    /// <summary>
    /// 대본 하나(말풍선 전부)가 끝까지 재생되면 발생한다. 인자는 방금 끝난 대본 이름.
    /// TODO: 말풍선 외의 "이벤트"(연출/이펙트 등)는 아직 ScriptData에 정의되어 있지 않다. 필요해지면 이 지점에서 확장한다.
    /// </summary>
    public event Action<string> ScriptFinished;

    /// <summary>
    /// 말풍선이 하나 닫힐 때마다 발생한다. (대본 이름, 방금 끝난 말풍선의 인덱스)
    /// </summary>
    public event Action<string, int> BubbleFinished;

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        // 중복 초기화 방지
        if (s_init)
            return;

        s_init = true;
        JsonDataManager.Instance.Init();
    }

    /// <summary>
    /// 대본을 재생한다.
    /// </summary>
    /// <param name="name">대본 이름 (Resources/Data/Scripts 안의 json 파일명)</param>
    public void Play(string name)
    {
        if (JsonDataManager.Instance.ScriptData.ContainsKey(name) == false)
        {
            Debug.LogError($"{name}라는 대본은 존재하지 않는다.");
            return;
        }

        if (IsPlaying)
        {
            _pending.Enqueue(name);
            return;
        }

        StartScript(name);
    }

    void StartScript(string name)
    {
        _current = JsonDataManager.Instance.ScriptData[name];
        _index = 0;
        PlayNextBubble(name);
    }

    void PlayNextBubble(string name)
    {
        // 대본의 말풍선을 모두 재생했다
        if (_index >= _current.Bubbles.Count)
        {
            _current = null;
            OnScriptFinished(name);

            if (_pending.Count > 0)
                StartScript(_pending.Dequeue());
            return;
        }

        SpeechBubbleData data = _current.Bubbles[_index];
        SpeechBubbleController bubble = SpeechBubbleController.Get(data.ObjectType);    // 오브젝트 타입을 통해 화자의 말풍선을 찾는다
        if (bubble == null)
        {
            Debug.LogError($"{data.ObjectType}의 말풍선을 찾을 수 없다. 대사를 건너뛴다: {name}[{_index}]");
            _index++;
            PlayNextBubble(name);
            return;
        }

        bubble.Closed += OnBubbleClosed;
        bubble.Show(data.Bubble, _current.IsAuto);  // CoShow 코루틴 실행 중

        // 이 말풍선이 닫힐 때마다 다음 말풍선으로 넘어간다
        void OnBubbleClosed()
        {
            bubble.Closed -= OnBubbleClosed;
            int finishedIndex = _index;
            _index++;

            OnBubbleFinished(name, finishedIndex);
            PlayNextBubble(name);
        }
    }

    // ScriptFinished 이벤트를 발생시킨다
    void OnScriptFinished(string name)
    {
        ScriptFinished?.Invoke(name);
    }

    // BubbleFinished 이벤트를 발생시킨다
    void OnBubbleFinished(string name, int index)
    {
        BubbleFinished?.Invoke(name, index);
    }
}
