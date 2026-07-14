using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    /// <summary>아이콘 표시 조건과 클릭 규칙을 직접 직렬화하는 데스크톱 바로가기입니다.</summary>
    public sealed class DesktopShortcut : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeReference] private List<InteractionCondition> visibilityConditions =
            new List<InteractionCondition>();
        [SerializeField] private List<InteractionRule> rules = new List<InteractionRule>();

        private ControlSSceneController scene;
        private ControlSState state;
        private Coroutine routine;

        public IReadOnlyList<InteractionCondition> VisibilityConditions => visibilityConditions;
        public IReadOnlyList<InteractionRule> Rules => rules;

        public void Configure(Button shortcutButton, Text shortcutLabel,
            IEnumerable<InteractionCondition> conditions, IEnumerable<InteractionRule> clickRules)
        {
            button = shortcutButton;
            label = shortcutLabel;
            visibilityConditions = conditions != null
                ? new List<InteractionCondition>(conditions)
                : new List<InteractionCondition>();
            rules = clickRules != null
                ? new List<InteractionRule>(clickRules)
                : new List<InteractionRule>();
        }

        public void Initialize(ControlSSceneController owner)
        {
            scene = owner;
            state = owner != null ? owner.State : ControlSState.Current;
            if (state != null) state.Changed += RefreshVisibility;
            RefreshVisibility();
        }

        public bool ValidateReferences() => button != null && label != null && rules != null && rules.Count > 0;

        public void Execute()
        {
            if (routine != null || scene == null) return;
            routine = StartCoroutine(ExecuteRules());
        }

        public void RefreshVisibility()
        {
            if (scene == null) return;
            var context = new InteractionContext(null, gameObject, scene);
            var visible = true;
            foreach (var condition in visibilityConditions)
            {
                if (condition != null && condition.Evaluate(context)) continue;
                visible = false;
                break;
            }
            gameObject.SetActive(visible);
        }

        private IEnumerator ExecuteRules()
        {
            var context = new InteractionContext(null, gameObject, scene);
            foreach (var rule in rules)
            {
                if (rule == null || !rule.IsSatisfied(context)) continue;
                yield return rule.Execute(context);
                if (rule.StopAfterExecution) break;
            }
            routine = null;
        }

        private void OnDestroy()
        {
            if (state != null) state.Changed -= RefreshVisibility;
        }
    }
}
