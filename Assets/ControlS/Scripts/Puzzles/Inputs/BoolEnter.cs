using UnityEngine;

namespace ControlS
{
    public sealed class BoolEnter : BaseEnter
    {
        [SerializeField] private bool value;

        public void SetValue(bool answer) { value = answer; SetAnswer(Answer.From(value)); }
        public void Enter(bool answer) { SetValue(answer); EnterAnswer(); }
        protected override Answer ReadAnswer() => Answer.From(value);
    }
}
