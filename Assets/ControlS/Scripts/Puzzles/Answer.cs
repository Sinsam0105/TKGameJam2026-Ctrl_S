using System;
using UnityEngine;

namespace ControlS
{
    public enum AnswerType
    {
        None,
        Bool,
        Int,
        Float,
        String,
        IntSequence
    }

    [Serializable]
    public struct Answer : IEquatable<Answer>
    {
        [SerializeField] private AnswerType type;
        [SerializeField] private bool boolValue;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;
        [SerializeField] private string stringValue;
        [SerializeField] private int[] intSequence;

        public AnswerType Type => type;

        public static Answer None() => new() { type = AnswerType.None };
        public static Answer From(bool value) => new() { type = AnswerType.Bool, boolValue = value };
        public static Answer From(int value) => new() { type = AnswerType.Int, intValue = value };
        public static Answer From(float value) => new() { type = AnswerType.Float, floatValue = value };
        public static Answer From(string value) => new() { type = AnswerType.String, stringValue = value ?? string.Empty };
        public static Answer From(int[] value) => new()
        {
            type = AnswerType.IntSequence,
            intSequence = value == null ? Array.Empty<int>() : (int[])value.Clone()
        };

        public bool TryGetBool(out bool value) { value = boolValue; return type == AnswerType.Bool; }
        public bool TryGetInt(out int value) { value = intValue; return type == AnswerType.Int; }
        public bool TryGetFloat(out float value) { value = floatValue; return type == AnswerType.Float; }
        public bool TryGetString(out string value) { value = stringValue ?? string.Empty; return type == AnswerType.String; }
        public bool TryGetIntSequence(out int[] value)
        {
            value = intSequence == null ? Array.Empty<int>() : (int[])intSequence.Clone();
            return type == AnswerType.IntSequence;
        }

        public bool Equals(Answer other)
        {
            if (type != other.type) return false;
            return type switch
            {
                AnswerType.None => true,
                AnswerType.Bool => boolValue == other.boolValue,
                AnswerType.Int => intValue == other.intValue,
                AnswerType.Float => Mathf.Approximately(floatValue, other.floatValue),
                AnswerType.String => string.Equals(stringValue ?? string.Empty,
                    other.stringValue ?? string.Empty, StringComparison.Ordinal),
                AnswerType.IntSequence => SequenceEquals(intSequence, other.intSequence),
                _ => false
            };
        }

        public override bool Equals(object obj) => obj is Answer other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(type, boolValue, intValue, floatValue, stringValue);

        private static bool SequenceEquals(int[] left, int[] right)
        {
            left ??= Array.Empty<int>();
            right ??= Array.Empty<int>();
            if (left.Length != right.Length) return false;
            for (var i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }
    }
}
