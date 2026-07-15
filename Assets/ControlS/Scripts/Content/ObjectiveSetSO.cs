using System;
using System.Collections.Generic;
using UnityEngine;

namespace ControlS
{
    [Serializable]
    public sealed class ObjectiveRule
    {
        [SerializeField] private string objective;
        [SerializeField] private List<ProgressRequirement> requirements = new();
        public string Objective => objective;
        public IReadOnlyList<ProgressRequirement> Requirements => requirements;
    }

    [CreateAssetMenu(fileName = "ObjectiveSet", menuName = "CONTROL S/Content/Objectives")]
    public sealed class ObjectiveSetSO : ScriptableObject
    {
        [SerializeField] private string hudFormat = "Progress {0}/{1} | {2}";
        [SerializeField] private List<ProgressFlagSO> trackedProgress = new();
        [SerializeField] private List<ObjectiveRule> orderedObjectives = new();
        [SerializeField] private string fallbackObjective;
        public string HudFormat => hudFormat;
        public IReadOnlyList<ProgressFlagSO> TrackedProgress => trackedProgress;

        public string GetCurrentObjective(ProgressManager progress)
        {
            if (progress == null) return fallbackObjective;
            foreach (var rule in orderedObjectives)
                if (rule != null && progress.AreRequirementsMet(rule.Requirements)) return rule.Objective;
            return fallbackObjective;
        }

        public int GetCompletedCount(ProgressManager progress)
        {
            if (progress == null) return 0;
            var count = 0;
            foreach (var flag in trackedProgress) if (flag != null && progress.GetFlag(flag)) count++;
            return count;
        }
    }
}
