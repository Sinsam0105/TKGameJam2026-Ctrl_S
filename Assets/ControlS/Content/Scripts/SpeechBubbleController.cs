using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum ESpeechBubbleSpeed
{
    None = 0,       // 그냥 한 번에 출력
    VerySlow = 2,   // 거의 뭐 한 글자씩 출력되는 수준
    Slow = 10,
    Normal = 20,    // 기본 속도
    Fast = 40,
    VeryFast = 100, // 너무 빨라서 눈으로 따라가기 힘들다
}

/// <summary>
/// 모든 말풍선
/// 어마어마한 대사 분량을 고려하여 json 사용을 추천한다.
/// 대사 데이터 파일(json)은 기획자가 작성해야 한다.
/// Assets/ControlS/Resources/Data 안에 넣으면 된다.
/// </summary>
[Serializable]
public class SpeechBubbleData   // TODO: 구조 재설계
{
    public int DataId;
    public List<SpeechBubbleInfo> Bubble;
}

[Serializable]
public class SpeechBubbleInfo
{
    public string Text;
    /// <summary>
    /// 높을수록 빠르게 출력한다. (1/Speed = 출력 간격)
    /// </summary>
    public ESpeechBubbleSpeed Speed;
    public int FontSize;
    
    [JsonIgnore] public Color FontColor => new Color(R, G, B, A);
    public float R, G, B, A;

    /// <summary>
    /// 처음에만 true로 설정해도, 이후 글자는 모두 자동으로 넘어간다. 이때, 상호작용 불가 (스킵/닫기 등)
    /// </summary>
    public bool IsAuto;
    
    public SpeechBubbleInfo()
    {
    }

    #region 테스트용
    public SpeechBubbleInfo(string text, ESpeechBubbleSpeed speed, int fontSize, float r, float g, float b, float a, bool isAuto)
    {
        Text = text;
        Speed = speed;
        FontSize = fontSize;
        R = r;
        G = g;
        B = b;
        A = a;
        IsAuto = isAuto;
    }

    public SpeechBubbleInfo(string text, ESpeechBubbleSpeed speed, int fontSize, Color color, bool isAuto)
    {
        Text = text;
        Speed = speed;
        FontSize = fontSize;
        R = color.r;
        G = color.g;
        B = color.b;
        A = color.a;
        IsAuto = isAuto;
    }
    #endregion
}

[Serializable]
public class SpeechBubbleDataLoader : ILoader<int, SpeechBubbleData>
{
    public List<SpeechBubbleData> SpeechBubble = new List<SpeechBubbleData>();
    public Dictionary<int, SpeechBubbleData> MakeDict()
    {
        Dictionary<int, SpeechBubbleData> dict = new Dictionary<int, SpeechBubbleData>();
        foreach (SpeechBubbleData data in SpeechBubble)
            dict.Add(data.DataId, data);
        return dict;
    }
}

public class SpeechBubbleController : MonoBehaviour
{
    SpeechBubbleData Data;
    Text _textUI;   // TODO: 추후 TMP로 변경 필요

    /// <summary>
    /// 대사 출력 중인가
    /// </summary>
    public bool IsTyping { get; private set; } = false;
    
    bool _isSkip = false;   // 대사 출력 중에 스킵키를 눌렀는가
    bool _isAuto = false;   // 자동으로 다음 대사로 넘어가는가 (false: 수동), 상호작용 불가 (스킵/닫기 등)

    void Awake()
    {
        Init();
    }

    public void Init()
    {
        Debug.Log("SpeechBubbleController Init");
        _textUI = GetComponentInChildren<Text>();
        transform.localPosition = new Vector3(0, 1.5f, transform.localPosition.z);  // 머리 위 배치

        #region TODO: 테스트 용도이다. 제거 필요
        {
            JsonDataManager jsonData = new JsonDataManager();
            jsonData.Init();
            Data = jsonData.SpeechBubbleDataDic[1001];
            StartCoroutine(CoShow(Data.Bubble));

            //StartCoroutine(CoShow(new List<SpeechBubbleInfo>
            //{
            //    new SpeechBubbleInfo("그래. 귀관에게 ", ESpeechBubbleSpeed.Slow, 25, Color.blue, false),
            //    new SpeechBubbleInfo("주어진 ", ESpeechBubbleSpeed.Slow, 25, Color.blue, false),
            //    new SpeechBubbleInfo("아주아주 막중한 임무", ESpeechBubbleSpeed.Fast, 25, Color.blue, false),
            //    new SpeechBubbleInfo("가 있기 때문이지.", ESpeechBubbleSpeed.Slow, 25, Color.blue, false),
            //    //new SpeechBubbleInfo("\n알겠는가?알겠는가?알겠는가?알겠는가?알겠는가?알겠는가?", ESpeechBubbleSpeed.Fast, 25, Color.darkRed, false),
            //    //new SpeechBubbleInfo("\n알겠는가?알겠는가?알겠는가?알겠는가?알겠는가?알겠는가?알겠는가?알겠는가?알겠는가?", ESpeechBubbleSpeed.VeryFast, 25, Color.red, false),
            //}));
            //StartCoroutine(CoShow(new SpeechBubbleInfo("집가고 싶어집가고 싶어집가고 싶어\n집가고 싶어집가고 싶어집가고 싶어", ESpeechBubbleSpeed.Normal, 15, Color.red, false)));
        }
        #endregion
    }

