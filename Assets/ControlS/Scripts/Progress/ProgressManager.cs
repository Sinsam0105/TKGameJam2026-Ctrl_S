using System;
using System.Collections.Generic;
using Sinsam.SingletonSystem;

namespace ControlS
{
    [AutoSingleton]
    public sealed class ProgressManager : MonoSingleton<ProgressManager>
    {
        private readonly Dictionary<ProgressFlagSO, bool> flags = new();

        public event Action<ProgressFlagSO, bool> FlagChanged;
        public event Action ProgressChanged;
        public event Action ProgressReset;

        public int CompletedPuzzleCount
        {
            get
            {
                var count = 0;
                foreach (var pair in flags)
                    if (pair.Key != null && pair.Key.CountsTowardsProgress && pair.Value) count++;
                return count;
            }
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
            if (flags.Count == 0)
            {
                ProgressReset?.Invoke();
                ProgressChanged?.Invoke();
                return;
            }

            var changed = new List<ProgressFlagSO>(flags.Keys);
            flags.Clear();
            foreach (var flag in changed)
                if (flag != null) FlagChanged?.Invoke(flag, flag.DefaultValue);
            ProgressReset?.Invoke();
            ProgressChanged?.Invoke();
        }
    }
}
