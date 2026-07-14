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

        public void Configure(Slider slider, Image previewImage, Image silhouetteImage, Text clueText, Text labelText)
        {
            brightness = slider;
            preview = previewImage;
            silhouette = silhouetteImage;
            clue = clueText;
            label = labelText;
        }

        public void Refresh()
        {
            var state = ControlSState.Current;
            if (state == null) return;
            brightness.SetValueWithoutNotify(state.PhotoClueRevealed ? .88f : .08f);
            OnBrightnessChanged(brightness.value);
        }

        public void OnBrightnessChanged(float value)
        {
            var state = ControlSState.Current;
            if (state == null) return;
            var light = Mathf.Lerp(.018f, .52f, value);
            preview.color = new Color(light * .55f, light * .58f, light * .63f, 1f);
            silhouette.color = new Color(light * .34f, light * .29f, light * .27f, 1f);
            clue.color = new Color(.48f, .055f, .045f, Mathf.InverseLerp(.68f, .86f, value));
            label.text = string.Format(state.Content.desktop.brightnessFormat, Mathf.RoundToInt(value * 100));
            if (value >= state.Content.puzzles.photoBrightnessThreshold) state.RevealPhotoClue();
        }
    }
}
