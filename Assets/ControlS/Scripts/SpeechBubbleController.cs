using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ScriptData;

public class SpeechBubbleController : MonoBehaviour
{
    protected static readonly Dictionary<EObject, SpeechBubbleController> s_registry = new();
    /// <summary>
    /// 화자(EObject)당 말풍선이 하나씩만 있어야 한다. 이 레지스트리로 화자에 맞는 말풍선을 찾는다.
    /// </summary>
    /// <param name="speaker"></param>
    /// <returns></returns>
    public static SpeechBubbleController Get(EObject speaker) => s_registry.GetValueOrDefault(speaker);

    [SerializeField] protected EObject _speaker;    // 이 말풍선의 주인 (ex. Player/Monster/NPC)

    public Transform Owner { get; private set; }
    protected TMP_Text _textUI;

    /// <summary>
    /// 대사 출력 중인가
    /// </summary>
    public bool IsTyping { get; private set; } = false;

    /// <summary>
    /// 말풍선이 닫힐 때(자동/수동 모두) 발생한다. ScriptManager가 이걸로 다음 대사 진행 시점을 판단한다.
    /// </summary>
    public event Action Closed;

    protected bool _isSkip = false;   // 대사 출력 중에 스킵키를 눌렀는가
    protected bool _isAuto = false;   // 자동으로 다음 대사로 넘어가는가 (false: 수동), 상호작용 불가 (스킵/닫기 등)

    protected Action<EExpression> _changeExpression;

    void Awake()
    {
        Init();
    }

    void OnDestroy()
    {
        if (s_registry.TryGetValue(_speaker, out var self) && self == this)
            s_registry.Remove(_speaker);
    }

    public virtual void Init()
    {
        //Debug.Log("SpeechBubbleController Init");
        Owner = transform.parent.transform;
        {
            if (Owner.GetComponent<PlayerMove>() != null)
                _speaker = EObject.Player;
        }

        transform.localPosition = new Vector3(0, 1.5f, transform.localPosition.z);  // 머리 위 배치
        _textUI = GetComponentInChildren<TMP_Text>();

        s_registry[_speaker] = this;
        gameObject.SetActive(false);
    }

    void Update()
    {
        // 자동 출력 중일 때는 상호작용 불가
        if (_isAuto)
            return;

        // 기존 클릭 입력에 프롤로그 진행용 Enter만 추가한다.
        bool advance = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        advance |= Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
        if (advance)
        {
            if (IsTyping)
            {
                _isSkip = true;
                return;
            }

            OnClosed();
        }
    }

    /// <summary>
    /// 말풍선을 띄운다.
    /// </summary>
    /// <param name="speechBubble">출력할 대사 (효과 단위로 분리된 조각들)</param>
    /// <param name="isAuto">true면 출력 완료 후 자동으로 닫힌다 (상호작용 불가). false면 클릭으로 스킵/닫기.</param>
    public void Show(SpeechBubbleData data, bool isAuto = false)
    {
        if (IsTyping)   // 중복 출력 방지
        {
            Debug.Log($"이미 다른 대사가 출력 중입니다");
            return;
        }

        //Debug.Log($"Show SpeechBubbleInfo");


        _isAuto = isAuto;
        gameObject.SetActive(true);
        _textUI.text = "";
        IsTyping = true;

        StartCoroutine(CoShow(data.Bubble, data.Expression));
    }

    protected void OnClosed()
    {
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    protected virtual IEnumerator CoShow(List<SpeechBubbleInfo> speechBubble, EExpression expression)
    {
        // 대사 출력
        int range = speechBubble.Count - 1;
        for (int idx = 0; idx < range; idx++)
        {
            // 시스템 - 플레이어 표정 바뀜
            _changeExpression?.Invoke(expression);

            // 즉시 출력
            if (_isSkip)
            {
                Skip(speechBubble[idx]);
                continue;
            }

            // 분리된 단위로 출력
            yield return StartCoroutine(CoShow(speechBubble[idx], true));

            // 현재 단위와 다음 단위 사이의 간격
            float speed = ((float)speechBubble[idx].Speed + (float)speechBubble[idx + 1].Speed) / 2f;
            yield return new WaitForSeconds(GetInterval(speed));  // 단위별로 출력 간격
        }
        yield return StartCoroutine(CoShow(speechBubble[range], true)); // 마지막 단위 출력

        // 대사 모두 출력 후, 잠시 대기한 뒤에 닫는다
        if (_isAuto)
        {
            yield return new WaitForSeconds(0.8f);
            _isAuto = false;
            OnClosed();
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
    protected IEnumerator CoShow(SpeechBubbleInfo text, bool isContinuing = false)
    {
        // fontSize/color를 TMP_Text에 직접 대입하면 이미 출력된 글자들까지 전부 바뀌므로,
        // 이 단위(segment)만 감싸는 리치 텍스트 태그로 스타일을 적용한다.
        if (text.Speed == ESpeechBubbleSpeed.None)
        {
            _textUI.text += GetStyleTag(text) + text.Text + CloseStyleTag;
            yield break;
        }

        float interval = GetInterval(text.Speed);
        int range = text.Text.Length - 1;

        _textUI.text += GetStyleTag(text);

        // interval마다 한 글자씩 출력
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
        _textUI.text += text.Text[range] + CloseStyleTag;   // 마지막 글자

        if (isContinuing)
            yield break;

        if (_isAuto)
        {
            yield return new WaitForSeconds(0.5f);
            _isAuto = false;
            OnClosed();
        }

        // 대사 모두 출력한 후, 초기화
        _isSkip = false;
        IsTyping = false;

        // TODO: _isAuto가 false면, 대사 출력이 완료될 때 아이콘 추가 (ex. ▼)
    }

    protected void Skip(SpeechBubbleInfo text, int idx = -1)  // 글자가 잘리지 않게 idx 전달한다. 그대로 출력하고 싶다면 -1
    {
        if (_isSkip == false || _isAuto)
            return;

        // idx >= 0: CoShow 도중 스킵된 경우, 스타일 태그는 이미 열려 있으므로 남은 글자만 이어붙인다.
        // idx == -1: CoShow를 거치지 않고 통째로 스킵된 경우, 스타일 태그를 직접 열어줘야 한다.
        string content = idx >= 0 ? text.Text[idx..] : GetStyleTag(text) + text.Text;
        _textUI.text += content + CloseStyleTag;
    }

    /// <summary>
    /// 이 단위(segment)에만 적용되는 폰트 크기/색을 여는 리치 텍스트 태그.
    /// CloseStyleTag와 항상 짝을 맞춰서 닫아야 이후 단위에 스타일이 새지 않는다.
    /// </summary>
    protected static string GetStyleTag(SpeechBubbleInfo text) =>
        $"<size={text.FontSize}><color=#{ColorUtility.ToHtmlStringRGBA(text.FontColor)}>";

    protected const string CloseStyleTag = "</color></size>";

    protected float GetInterval(float speed)
    {
        float actualSpeed = Mathf.Max(speed, 1f);
        return 1 / actualSpeed; // speed가 높을수록 출력 간격이 짧아진다
    }

    protected float GetInterval(ESpeechBubbleSpeed speed)
    {
        float actualSpeed = Mathf.Max((float)speed, 1f);
        return 1 / actualSpeed; // speed가 높을수록 출력 간격이 짧아진다
    }
}
