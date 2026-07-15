using UnityEngine;

namespace ControlS
{
    [CreateAssetMenu(fileName = "Soundscape", menuName = "CONTROL S/Content/Soundscape")]
    public sealed class SoundscapeSO : ScriptableObject
    {
        [Header("Source")]
        public AudioClip bgm;
        [Range(0f, 1f)] public float bgmVolume = .42f;
        [Range(0f, 1f)] public float uiVolume = .8f;
        [Range(0f, 1f)] public float droneVolume = .035f;
        [Range(0f, 1f)] public float finalDroneVolume = .11f;
        public bool playOnStart = true;
        [Min(.01f)] public float transitionSpeed = .22f;

        [Header("Damaged Copy")]
        [Range(0f, 1f)] public float damageStart = .28f;
        [Range(0f, 1f)] public float damagedLayerVolume = .34f;
        [Range(-100f, 100f)] public float damagedPitchCentsAtStart = -7f;
        [Range(-100f, 100f)] public float damagedPitchCentsAtEnd = -32f;
        [Range(0f, 80f)] public float maxPitchWobbleCents = 38f;

        [Header("Dissonance")]
        [Range(0f, 1f)] public float dissonanceStart = .62f;
        [Range(-6f, 6f)] public float dissonanceSemitones = 1f;
        [Range(0f, 1f)] public float dissonanceLayerVolume = .25f;

        [Header("Collapse")]
        [Range(0f, 1f)] public float collapseStart = .78f;
        [Range(0f, 1f)] public float finalOriginalVolume = .38f;
        [Range(10f, 22000f)] public float finalLowPassCutoff = 3200f;
        [Range(1f, 10f)] public float lowPassResonance = 4.5f;
        [Range(.1f, 20f)] public float finalReverbDecay = 7f;
        [Range(0f, 1f)] public float maxStereoDrift = .36f;

        [Header("Glitch Fragments")]
        [Range(0f, 1f)] public float glitchStart = .5f;
        public Vector2 glitchInterval = new(8f, 20f);
        public Vector2 glitchFragmentDuration = new(.12f, .28f);
        [Range(2, 8)] public int maxGlitchRepeats = 5;

#if UNITY_EDITOR
        private void OnValidate()
        {
            damageStart = Mathf.Clamp01(damageStart);
            dissonanceStart = Mathf.Max(damageStart, Mathf.Clamp01(dissonanceStart));
            collapseStart = Mathf.Max(dissonanceStart, Mathf.Clamp01(collapseStart));
            glitchInterval.x = Mathf.Max(.1f, glitchInterval.x);
            glitchInterval.y = Mathf.Max(glitchInterval.x, glitchInterval.y);
            glitchFragmentDuration.x = Mathf.Max(.02f, glitchFragmentDuration.x);
            glitchFragmentDuration.y = Mathf.Max(glitchFragmentDuration.x, glitchFragmentDuration.y);
        }
#endif
    }
}
