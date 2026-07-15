using System;
using UnityEngine;
using UnityEngine.Events;

namespace ControlS
{
    [Serializable] public sealed class PuzzleAttemptResultEvent : UnityEvent<PuzzleAttemptResult> { }

    public abstract class BaseEnter : MonoBehaviour
    {
        [SerializeField] private PuzzleRunner targetPuzzle;
        [SerializeField] private bool resetOnEnable = true;
        [SerializeField] private bool resetAfterSuccess;
        [SerializeField] private PuzzleAttemptResultEvent onResult = new();

        private Answer currentAnswer;

        public PuzzleRunner TargetPuzzle => targetPuzzle;
        public Answer CurrentAnswer => currentAnswer;

        protected virtual void OnEnable()
        {
            if (resetOnEnable) Reset();
        }

        public virtual void Reset() => SetAnswer(Answer.None());

        public PuzzleAttemptResult EnterAnswer()
        {
            return EnterAnswer(targetPuzzle);
        }

        public PuzzleAttemptResult EnterAnswer(IPuzzle puzzle)
        {
            if (puzzle == null) return PuzzleAttemptResult.InvalidType;
            SetAnswer(ReadAnswer());
            var result = puzzle.TryAnswer(currentAnswer);
            OnAnswerResult(result);
            onResult?.Invoke(result);
            if (resetAfterSuccess && result == PuzzleAttemptResult.Success) Reset();
            return result;
        }

        protected void SetAnswer(Answer answer) => currentAnswer = answer;
        protected abstract Answer ReadAnswer();
        protected virtual void OnAnswerResult(PuzzleAttemptResult result) { }
    }
}
