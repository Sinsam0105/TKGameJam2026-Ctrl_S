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
    static readonly Dictionary<EObject, SpeechBubbleController> s_registry = new();
    /// <summary>
    /// 화자(EObject)당 말풍선이 하나씩만 있어야 한다. 이 레지스트리로 화자에 맞는 말풍선을 찾는다.
    /// </summary>
    /// <param name="speaker"></param>
    /// <returns></returns>
    public static SpeechBubbleController Get(EObject speaker) => s_registry.GetValueOrDefault(speaker);

    /// <summary>
    /// 시스템 안내 문구를 System 말풍선으로 띄운다. (프롬프트/목표 텍스트 대체용)
    /// System 말풍선이 씬에 없으면 조용히 넘어간다.
    /// </summary>
    public static void ShowSystem(string text, bool isAuto = false, int fontSize = 28)
    {
        if (string.IsNullOrEmpty(text))
            return;

        SpeechBubbleController bubble = Get(EObject.System);
        if (bubble == null)
        {
            Debug.LogWarning("[SpeechBubbleController] System 말풍선이 등록되어 있지 않습니다.");
            return;
        }

        SpeechBubbleInfo info = new SpeechBubbleInfo
        {
            Text = text,
            Speed = ESpeechBubbleSpeed.VeryFast,
            FontSize = fontSize,
            R = 1f, G = 1f, B = 1f, A = 1f,
        };
        bubble.Show(new List<SpeechBubbleInfo> { info }, isAuto);
    }

    [SerializeField] EObject _speaker;    // 이 말풍선의 주인 (ex. Player/Monster/NPC)

    [Header("배치")]
    [Tooltip("실제로 움직이는 말풍선 본체. 루트는 화면 전체를 덮는 Overlay 캔버스라 건드리지 않는다.")]
    [SerializeField] RectTransform _bubbleRoot;
    [Tooltip("화자 기준 머리 위 오프셋 (월드 단위)")]
    [SerializeField] Vector2 _worldHeadOffset = new Vector2(0f, 1.5f);
    [Tooltip("데스크톱이 열렸을 때 붙을 화면 위치 (0~1 뷰포트). 우하단 일러 바로 위.")]
    [SerializeField] Vector2 _desktopViewportAnchor = new Vector2(0.97f, 0.30f);

    [Tooltip("System 말풍선의 고정 화면 위치 (0~1 뷰포트). 화면 상단 중앙.")]
    [SerializeField] Vector2 _systemViewportAnchor = new Vector2(0.5f, 0.92f);

    [Tooltip("켜면 대사 색을 JSON 값과 무관하게 흰색으로 고정한다. (검은 말풍선 위 하얀 글씨)")]
    [SerializeField] bool _overrideTextColorWhite = true;

    Camera _camera;
    ComputerWindowedUI _desktop;
    bool _desktopSearched;

    public Transform Owner { get; private set; }
    Text _textUI;
    TMP_Text _tmpTextUI;

    /// <summary>
    /// 대사 출력 중인가
    /// </summary>
    public bool IsTyping { get; private set; } = false;

    /// <summary>
    /// 말풍선이 닫힐 때(자동/수동 모두) 발생한다. ScriptManager가 이걸로 다음 대사 진행 시점을 판단한다.
    /// </summary>
    public event Action Closed;

    bool _isSkip = false;   // 대사 출력 중에 스킵키를 눌렀는가
    bool _isAuto = false;   // 자동으로 다음 대사로 넘어가는가 (false: 수동), 상호작용 불가 (스킵/닫기 등)

    void Awake()
    {
        Init();
    }

    void OnDestroy()
    {
        if (s_registry.TryGetValue(_speaker, out var self) && self == this)
            s_registry.Remove(_speaker);
    }

    public void Init()
    {
        //Debug.Log("SpeechBubbleController Init");
        Owner = transform.parent;
        // 임시. TODO: 플레이어/몬스터/NPC 크리처들이 Base 컴포넌트를 상속하면 좋겠다
        // 또는, 태그를 활용한다던가..?
        {
            if (Owner != null && Owner.GetComponent<PlayerMove>() != null)
                _speaker = EObject.Player;
        }

        ResolveTextUI();
        if (_bubbleRoot == null)
        {
            RectTransform textRect = GetTextRectTransform();
            _bubbleRoot = textRect != null ? textRect.parent as RectTransform : null;
        }

        if (!HasTextUI())
            Debug.LogError($"[SpeechBubbleController] {name}에서 Text 또는 TMP_Text 컴포넌트를 찾을 수 없습니다.");

        s_registry[_speaker] = this;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 말풍선은 Overlay 캔버스라 데스크톱/정면샷 UI 위에 그려진다.
    /// 대신 월드 좌표를 따라가지 않으므로 매 프레임 화면 좌표를 직접 계산한다.
    /// </summary>
    void LateUpdate()
    {
        if (_bubbleRoot == null)
            return;

        // System 말풍선은 화자를 따라가지 않고 화면 상단 중앙에 고정한다.
        if (_speaker == EObject.System)
        {
            Vector2 systemPivot = new Vector2(0.5f, 1f);
            if (_bubbleRoot.pivot != systemPivot)
                _bubbleRoot.pivot = systemPivot;

            SetTextAlignment(TextAnchor.UpperCenter, TextAlignmentOptions.Top);

            _bubbleRoot.position = new Vector3(
                Screen.width * _systemViewportAnchor.x,
                Screen.height * _systemViewportAnchor.y,
                0f);
            return;
        }

        bool desktop = IsDesktopOpen();

        // 데스크톱에서는 화면 오른쪽 끝에 붙으므로 피벗을 오른쪽으로 옮겨야 잘리지 않는다.
        Vector2 pivot = desktop ? new Vector2(1f, 0f) : new Vector2(0.5f, 0f);
        if (_bubbleRoot.pivot != pivot)
            _bubbleRoot.pivot = pivot;

        SetTextAlignment(
            desktop ? TextAnchor.LowerRight : TextAnchor.MiddleCenter,
            desktop ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.Center);

        _bubbleRoot.position = desktop
            ? new Vector3(Screen.width * _desktopViewportAnchor.x, Screen.height * _desktopViewportAnchor.y, 0f)
            : GetOwnerHeadScreenPoint();
    }

    Vector3 GetOwnerHeadScreenPoint()
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null || Owner == null)
            return _bubbleRoot.position;

        Vector3 head = Owner.position + (Vector3)_worldHeadOffset;
        Vector3 point = _camera.WorldToScreenPoint(head);
        point.z = 0f;
        return point;
    }

    bool IsDesktopOpen()
    {
        // 비활성 상태로 시작할 수 있어서 한 번만 찾아 캐싱한다.
        if (!_desktopSearched)
        {
            _desktop = FindAnyObjectByType<ComputerWindowedUI>(FindObjectsInactive.Include);
            _desktopSearched = true;
        }

        return _desktop != null && _desktop.IsOpen;
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
    public void Show(List<SpeechBubbleInfo> speechBubble, bool isAuto = false)
    {
        if (IsTyping)   // 중복 출력 방지
        {
            Debug.Log($"이미 다른 대사가 출력 중입니다");
            return;
        }

        if (speechBubble == null || speechBubble.Count == 0)
        {
            Debug.LogWarning($"[SpeechBubbleController] {name}에 표시할 대사가 없습니다.");
            return;
        }

        if (!ResolveTextUI())
        {
            Debug.LogError($"[SpeechBubbleController] {name}에서 Text 또는 TMP_Text 컴포넌트를 찾을 수 없어 대사를 표시할 수 없습니다.");
            return;
        }

        //Debug.Log($"Show SpeechBubbleInfo");

        _isAuto = isAuto;
        gameObject.SetActive(true);
        SetText(string.Empty);
        IsTyping = true;

        StartCoroutine(CoShow(speechBubble));
    }

    void OnClosed()
    {
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    /// <summary>
    /// 조건 충족 등으로 말풍선을 강제로 닫는다. (System 안내를 코드로 내릴 때 사용)
    /// </summary>
    public void ForceClose()
    {
        if (!gameObject.activeSelf)
            return;

        StopAllCoroutines();
        IsTyping = false;
        _isSkip = false;
        _isAuto = false;
        OnClosed();
    }

    IEnumerator CoShow(List<SpeechBubbleInfo> speechBubble)
    {
        // 대사 출력
        int range = speechBubble.Count - 1;
        for (int idx = 0; idx < range; idx++)
        {
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
            yield return new WaitForSeconds(0.5f);
            _isAuto = false;
            _isSkip = false;
            IsTyping = false;
            OnClosed();
            yield break;
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
    IEnumerator CoShow(SpeechBubbleInfo text, bool isContinuing = false)
    {
        ApplyTextStyle(text);

        if (text.Speed == ESpeechBubbleSpeed.None)
        {
            AppendText(text.Text);
            yield break;
        }

        float interval = GetInterval(text.Speed);
        int range = text.Text.Length - 1;

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

            AppendText(text.Text[idx].ToString());
            yield return new WaitForSeconds(interval);
        }
        AppendText(text.Text[range].ToString());   // 마지막 글자

        if (isContinuing)
            yield break;

        if (_isAuto)
        {
            yield return new WaitForSeconds(0.5f);
            _isAuto = false;
            _isSkip = false;
            IsTyping = false;
            OnClosed();
            yield break;
        }

        // 대사 모두 출력한 후, 초기화
        _isSkip = false;
        IsTyping = false;

        // TODO: _isAuto가 false면, 대사 출력이 완료될 때 아이콘 추가 (ex. ▼)
    }

    void Skip(SpeechBubbleInfo text, int idx = -1)  // 글자가 잘리지 않게 idx 전달한다. 그대로 출력하고 싶다면 -1
    {
        if (_isSkip == false || _isAuto)
            return;

        //Debug.Log($"Skip SpeechBubbleInfo {text.Text}");
        ApplyTextStyle(text);
        AppendText(idx >= 0 ? text.Text[idx..] : text.Text);
    }

    bool ResolveTextUI()
    {
        if (_textUI == null)
            _textUI = GetComponentInChildren<Text>(true);
        if (_tmpTextUI == null)
            _tmpTextUI = GetComponentInChildren<TMP_Text>(true);

        return HasTextUI();
    }

    bool HasTextUI()
    {
        return _textUI != null || _tmpTextUI != null;
    }

    RectTransform GetTextRectTransform()
    {
        if (_textUI != null)
            return _textUI.rectTransform;
        return _tmpTextUI != null ? _tmpTextUI.rectTransform : null;
    }

    void SetText(string value)
    {
        if (_textUI != null)
            _textUI.text = value;
        else if (_tmpTextUI != null)
            _tmpTextUI.text = value;
    }

    void AppendText(string value)
    {
        if (_textUI != null)
            _textUI.text += value;
        else if (_tmpTextUI != null)
            _tmpTextUI.text += value;
    }

    void ApplyTextStyle(SpeechBubbleInfo text)
    {
        Color color = _overrideTextColorWhite ? Color.white : text.FontColor;

        if (_textUI != null)
        {
            _textUI.fontSize = text.FontSize;
            _textUI.color = color;
        }
        else if (_tmpTextUI != null)
        {
            _tmpTextUI.fontSize = text.FontSize;
            _tmpTextUI.color = color;
        }
    }

    void SetTextAlignment(TextAnchor legacyAlignment, TextAlignmentOptions tmpAlignment)
    {
        if (_textUI != null)
        {
            if (_textUI.alignment != legacyAlignment)
                _textUI.alignment = legacyAlignment;
        }
        else if (_tmpTextUI != null && _tmpTextUI.alignment != tmpAlignment)
        {
            _tmpTextUI.alignment = tmpAlignment;
        }
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
