using System;
using UnityEngine;

namespace ControlS
{
    [Serializable]
    public abstract class AnswerMatcher
    {
        public abstract AnswerType ExpectedType { get; }
        public virtual bool Accepts(Answer answer) => answer.Type == ExpectedType;
        public abstract bool IsMatch(Answer answer);
    }

    [Serializable]
    public sealed class ExactAnswerMatcher : AnswerMatcher
    {
        [SerializeField] private Answer expected;
        public override AnswerType ExpectedType => expected.Type;
        public override bool IsMatch(Answer answer) => Accepts(answer) && expected.Equals(answer);
        public ExactAnswerMatcher() { }
        public ExactAnswerMatcher(Answer value) => expected = value;
    }

    [Serializable]
    public sealed class StringAnswerMatcher : AnswerMatcher
    {
        [SerializeField] private string expected;
        [SerializeField] private bool trim = true;
        [SerializeField] private bool ignoreSpaces;
        [SerializeField] private bool ignoreColons;
        [SerializeField] private bool ignoreCase;

        public override AnswerType ExpectedType => AnswerType.String;

        public StringAnswerMatcher() { }
        public StringAnswerMatcher(string value, bool ignoreWhitespace = false, bool ignorePunctuationColons = false,
            bool caseInsensitive = false)
        {
            expected = value;
            ignoreSpaces = ignoreWhitespace;
            ignoreColons = ignorePunctuationColons;
            ignoreCase = caseInsensitive;
        }

        public override bool IsMatch(Answer answer)
        {
            if (!answer.TryGetString(out var value)) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(Normalize(value), Normalize(expected), comparison);
        }

        private string Normalize(string value)
        {
            value ??= string.Empty;
            if (trim) value = value.Trim();
            if (ignoreSpaces) value = value.Replace(" ", string.Empty);
            if (ignoreColons) value = value.Replace(":", string.Empty);
            return value;
        }
    }

    public enum FloatComparisonMode { Approximately, GreaterOrEqual, LessOrEqual, Range }

    [Serializable]
    public sealed class FloatAnswerMatcher : AnswerMatcher
    {
        [SerializeField] private FloatComparisonMode mode;
        [SerializeField] private float expected;
        [SerializeField] private float maximum = 1f;
        [SerializeField, Min(0f)] private float tolerance = .001f;

        public override AnswerType ExpectedType => AnswerType.Float;

        public FloatAnswerMatcher() { }
        public FloatAnswerMatcher(FloatComparisonMode comparison, float value, float max = 1f, float epsilon = .001f)
        {
            mode = comparison;
            expected = value;
            maximum = max;
            tolerance = Mathf.Max(0f, epsilon);
        }

        public override bool IsMatch(Answer answer)
        {
            if (!answer.TryGetFloat(out var value)) return false;
            return mode switch
            {
                FloatComparisonMode.Approximately => Mathf.Abs(value - expected) <= tolerance,
                FloatComparisonMode.GreaterOrEqual => value >= expected,
                FloatComparisonMode.LessOrEqual => value <= expected,
                FloatComparisonMode.Range => value >= Mathf.Min(expected, maximum) && value <= Mathf.Max(expected, maximum),
                _ => false
            };
        }
    }

    [Serializable]
    public sealed class IntSequenceAnswerMatcher : AnswerMatcher
    {
        [SerializeField] private int[] expected = Array.Empty<int>();
        public override AnswerType ExpectedType => AnswerType.IntSequence;
        public IntSequenceAnswerMatcher() { }
        public IntSequenceAnswerMatcher(int[] value) => expected = value == null ? Array.Empty<int>() : (int[])value.Clone();

        public override bool IsMatch(Answer answer)
        {
            if (!answer.TryGetIntSequence(out var value) || value.Length != expected.Length) return false;
            for (var i = 0; i < expected.Length; i++) if (value[i] != expected[i]) return false;
            return true;
        }
    }
}
