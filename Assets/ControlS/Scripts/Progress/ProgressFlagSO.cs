using System;
using UnityEngine;

namespace ControlS
{
    [CreateAssetMenu(fileName = "NewProgressFlag", menuName = "CONTROL S/Progress/Flag")]
    public sealed class ProgressFlagSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private bool defaultValue;
        [SerializeField] private bool countsTowardsProgress;

        public string Id => id;
        public bool DefaultValue => defaultValue;
        public bool CountsTowardsProgress => countsTowardsProgress;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = Guid.NewGuid().ToString("N");
        }
#endif
    }

    [Serializable]
    public sealed class ProgressRequirement
    {
        [SerializeField] private ProgressFlagSO flag;
        [SerializeField] private bool expectedValue = true;

        public ProgressFlagSO Flag => flag;
        public bool ExpectedValue => expectedValue;

        public bool IsMet(ProgressManager progress) =>
            progress != null && flag != null && progress.GetFlag(flag) == expectedValue;
    }
}
