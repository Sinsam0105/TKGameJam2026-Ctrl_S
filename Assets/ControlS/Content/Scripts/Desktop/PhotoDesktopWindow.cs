using UnityEngine;
using UnityEngine.UI;

namespace ControlS
{
    public sealed class PhotoDesktopWindow : MonoBehaviour
    {
        [SerializeField] private Slider brightness;
        [SerializeField] private Image preview;
        [SerializeField] private Image silhouette;
        [SerializeField] private Text clue;
        [SerializeField] private Text label;
        [SerializeField] private SliderEnter enter;
        [SerializeField] private ProgressFlagSO revealedFlag;
        [SerializeField] private PhotoWindowContentSO content;

        public void Refresh()
        {
            var value = revealedFlag != null && ProgressManager.Instance.GetFlag(revealedFlag) ? .88f : .08f;
            if (brightness != null) brightness.SetValueWithoutNotify(value);
            UpdateVisual(value);
        }

        public void OnBrightnessChanged(float value)
        {
            UpdateVisual(value);
            enter?.OnValueChanged(value);
        }

        private void UpdateVisual(float value)
        {
            if (content == null && ContentManager.HasInstance) content = ContentManager.Instance.Current?.photo;
            var light = Mathf.Lerp(.018f, .52f, value);
            if (preview != null) preview.color = new Color(light * .55f, light * .58f, light * .63f, 1f);
            if (silhouette != null) silhouette.color = new Color(light * .34f, light * .29f, light * .27f, 1f);
            if (clue != null)
            {
                clue.color = new Color(.48f, .055f, .045f, Mathf.InverseLerp(.68f, .86f, value));
                if (content != null) clue.text = content.hiddenClue;
            }
            if (label != null) label.text = string.Format(content != null ? content.brightnessFormat : "Brightness {0}%",
                Mathf.RoundToInt(value * 100));
        }
    }
}
