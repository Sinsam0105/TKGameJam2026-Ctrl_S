using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ControlS
{
    public sealed class InteractionContext
    {
        public GameObject Interactor { get; }
        public GameObject Target { get; }
        public ProgressManager Progress => ProgressManager.Instance;
        public UIManager UI => UIManager.Instance;
        public VirtualDesktop Desktop => VirtualDesktop.Instance;
        public AtmosphereManager Atmosphere => AtmosphereManager.Instance;
        public SoundManager Sound => SoundManager.Instance;

        public InteractionContext(GameObject interactor, GameObject target)
        {
            Interactor = interactor;
            Target = target;
        }
    }

    [Serializable]
    public abstract class InteractionCondition
    {
        public abstract bool Evaluate(InteractionContext context);
    }

    [Serializable]
    public abstract class InteractionAction
    {
        public abstract IEnumerator Execute(InteractionContext context);
    }

    [Serializable]
    public sealed class InteractionRule
    {
        [SerializeField] private string label = "New Rule";
        [SerializeReference] private List<InteractionCondition> conditions = new();
        [SerializeReference] private List<InteractionAction> actions = new();
        [SerializeField] private bool stopAfterExecution = true;
        [SerializeField] private bool executeOnce;

        [NonSerialized] private bool hasExecuted;

        public string Label => label;
        public IReadOnlyList<InteractionCondition> Conditions => conditions;
        public IReadOnlyList<InteractionAction> Actions => actions;
        public bool StopAfterExecution => stopAfterExecution;

        public bool IsSatisfied(InteractionContext context)
        {
            if (executeOnce && hasExecuted) return false;
            foreach (var condition in conditions)
                if (condition == null || !condition.Evaluate(context)) return false;
            return true;
        }

        public IEnumerator Execute(InteractionContext context)
        {
            hasExecuted = true;
            foreach (var action in actions)
            {
                if (action == null) continue;
                yield return action.Execute(context);
            }
        }
    }

    public sealed class InteractionSequence : MonoBehaviour
    {
        [SerializeField] private string sequenceName = "Sequence";
        [SerializeField] private bool playOnStart;
        [SerializeField] private bool blockReentry = true;
        [SerializeReference] private List<InteractionAction> actions = new();

        private Coroutine routine;

        public string SequenceName => sequenceName;
        public IReadOnlyList<InteractionAction> Actions => actions;
        public bool IsRunning => routine != null;

        private void Start()
        {
            if (playOnStart) Execute();
        }

        public void Execute()
        {
            if (blockReentry && routine != null) return;
            routine = StartCoroutine(ExecuteRoutine());
        }

        public void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        private IEnumerator ExecuteRoutine()
        {
            var context = new InteractionContext(null, gameObject);
            foreach (var action in actions)
            {
                if (action == null) continue;
                yield return action.Execute(context);
            }
            routine = null;
        }
    }

    [Serializable]
    public sealed class DelayAction : InteractionAction
    {
        [SerializeField, Min(0f)] private float seconds = .5f;
        public override IEnumerator Execute(InteractionContext context)
        {
            if (seconds > 0f) yield return new WaitForSecondsRealtime(seconds);
        }
    }
}
