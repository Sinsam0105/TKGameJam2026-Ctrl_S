using System.Collections;
using Sinsam.SingletonSystem;
using UnityEngine;

namespace ControlS
{
    public sealed class SoundManager : MonoSingleton<SoundManager>
    {
        private sealed class BgmLayer
        {
            public AudioSource Source;
            public AudioLowPassFilter LowPass;
            public AudioReverbFilter Reverb;
        }

        [Header("Content")]
        [SerializeField] private SoundscapeSO profileOverride;

        [Header("Scene Sources")]
        [SerializeField] private AudioSource droneSource;
        [SerializeField] private AudioSource uiSource;

        private ProgressManager progressManager;
        private ContentManager contentManager;
        private SoundscapeSO profile;
        private BgmLayer originalLayer;
        private BgmLayer damagedLayer;
        private BgmLayer dissonanceLayer;
        private AudioSource glitchSource;
        private AudioClip generatedDroneClip;
        private Coroutine glitchRoutine;
        private float targetCorruption;
        private float currentCorruption;
        private float glitchTimer;
        private bool hasStarted;

        public SoundscapeSO Profile => profile;
        public float Corruption => currentCorruption;
        public bool IsPlaying => originalLayer?.Source != null && originalLayer.Source.isPlaying;

        protected override bool ShouldPersist() => false;

        protected override void Awake()
        {
            base.Awake();
            originalLayer = CreateBgmLayer("BGM - Original");
            damagedLayer = CreateBgmLayer("BGM - Damaged Copy");
            dissonanceLayer = CreateBgmLayer("BGM - Dissonance");
            glitchSource = CreateSource("BGM - Glitch Fragment", false);
            SetupSceneSources();
        }

        private void OnEnable()
        {
            progressManager = ProgressManager.Instance;
            contentManager = ContentManager.Instance;
            if (progressManager != null) progressManager.ProgressChanged += RefreshFromProgress;
            if (contentManager != null) contentManager.ContentChanged += HandleContentChanged;
            ApplyProfile(ResolveProfile());
            RefreshFromProgress();
            if (hasStarted && profile != null && profile.playOnStart) PlayBgm();
        }

        private void Start()
        {
            hasStarted = true;
            PlayBgm();
        }

        private void OnDisable()
        {
            if (progressManager != null) progressManager.ProgressChanged -= RefreshFromProgress;
            if (contentManager != null) contentManager.ContentChanged -= HandleContentChanged;
            progressManager = null;
            contentManager = null;
            if (glitchRoutine != null) StopCoroutine(glitchRoutine);
            glitchRoutine = null;
        }

        protected override void OnDestroy()
        {
            if (generatedDroneClip != null) Destroy(generatedDroneClip);
            base.OnDestroy();
        }

        private void Update()
        {
            if (profile == null) return;
            currentCorruption = Mathf.MoveTowards(currentCorruption, targetCorruption,
                profile.transitionSpeed * Time.unscaledDeltaTime);
            ApplyCorruption(currentCorruption);
            UpdateRandomGlitch();
        }

        public bool ValidateReferences()
        {
            var content = contentManager != null ? contentManager : GetComponent<ContentManager>();
            var configuredProfile = profileOverride != null ? profileOverride : content?.Current?.soundscape;
            return droneSource != null && uiSource != null && configuredProfile != null && configuredProfile.bgm != null;
        }

        public void ApplyProfile(SoundscapeSO value)
        {
            if (value == null || profile == value) return;
            var wasPlaying = IsPlaying;
            profile = value;
            AssignClip(originalLayer.Source, profile.bgm);
            AssignClip(damagedLayer.Source, profile.bgm);
            AssignClip(dissonanceLayer.Source, profile.bgm);
            AssignClip(glitchSource, profile.bgm);
            SetupSceneSources();
            ApplyCorruption(currentCorruption);
            if ((wasPlaying || hasStarted) && profile.playOnStart) PlayBgm();
        }

        public void RefreshFromProgress()
        {
            if (progressManager == null)
            {
                SetCorruption(0f);
                return;
            }

            var trackedCount = contentManager?.Current?.objectives?.TrackedProgress.Count ?? 0;
            if (trackedCount <= 0) trackedCount = profile != null ? profile.fallbackProgressSteps : 4;
            SetCorruption(progressManager.CompletedPuzzleCount / (float)Mathf.Max(1, trackedCount));
        }

