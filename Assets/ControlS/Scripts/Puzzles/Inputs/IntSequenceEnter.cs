using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ControlS
{
    [System.Serializable] public sealed class StringEvent : UnityEvent<string> { }

    public sealed class IntSequenceEnter : BaseEnter
    {
        [SerializeField, Min(1)] private int answerLength = 4;
        [SerializeField] private bool submitWhenFull = true;
        [SerializeField] private bool resetAfterSubmitFailure = true;
        [SerializeField] private string separator = "  >  ";
        [SerializeField] private string emptyText = "(empty)";
        [SerializeField] private StringEvent onSequenceChanged = new();

        private readonly List<int> values = new();

        public IReadOnlyList<int> Values => values;
        public StringEvent OnSequenceChanged => onSequenceChanged;

        public void Push(int value)
        {
            if (values.Count >= answerLength) Reset();
            values.Add(value);
            SetAnswer(Answer.From(values.ToArray()));
            NotifyChanged();
            if (!submitWhenFull || values.Count < answerLength) return;
            var result = EnterAnswer();
            if (resetAfterSubmitFailure && result != PuzzleAttemptResult.Success) Reset();
        }

        public override void Reset()
        {
            values.Clear();
            SetAnswer(Answer.From(values.ToArray()));
            NotifyChanged();
        }

        protected override Answer ReadAnswer() => Answer.From(values.ToArray());

        private void NotifyChanged() =>
            onSequenceChanged?.Invoke(values.Count == 0 ? emptyText : string.Join(separator, values));
    }
}
