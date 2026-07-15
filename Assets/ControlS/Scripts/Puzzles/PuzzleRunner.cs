using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ControlS
{
    [Serializable]
    public sealed class PuzzleOutcomeBinding
    {
        [SerializeReference] private List<InteractionAction> actions = new();
        [SerializeField] private UnityEvent onCompleted = new();

        public IReadOnlyList<InteractionAction> Actions => actions;
        public UnityEvent OnCompleted => onCompleted;
    }

    public class PuzzleRunner : MonoBehaviour, IPuzzle
    {
        [SerializeField] private PuzzleDefinitionSO definition;
        [SerializeField] private PuzzleOutcomeBinding onSuccess = new();
        [SerializeField] private PuzzleOutcomeBinding onIncorrect = new();
        [SerializeField] private PuzzleOutcomeBinding onConditionsNotMet = new();
        [SerializeField] private PuzzleOutcomeBinding onAlreadySolved = new();

        private Coroutine outcomeRoutine;

        public PuzzleDefinitionSO Definition => definition;
        public bool IsResolving => outcomeRoutine != null;

        public PuzzleAttemptResult TryAnswer(Answer answer)
        {
            if (IsResolving) return PuzzleAttemptResult.Busy;
            if (definition == null || definition.Matcher == null) return PuzzleAttemptResult.InvalidType;

            var progress = ProgressManager.Instance;
            if (progress == null || !progress.AreRequirementsMet(definition.Requirements))
            {
                BeginOutcome(OnConditionsNotMet(answer));
                return PuzzleAttemptResult.ConditionsNotMet;
            }

            if (!definition.AllowRepeat && definition.CompletionFlag != null &&
                progress.GetFlag(definition.CompletionFlag))
            {
                BeginOutcome(OnAlreadySolved(answer));
                return PuzzleAttemptResult.AlreadySolved;
            }

            if (!definition.Matcher.Accepts(answer)) return PuzzleAttemptResult.InvalidType;
            if (!definition.Matcher.IsMatch(answer))
            {
                BeginOutcome(OnIncorrect(answer));
                return PuzzleAttemptResult.Incorrect;
            }

            if (definition.CompletionFlag != null) progress.SetFlag(definition.CompletionFlag, true);
            BeginOutcome(OnSuccess(answer));
            return PuzzleAttemptResult.Success;
        }

        protected virtual IEnumerator OnSuccess(Answer answer) => ExecuteOutcome(onSuccess);
        protected virtual IEnumerator OnIncorrect(Answer answer) => ExecuteOutcome(onIncorrect);
        protected virtual IEnumerator OnConditionsNotMet(Answer answer) => ExecuteOutcome(onConditionsNotMet);
        protected virtual IEnumerator OnAlreadySolved(Answer answer) => ExecuteOutcome(onAlreadySolved);

        private void BeginOutcome(IEnumerator routine)
        {
            if (routine != null) outcomeRoutine = StartCoroutine(RunOutcome(routine));
        }

        private IEnumerator RunOutcome(IEnumerator routine)
        {
            yield return routine;
            outcomeRoutine = null;
        }

        private IEnumerator ExecuteOutcome(PuzzleOutcomeBinding outcome)
        {
            if (outcome == null) yield break;
            var context = new InteractionContext(null, gameObject);
            foreach (var action in outcome.Actions)
            {
                if (action == null) continue;
                yield return action.Execute(context);
            }
            outcome.OnCompleted?.Invoke();
        }

        private void OnDisable()
        {
            if (outcomeRoutine != null) StopCoroutine(outcomeRoutine);
            outcomeRoutine = null;
        }
    }
}
