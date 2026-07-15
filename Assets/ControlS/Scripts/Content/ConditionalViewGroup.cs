using System;
using System.Collections.Generic;
using UnityEngine;

namespace ControlS
{
    [Serializable]
    public sealed class ConditionalView
    {
        [SerializeField] private GameObject view;
        [SerializeReference] private List<InteractionCondition> conditions = new();
        public GameObject View => view;
        public IReadOnlyList<InteractionCondition> Conditions => conditions;
    }

    public sealed class ConditionalViewGroup : MonoBehaviour
    {
        [SerializeField] private List<ConditionalView> views = new();
        private ProgressManager progressManager;

        private void OnEnable()
        {
            progressManager = ProgressManager.Instance;
            if (progressManager != null) progressManager.ProgressChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (progressManager != null) progressManager.ProgressChanged -= Refresh;
            progressManager = null;
        }

        public void Refresh()
        {
            var context = new InteractionContext(null, gameObject);
            var selected = -1;
            for (var i = 0; i < views.Count; i++)
            {
                var candidate = views[i];
                var matches = candidate != null && candidate.View != null;
                if (matches)
                    foreach (var condition in candidate.Conditions)
                        if (condition == null || !condition.Evaluate(context)) { matches = false; break; }
                if (matches) { selected = i; break; }
            }
            for (var i = 0; i < views.Count; i++)
                if (views[i]?.View != null) views[i].View.SetActive(i == selected);
        }
    }
}
