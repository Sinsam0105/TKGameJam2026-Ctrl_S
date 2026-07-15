using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class DesktopShortcut : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeReference] private List<InteractionCondition> visibilityConditions = new();
        [SerializeField] private List<InteractionRule> rules = new();

        private Coroutine routine;

        public IReadOnlyList<InteractionCondition> VisibilityConditions => visibilityConditions;
        public IReadOnlyList<InteractionRule> Rules => rules;

        private void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        public bool ValidateReferences() => button != null && label != null && rules != null && rules.Count > 0;

        public void Execute()
        {
            if (routine == null) routine = StartCoroutine(ExecuteRules());
        }

        public void RefreshVisibility()
        {
            var context = new InteractionContext(null, gameObject);
            foreach (var condition in visibilityConditions)
            {
                if (condition != null && condition.Evaluate(context)) continue;
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
        }

        private IEnumerator ExecuteRules()
        {
            var context = new InteractionContext(null, gameObject);
            foreach (var rule in rules)
            {
                if (rule == null || !rule.IsSatisfied(context)) continue;
                yield return rule.Execute(context);
                if (rule.StopAfterExecution) break;
            }
            routine = null;
        }
    }
}
