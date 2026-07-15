using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 테스트용 임시 구조체.
/// 
/// 이름 변경은 취향대로 변경해도 된다.
/// 어마어마한 대사 분량을 고려하여 json과 class 사용을 추천한다.
/// 대사 데이터 파일(json)은 기획자가 작성해야 한다.
/// 
/// ==================================================================================
/// ex 1. 대사가 모두 같은 크기/색깔/속도일 때는, 하나의 SpeechBubble로 처리 가능 
/// 
/// 집에 가고 싶다 => 
/// new SpeechBubble("집에 가고 싶다", 15f, 10, Color.red, false),
///
/// ==================================================================================
/// ex 2. 대사가 일부 다른 크기/색깔/속도일 때는, 여러 개의 SpeechBubble로 분리해서 List로 묶음
///
/// 집에 /가고(글자 크기 줄어듬)/ 싶다 =>
/// new List<SpeechBubble>
/// {
///     new SpeechBubble("집에 ", 15f, 10, Color.red, false),
///     new SpeechBubble("가고", 15f, 3, Color.red, false),   // 글자 크기 줄어듬 (10 -> 3)
///     new SpeechBubble(" 싶다", 15f, 10, Color.red, false)
/// }
/// 
/// ==================================================================================
/// </summary>
public struct SpeechBubble
{
    public string Text;
    /// <summary>
    /// 높을수록 빠르게 출력한다. (1/Speed = 출력 간격)
    /// </summary>
    public float Speed;
    public int FontSize;
    public Color FontColor;

    /// <summary>
    /// 처음에만 true로 설정해도, 이후 글자는 모두 자동으로 넘어간다. 이때, 상호작용 불가 (스킵/닫기 등)
    /// </summary>
    public bool IsAuto;

    /// <summary>
    /// </summary>
    /// <param name="text"></param>
    /// <param name="speed">높을수록 빠르게 출력한다. (1/Speed = 출력 간격)</param>
    /// <param name="fontSize"></param>
    /// <param name="fontColor"></param>
    /// <param name="isAuto">하나만 true로 설정해도, 이후 글자는 모두 자동으로 넘어간다. 이때, 상호작용 불가 (스킵/닫기 등)</param>
    public SpeechBubble(string text, float speed, int fontSize, Color fontColor, bool isAuto)
    {
        Text = text;
        Speed = speed;
        FontSize = fontSize;
        FontColor = fontColor;
        IsAuto = isAuto;
    }
}

public class SpeechBubbleController : MonoBehaviour
{
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

        // test
        {
            StartCoroutine(CoShow(new List<SpeechBubble>
            {
                new SpeechBubble("집가고 싶어집가고 싶어집가고 싶어\n집가고 싶어집가고 싶어집가고 싶어", 15f, 15, Color.red, false),
                new SpeechBubble("\n집 가는 길이 너무 고되다", 15f, 8, Color.blue, false)
            }));
            //StartCoroutine(CoShow(new SpeechBubble("집가고 싶어집가고 싶어집가고 싶어\n집가고 싶어집가고 싶어집가고 싶어", 15f, 15, Color.red, false)));
        }
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
    public IEnumerator CoShow(List<SpeechBubble> textList)
    {
        if (IsTyping)   // 중복 출력 방지
        {
            Debug.Log($"이미 다른 대사가 출력 중입니다");
            yield break;
        }
        Debug.Log($"Show SpeechBubble");

        #region 초기화
        gameObject.SetActive(true);
        _textUI.text = "";
        
        IsTyping = true;
        _isAuto = textList[0].IsAuto;
        #endregion

        // 대사 출력
        // TODO: TMP로 변경 시, 변경 필요
        foreach (SpeechBubble text in textList)
        {
            // 즉시 출력
            if (_isSkip)
            {
                Skip(text);
                continue;
            }

            // 분리된 단위로 출력
            yield return StartCoroutine(CoShow(text, true));
        }

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
    public IEnumerator CoShow(SpeechBubble text, bool isContinuing = false)
    {
        #region 초기화
        if (isContinuing == false)
        {
            if (IsTyping)   // 중복 출력 방지
            {
                Debug.Log($"이미 다른 대사가 출력 중입니다");
                yield break;
            }

            Debug.Log($"Show SpeechBubble");

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

        float speed = Mathf.Max(text.Speed, 1f);
        float actualInterval = 1 / speed;   // speed가 높을수록 출력 간격이 짧아진다

        // actualInterval마다 한 글자씩 출력
        for (int idx = 0; idx < text.Text.Length; idx++)
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
            yield return new WaitForSeconds(actualInterval);
        }

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
    void Skip(SpeechBubble text, int idx = -1)  // 글자가 잘리지 않게 idx 전달한다. 그대로 출력하고 싶다면 -1
    {
        if (_isSkip == false || _isAuto)
            return;

        Debug.Log($"Skip SpeechBubble");

        // TODO: TMP로 변경 시, 제거
        {
            _textUI.fontSize = text.FontSize;
            _textUI.color = text.FontColor;
        }
        
        _textUI.text += (idx >= 0 ? text.Text[idx..] : text.Text);
    }
}