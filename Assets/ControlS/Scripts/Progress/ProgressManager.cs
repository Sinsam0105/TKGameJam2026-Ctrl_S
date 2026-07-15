using System;
using System.Collections.Generic;
using Sinsam.SingletonSystem;

namespace ControlS
{
    [AutoSingleton]
    public sealed class ProgressManager : MonoSingleton<ProgressManager>
    {
        private readonly Dictionary<ProgressFlagSO, bool> flags = new();
        private readonly List<ProgressFlagSO> configuredFlags = new();
        private ProgressSetSO progressSet;

        public event Action<ProgressFlagSO, bool> FlagChanged;
        public event Action ProgressChanged;
        public event Action ProgressReset;
        public ProgressSetSO ProgressSet => progressSet;

        public int TotalPuzzleCount
        {
            get
            {
                var count = 0;
                if (configuredFlags.Count > 0)
                {
                    foreach (var flag in configuredFlags)
                        if (flag != null && flag.CountsTowardsProgress) count++;
                    return count;
                }

                foreach (var pair in flags)
                    if (pair.Key != null && pair.Key.CountsTowardsProgress) count++;
                return count;
            }
        }

        public int CompletedPuzzleCount
        {
            get
            {
                var count = 0;
                if (configuredFlags.Count > 0)
                {
                    foreach (var flag in configuredFlags)
                        if (flag != null && flag.CountsTowardsProgress && GetFlag(flag)) count++;
                    return count;
                }

                foreach (var pair in flags)
                    if (pair.Key != null && pair.Key.CountsTowardsProgress && pair.Value) count++;
                return count;
            }
        }

        public float CompletionRatio
        {
            get
            {
                var total = TotalPuzzleCount;
                return total == 0 ? 0f : CompletedPuzzleCount / (float)total;
            }
        }

        public void Configure(ProgressSetSO set)
        {
            progressSet = set;
            configuredFlags.Clear();
            var unique = new HashSet<ProgressFlagSO>();
            if (set != null)
            {
                foreach (var flag in set.Flags)
                {
                    if (flag == null || !unique.Add(flag)) continue;
                    configuredFlags.Add(flag);
                    if (!flags.ContainsKey(flag)) flags.Add(flag, flag.DefaultValue);
                }
            }
            ProgressChanged?.Invoke();
        }

        public bool GetFlag(ProgressFlagSO flag)
        {
            if (flag == null) return false;
            if (flags.TryGetValue(flag, out var value)) return value;
            flags.Add(flag, flag.DefaultValue);
            return flag.DefaultValue;
        }

        public bool TryGetFlag(ProgressFlagSO flag, out bool value)
        {
            if (flag == null)
            {
                value = false;
                return false;
            }

            value = GetFlag(flag);
            return true;
        }

        public void SetFlag(ProgressFlagSO flag) => SetFlag(flag, true);

        public void SetFlag(ProgressFlagSO flag, bool value)
        {
            if (flag == null || GetFlag(flag) == value) return;
            flags[flag] = value;
            FlagChanged?.Invoke(flag, value);
            ProgressChanged?.Invoke();
        }

        public void ToggleFlag(ProgressFlagSO flag)
        {
            if (flag != null) SetFlag(flag, !GetFlag(flag));
        }

        public bool AreRequirementsMet(IReadOnlyList<ProgressRequirement> requirements)
        {
            if (requirements == null) return true;
            for (var i = 0; i < requirements.Count; i++)
                if (requirements[i] == null || !requirements[i].IsMet(this)) return false;
            return true;
        }

        public void ResetProgress()
        {
            var changed = new HashSet<ProgressFlagSO>(flags.Keys);
            foreach (var flag in configuredFlags)
                if (flag != null) changed.Add(flag);
            flags.Clear();
            foreach (var flag in changed)
            {
                if (flag == null) continue;
                flags[flag] = flag.DefaultValue;
                FlagChanged?.Invoke(flag, flag.DefaultValue);
            }
            ProgressReset?.Invoke();
            ProgressChanged?.Invoke();
        }
    }
}