        public void SetCorruption(float value)
        {
            targetCorruption = Mathf.Clamp01(value);
        }

        public void SetCorruptionImmediate(float value)
        {
            targetCorruption = currentCorruption = Mathf.Clamp01(value);
            ApplyCorruption(currentCorruption);
        }

        public void PlayBgm()
        {
            if (profile == null || profile.bgm == null || originalLayer.Source.isPlaying) return;
            ApplyCorruption(currentCorruption);
            var startTime = AudioSettings.dspTime + .08d;
            originalLayer.Source.PlayScheduled(startTime);
            damagedLayer.Source.PlayScheduled(startTime);
            dissonanceLayer.Source.PlayScheduled(startTime);
        }

        public void StopBgm()
        {
            originalLayer?.Source.Stop();
            damagedLayer?.Source.Stop();
            dissonanceLayer?.Source.Stop();
            glitchSource?.Stop();
        }

        public void PlayOneShot(AudioClip clip) => PlayOneShot(clip, 1f);

        public void PlayOneShot(AudioClip clip, float volume)
        {
            if (uiSource != null && clip != null) uiSource.PlayOneShot(clip, Mathf.Clamp01(volume));
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

        public void PlayGlitch(float strength)
        {
            if (glitchRoutine == null && profile != null && profile.bgm != null)
                glitchRoutine = StartCoroutine(GlitchFragmentRoutine(Mathf.Clamp01(strength)));
        }

        private SoundscapeSO ResolveProfile() =>
            profileOverride != null ? profileOverride : contentManager?.Current?.soundscape;

        private void HandleContentChanged(GameContentSetSO _)
        {
            ApplyProfile(ResolveProfile());
            RefreshFromProgress();
        }

        private void ApplyCorruption(float corruption)
        {
            if (profile == null || originalLayer == null) return;
            var damage = SmoothRange(corruption, profile.damageStart, 1f);
            var dissonance = Mathf.Pow(SmoothRange(corruption, profile.dissonanceStart, 1f), 2f);
            var collapse = SmoothRange(corruption, profile.collapseStart, 1f);
            var instability = corruption * corruption;
            var randomWobble = Mathf.Lerp(-1f, 1f, Mathf.PerlinNoise(Time.unscaledTime * .17f, 8.31f));
            var wobbleCents = randomWobble * profile.maxPitchWobbleCents * instability;

            originalLayer.Source.volume = profile.bgmVolume * Mathf.Lerp(1f, profile.finalOriginalVolume, collapse);
            damagedLayer.Source.volume = profile.bgmVolume * profile.damagedLayerVolume * damage;
            dissonanceLayer.Source.volume = profile.bgmVolume * profile.dissonanceLayerVolume * dissonance;

            originalLayer.Source.pitch = CentsToRatio(wobbleCents * .16f);
            damagedLayer.Source.pitch = CentsToRatio(Mathf.Lerp(profile.damagedPitchCentsAtStart,
                profile.damagedPitchCentsAtEnd, corruption) + wobbleCents);
            dissonanceLayer.Source.pitch = Mathf.Pow(2f, profile.dissonanceSemitones / 12f) *
                                             CentsToRatio(-wobbleCents * .45f);
            originalLayer.Source.panStereo = randomWobble * profile.maxStereoDrift * instability * .15f;
            damagedLayer.Source.panStereo = -.18f - randomWobble * profile.maxStereoDrift * instability;
            dissonanceLayer.Source.panStereo = .16f + randomWobble * profile.maxStereoDrift * instability;

            var cutoff = Mathf.Lerp(22000f, profile.finalLowPassCutoff, Mathf.Pow(corruption, 1.35f));
            ConfigureFilter(originalLayer, Mathf.Max(cutoff, 6500f), corruption * .3f);
            ConfigureFilter(damagedLayer, cutoff, damage);
            ConfigureFilter(dissonanceLayer, Mathf.Max(profile.finalLowPassCutoff, cutoff * .72f), dissonance);
            if (droneSource != null)
                droneSource.volume = Mathf.Lerp(profile.droneVolume, profile.finalDroneVolume, corruption);
        }

        private void ConfigureFilter(BgmLayer layer, float cutoff, float reverbAmount)
        {
            layer.LowPass.cutoffFrequency = Mathf.Clamp(cutoff, 10f, 22000f);
            layer.LowPass.lowpassResonanceQ = Mathf.Lerp(1f, profile.lowPassResonance, reverbAmount);
            layer.Reverb.dryLevel = 0f;
            layer.Reverb.room = Mathf.Lerp(-10000f, -900f, reverbAmount);
            layer.Reverb.roomHF = Mathf.Lerp(-10000f, -1800f, reverbAmount);
            layer.Reverb.reverbLevel = Mathf.Lerp(-10000f, -650f, reverbAmount);
            layer.Reverb.decayTime = Mathf.Lerp(1f, profile.finalReverbDecay, reverbAmount);
        }

        private void UpdateRandomGlitch()
        {
            if (currentCorruption < profile.glitchStart || glitchRoutine != null) return;
            glitchTimer -= Time.unscaledDeltaTime;
            if (glitchTimer > 0f) return;
            glitchTimer = Random.Range(profile.glitchInterval.x, profile.glitchInterval.y);
            var chance = Mathf.Pow(SmoothRange(currentCorruption, profile.glitchStart, 1f), 3f);
            if (Random.value <= chance) PlayGlitch(currentCorruption);
        }

        private IEnumerator GlitchFragmentRoutine(float strength)
        {
            if (glitchSource == null || originalLayer?.Source == null) yield break;
            var duration = Mathf.Lerp(profile.glitchFragmentDuration.x, profile.glitchFragmentDuration.y, strength);
            var repeatCount = Mathf.RoundToInt(Mathf.Lerp(2f, profile.maxGlitchRepeats, strength));
            var clipLength = profile.bgm.length;
            var start = Mathf.Repeat(originalLayer.Source.time - duration * .5f, Mathf.Max(.01f, clipLength));
            glitchSource.volume = profile.bgmVolume * Mathf.Lerp(.12f, .5f, strength);
            glitchSource.pitch = Mathf.Lerp(1f, .94f, strength);
            glitchSource.panStereo = Random.Range(-profile.maxStereoDrift, profile.maxStereoDrift);
            for (var i = 0; i < repeatCount; i++)
            {
                glitchSource.Stop();
                glitchSource.time = start;
                glitchSource.Play();
                yield return new WaitForSecondsRealtime(duration);
            }
            glitchSource.Stop();
            glitchRoutine = null;
        }

        private void SetupSceneSources()
        {
            if (uiSource != null)
            {
                uiSource.playOnAwake = false;
                uiSource.loop = false;
                uiSource.spatialBlend = 0f;
                uiSource.volume = profile != null ? profile.uiVolume : .8f;
            }
            if (droneSource == null || !Application.isPlaying) return;
            droneSource.playOnAwake = false;
            droneSource.loop = true;
            droneSource.spatialBlend = 0f;
            droneSource.volume = profile != null ? profile.droneVolume : .08f;
            if (droneSource.clip == null)
            {
                generatedDroneClip = CreateElectricalHum();
                droneSource.clip = generatedDroneClip;
            }
            if (!droneSource.isPlaying) droneSource.Play();
        }

        private BgmLayer CreateBgmLayer(string layerName)
        {
            var source = CreateSource(layerName, true);
            var lowPass = source.gameObject.AddComponent<AudioLowPassFilter>();
            var reverb = source.gameObject.AddComponent<AudioReverbFilter>();
            reverb.reverbPreset = AudioReverbPreset.User;
            return new BgmLayer { Source = source, LowPass = lowPass, Reverb = reverb };
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var child = new GameObject(sourceName) { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            return source;
        }

        private static void AssignClip(AudioSource source, AudioClip clip)
        {
            source.Stop();
            source.clip = clip;
        }

        private static AudioClip CreateElectricalHum()
        {
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
            return clip;
        }

        private static float SmoothRange(float value, float start, float end) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));

        private static float CentsToRatio(float cents) => Mathf.Pow(2f, cents / 1200f);
    }
}
