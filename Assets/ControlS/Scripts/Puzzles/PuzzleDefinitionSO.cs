using System;
using System.Collections.Generic;
using UnityEngine;

namespace ControlS
{
    [CreateAssetMenu(fileName = "NewPuzzle", menuName = "CONTROL S/Puzzles/Definition")]
    public sealed class PuzzleDefinitionSO : ScriptableObject
    {
        [SerializeField] private string puzzleId;
        [SerializeReference] private AnswerMatcher matcher = new ExactAnswerMatcher();
        [SerializeField] private List<ProgressRequirement> requirements = new();
        [SerializeField] private ProgressFlagSO completionFlag;
        [SerializeField] private bool allowRepeat;

        public string PuzzleId => puzzleId;
        public AnswerMatcher Matcher => matcher;
        public IReadOnlyList<ProgressRequirement> Requirements => requirements;
        public ProgressFlagSO CompletionFlag => completionFlag;
        public bool AllowRepeat => allowRepeat;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(puzzleId)) puzzleId = Guid.NewGuid().ToString("N");
        }
#endif
    }

    public enum PuzzleAttemptResult
    {
        Success,
        Incorrect,
        ConditionsNotMet,
        AlreadySolved,
        InvalidType,
        Busy
    }

    public interface IPuzzle
    {
        PuzzleAttemptResult TryAnswer(Answer answer);
    }
}
