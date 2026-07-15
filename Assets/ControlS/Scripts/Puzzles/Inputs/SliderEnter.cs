using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class SliderEnter : BaseEnter
    {
        [SerializeField] private Slider slider;
        [SerializeField] private bool submitOnValueChanged = true;
        [SerializeField] private float resetValue;

        public void OnValueChanged(float value)
        {
            SetAnswer(Answer.From(value));
            if (submitOnValueChanged) EnterAnswer();
        }

        public override void Reset()
        {
            if (slider != null) slider.SetValueWithoutNotify(resetValue);
            SetAnswer(Answer.From(resetValue));
        }

        protected override Answer ReadAnswer() => Answer.From(slider != null ? slider.value : 0f);
    }
}
