using UnityEngine;

namespace ControlS
{
    /// <summary>Owns camera pulse and synthesized room/UI audio.</summary>
    public sealed class ControlSAtmosphereController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private AudioSource droneSource;
        [SerializeField] private AudioSource uiSource;

        private ControlSState state;

        public void Configure(Camera sceneCamera, AudioSource drone, AudioSource ui)
        {
            worldCamera = sceneCamera;
            droneSource = drone;
            uiSource = ui;
        }

        public bool ValidateReferences() => worldCamera != null && droneSource != null && uiSource != null;

        public void Initialize(ControlSState gameState)
        {
            state = gameState;
            SetupAudio();
        }

        private void Update()
        {
            if (state == null || worldCamera == null) return;
            var progress = state.CompletedPuzzleCount / 4f;
            var pulse = Mathf.Sin(Time.unscaledTime * (1.25f + progress)) * .004f;
            worldCamera.backgroundColor = new Color(.012f + pulse, .016f, .021f + progress * .006f, 1f);
        }

        public void PlayUiTone(float frequency, float duration)
        {
            if (uiSource == null || !Application.isPlaying) return;
            const int sampleRate = 22050;
            var count = Mathf.Max(64, Mathf.RoundToInt(sampleRate * duration));
            var clip = AudioClip.Create("ui_" + Mathf.RoundToInt(frequency), count, 1, sampleRate, false);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var envelope = 1f - i / (float)count;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * .13f * envelope;
            }
            clip.SetData(data, 0);
            uiSource.PlayOneShot(clip);
            Destroy(clip, duration + .2f);
        }

        private void SetupAudio()
        {
            uiSource.playOnAwake = false;
            uiSource.volume = .8f;
            droneSource.loop = true;
            droneSource.playOnAwake = false;
            droneSource.volume = .08f;
            const int sampleRate = 22050;
            const int seconds = 6;
            var count = sampleRate * seconds;
            var clip = AudioClip.Create("Room electrical hum", count, 1, sampleRate, false);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var time = i / (float)sampleRate;
                var wobble = 1f + Mathf.Sin(time * .73f) * .025f;
                data[i] = (Mathf.Sin(2f * Mathf.PI * 46f * wobble * time) * .38f +
                           Mathf.Sin(2f * Mathf.PI * 92f * time) * .1f) * .22f;
            }
            clip.SetData(data, 0);
            droneSource.clip = clip;
            droneSource.Play();
        }
    }
}
