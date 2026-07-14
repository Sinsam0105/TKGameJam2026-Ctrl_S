using System;
using System.Collections;
using UnityEngine;

namespace ControlS
{
    [Serializable]
    public sealed class OpenDesktopWindowAction : InteractionAction
    {
        [SerializeField] private DesktopWindow window;

        public OpenDesktopWindowAction() { }
        public OpenDesktopWindowAction(DesktopWindow target) => window = target;

        public override IEnumerator Execute(InteractionContext context)
        {
            window?.Open();
            yield break;
        }
    }
}
