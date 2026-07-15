using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class TextEnter : BaseEnter
    {
        [SerializeField] private InputField input;
        [SerializeField] private bool focusOnEnable = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (focusOnEnable && input != null)
            {
                input.Select();
                input.ActivateInputField();
            }
        }

        public override void Reset()
        {
            if (input != null) input.SetTextWithoutNotify(string.Empty);
            base.Reset();
        }

        protected override Answer ReadAnswer() => Answer.From(input != null ? input.text : string.Empty);
    }
}