    void Update()
    {
        // 자동 출력 중일 때는 상호작용 불가
        if (_isAuto)
            return;

        // TODO: 추후 키 변경 예정 (임시로, 왼쪽 마우스)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsTyping)
                _isSkip = true;
            else
                gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 효과 단위로 분리된 글자를 모두 합쳐, 한 글자씩 말풍선을 띄운다. (크기/색깔/속도)
    /// </summary>
    /// <param name="text">한 개의 말풍선에 들어갈 대사 내용</param>
    public IEnumerator CoShow(List<SpeechBubbleInfo> textList)
    {
        if (IsTyping)   // 중복 출력 방지
        {
            Debug.Log($"이미 다른 대사가 출력 중입니다");
            yield break;
        }
        Debug.Log($"Show SpeechBubbleInfo");

        #region 초기화
        gameObject.SetActive(true);
        _textUI.text = "";
        
        IsTyping = true;
        _isAuto = textList[0].IsAuto;
        #endregion

        // 대사 출력
        // TODO: TMP로 변경 시, 변경 필요
        int range = textList.Count - 1;
        for (int idx = 0; idx < range; idx++)
        {
            // 즉시 출력
            if (_isSkip)
            {
                Skip(textList[idx]);
                continue;
            }

            // 분리된 단위로 출력
            yield return StartCoroutine(CoShow(textList[idx], true));
            
            // 현재 단위와 다음 단위 사이의 간격
            float speed = ((float)textList[idx].Speed + (float)textList[idx + 1].Speed) / 2f;
            yield return new WaitForSeconds(GetInterval(speed));  // 단위별로 출력 간격
        }
        yield return StartCoroutine(CoShow(textList[range], true)); // 마지막 단위 출력

        // 대사 모두 출력 후, 잠시 대기한 뒤에 닫는다
        if (_isAuto)
        {
            yield return new WaitForSeconds(0.5f);
            _isAuto = false;
            gameObject.SetActive(false);
        }

        // 대사 모두 출력한 후, 초기화
        _isSkip = false;
        IsTyping = false;

        // TODO: _isAuto가 false면, 대사 출력이 완료될 때 아이콘 추가 (ex. ▼)
    }

    /// <summary>
    /// 한 글자씩 말풍선을 띄운다.
    /// </summary>
    /// <param name="text">한 개의 말풍선에 들어갈 대사 내용</param>
    /// <param name="isContinuing">이전 텍스트에 이어서 출력하는지 여부. 효과 단위로 글자를 분리했을 때 사용한다.</param>
    public IEnumerator CoShow(SpeechBubbleInfo text, bool isContinuing = false)
    {
        #region 초기화
        if (isContinuing == false)
        {
            if (IsTyping)   // 중복 출력 방지
            {
                Debug.Log($"이미 다른 대사가 출력 중입니다");
                yield break;
            }

            Debug.Log($"Show SpeechBubbleInfo");

            gameObject.SetActive(true);
            _textUI.text = "";

            IsTyping = true;
            _isAuto = text.IsAuto;
        }
        #endregion

        // TODO: TMP로 변경 시, 제거
        {
            _textUI.fontSize = text.FontSize;
            _textUI.color = text.FontColor;
        }

        if (text.Speed == ESpeechBubbleSpeed.None)
        {
            _textUI.text += text.Text;
            yield break;
        }

        float interval = GetInterval(text.Speed);

        // interval마다 한 글자씩 출력
        int range = text.Text.Length - 1;
        for (int idx = 0; idx < range; idx++)
        {
            // 즉시 출력
            if (_isSkip)
            {
                Skip(text, idx);
                if (isContinuing)
                    yield break;
                break;
            }

            _textUI.text += text.Text[idx];
            yield return new WaitForSeconds(interval);
        }
        _textUI.text += text.Text[range];   // 마지막 글자

        if (isContinuing)
            yield break;

        if (_isAuto)
        {
            yield return new WaitForSeconds(0.5f);
            _isAuto = false;
            gameObject.SetActive(false);
        }

        // 대사 모두 출력한 후, 초기화
        _isSkip = false;
        IsTyping = false;

        // TODO: _isAuto가 false면, 대사 출력이 완료될 때 아이콘 추가 (ex. ▼)
    }

    // TODO: TMP로 변경 시, 제거
    void Skip(SpeechBubbleInfo text, int idx = -1)  // 글자가 잘리지 않게 idx 전달한다. 그대로 출력하고 싶다면 -1
    {
        if (_isSkip == false || _isAuto)
            return;

        Debug.Log($"Skip SpeechBubbleInfo {text.Text}");

        // TODO: TMP로 변경 시, 제거
        {
            _textUI.fontSize = text.FontSize;
            _textUI.color = text.FontColor;
        }
        
        _textUI.text += (idx >= 0 ? text.Text[idx..] : text.Text);
    }

    float GetInterval(float speed)
    {
        float actualSpeed = Mathf.Max(speed, 1f);
        return 1 / actualSpeed; // speed가 높을수록 출력 간격이 짧아진다
    }

    float GetInterval(ESpeechBubbleSpeed speed)
    {
        float actualSpeed = Mathf.Max((float)speed, 1f);
        return 1 / actualSpeed; // speed가 높을수록 출력 간격이 짧아진다
    }
}