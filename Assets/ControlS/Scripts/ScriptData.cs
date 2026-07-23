using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptData
{
    public enum ESpeechBubbleSpeed
    {
        None = 0,       // 바로 출력
        VerySlow = 2,   // 거의 뭐 한 글자씩 출력되는 수준
        Slow = 10,
        Normal = 20,    // 기본 속도
        Fast = 40,
        VeryFast = 100, // 너무 빨라서 눈으로 따라가기 힘들다
    }

    public enum EObject
    {
        None = 0,
        System,
        Player,
        Monster,
    }

    public enum EExpression
    {
        None,           // 무표정
        Smile,          // 웃기
        Anxious,            // 불안
        Sighing,            // 한숨
        Suspicious,         // 의심
        Angry,              // 화남
        Working,            // 작업 중일 때 표정
        Scared,             // 무서움
        Dizzy,              // 어지러움
        HoldingIntercom,    // 인터폰을 들고 있는 모습
        PrayingForSave      // 저장되길 비는 모습
    }

    /// <summary>
    /// 컷씬/상황별 대본
    /// 어마어마한 대사 분량을 고려하여 json을 추천한다.
    /// Assets/ControlS/Resources/Data/Scripts 안에 넣으면 된다.
    /// </summary>
    [Serializable]
    public class ScriptData
    {
        public string Description;  // 상황 간단 설명 (ex. 프롤로그 대본)
        public bool IsAuto; // 자동으로 대사로 넘어가는가 (false: 수동), 상호작용 불가 (스킵/닫기 등)
        public List<SpeechBubbleData> Bubbles; // 대본 (말풍선들)
    }

    [Serializable]
    public class SpeechBubbleData   // TODO: 구조 재설계
    {
        public EObject ObjectType;  // 누구의 말풍선
        public EExpression Expression;  // 표정
        public List<SpeechBubbleInfo> Bubble;   // 말풍선 하나
    }

    [Serializable]
    public struct SpeechBubbleInfo
    {
        public string Text;
        /// <summary>
        /// 높을수록 빠르게 출력한다. (1/Speed = 출력 간격)
        /// </summary>
        public ESpeechBubbleSpeed Speed;
        public int FontSize;
        public float R, G, B, A;

        [JsonIgnore] public Color FontColor => new Color(R, G, B, A);
    }
}