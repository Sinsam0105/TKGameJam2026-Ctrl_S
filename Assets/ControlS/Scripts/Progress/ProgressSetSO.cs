using System.Collections.Generic;
using UnityEngine;

namespace ControlS
{
    [CreateAssetMenu(fileName = "ProgressSet", menuName = "CONTROL S/Progress/Set")]
    public sealed class ProgressSetSO : ScriptableObject
    {
        [SerializeField] private List<ProgressFlagSO> flags = new();

        public IReadOnlyList<ProgressFlagSO> Flags => flags;
    }
}
