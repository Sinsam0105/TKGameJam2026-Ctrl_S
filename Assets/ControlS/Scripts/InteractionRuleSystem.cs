using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ControlS
{
    /// <summary>조건과 액션이 현재 상호작용에 접근할 때 사용하는 런타임 참조 묶음입니다.</summary>
    public sealed class InteractionContext
    {
        public GameObject Interactor { get; }
        public GameObject Target { get; }
        public ControlSSceneController Scene { get; }
        public ControlSState State => Scene != null ? Scene.State : ControlSState.Current;
        public ControlSContent Content => Scene != null ? Scene.Content : State?.Content;

        public InteractionContext(GameObject interactor, GameObject target, ControlSSceneController scene)
        {
            Interactor = interactor;
            Target = target;
            Scene = scene;
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
        [SerializeReference] private List<InteractionCondition> conditions = new List<InteractionCondition>();
        [SerializeReference] private List<InteractionAction> actions = new List<InteractionAction>();
        [SerializeField] private bool stopAfterExecution = true;
        [SerializeField] private bool executeOnce;

        [NonSerialized] private bool hasExecuted;

        public string Label => label;
        public IReadOnlyList<InteractionCondition> Conditions => conditions;
        public IReadOnlyList<InteractionAction> Actions => actions;
        public bool StopAfterExecution => stopAfterExecution;

        public InteractionRule() { }

        public InteractionRule(string ruleLabel, IEnumerable<InteractionCondition> ruleConditions,
            IEnumerable<InteractionAction> ruleActions, bool stop = true, bool once = false)
        {
            label = ruleLabel;
            conditions = ruleConditions != null
                ? new List<InteractionCondition>(ruleConditions)
                : new List<InteractionCondition>();
            actions = ruleActions != null
                ? new List<InteractionAction>(ruleActions)
                : new List<InteractionAction>();
            stopAfterExecution = stop;
            executeOnce = once;
        }

        public bool IsSatisfied(InteractionContext context)
        {
            if (executeOnce && hasExecuted) return false;
            foreach (var condition in conditions)
            {
                if (condition == null || !condition.Evaluate(context)) return false;
            }
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

    /// <summary>상호작용 외의 인트로·엔딩도 동일한 직렬화 액션 목록으로 실행합니다.</summary>
    public sealed class InteractionSequence : MonoBehaviour
    {
        [SerializeField] private string sequenceName = "Sequence";
        [SerializeField] private bool playOnStart;
        [SerializeField] private bool blockReentry = true;
        [SerializeReference] private List<InteractionAction> actions = new List<InteractionAction>();

        private ControlSSceneController scene;
        private Coroutine routine;

        public string SequenceName => sequenceName;
        public IReadOnlyList<InteractionAction> Actions => actions;
        public bool IsRunning => routine != null;

        public void Configure(string label, bool autoPlay, IEnumerable<InteractionAction> sequenceActions)
        {
            sequenceName = label;
            playOnStart = autoPlay;
            actions = sequenceActions != null
                ? new List<InteractionAction>(sequenceActions)
                : new List<InteractionAction>();
        }

        public void Initialize(ControlSSceneController owner) => scene = owner;

        private void Start()
        {
            if (playOnStart) Execute();
        }

        public void Execute()
        {
            if (blockReentry && routine != null) return;
            if (scene == null) scene = GetComponentInParent<ControlSSceneController>();
            if (scene == null) scene = ControlSSceneController.Current;
            if (scene == null) return;
            routine = StartCoroutine(ExecuteRoutine());
        }

        public void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        private IEnumerator ExecuteRoutine()
        {
            var context = new InteractionContext(null, gameObject, scene);
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

        public DelayAction() { }
        public DelayAction(float duration) => seconds = Mathf.Max(0f, duration);

        public override IEnumerator Execute(InteractionContext context)
        {
            if (seconds > 0f) yield return new WaitForSecondsRealtime(seconds);
        }
    }
}
