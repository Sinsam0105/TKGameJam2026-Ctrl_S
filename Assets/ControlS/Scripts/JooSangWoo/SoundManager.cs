using Sinsam.SingletonSystem;
using System.Collections;
using UnityEngine;

public sealed class SoundManager : MonoSingleton<SoundManager>
{

    [Header("Clips")]
    [Tooltip("정상 상태에서 재생할 원곡. 예: Comfortable Mystery")]
    [SerializeField] private AudioClip musicClip;

    [Tooltip("선택 사항. 팬, HDD, 전기 노이즈 등의 루프 클립")]
    [SerializeField] private AudioClip noiseLoopClip;

    [Tooltip("선택 사항. DAW/Audacity에서 미리 역재생·늘이기 처리한 원곡 텍스처")]
    [SerializeField] private AudioClip reverseTextureClip;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] private float progress;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 0.8f;
    [SerializeField, Min(0.01f)] private float progressSmoothTime = 0.8f;
    [SerializeField] private bool playOnStart = true;

    [Header("Corruption")]
    [Tooltip("손상 레이어의 최종 기본 재생 속도")]
    [SerializeField, Range(0.90f, 1f)] private float damagedFinalPitch = 0.985f;

    [Tooltip("손상 레이어의 최대 불규칙 피치 흔들림")]
    [SerializeField, Range(0f, 80f)] private float maxFlutterCents = 35f;

    [Tooltip("불협화 레이어의 음정 차이. 1이면 반음 위")]
    [SerializeField, Range(-12f, 12f)] private float dissonanceSemitones = 1f;

    [Tooltip("이 진행도부터 간헐적 버퍼 반복 글리치가 발생")]
    [SerializeField, Range(0f, 1f)] private float glitchStartProgress = 0.48f;
    private AudioSource _original;
    private AudioSource _damaged;
    private AudioSource _dissonance;
    private AudioSource _noise;
    private AudioSource _reverseTexture;
    private AudioSource _glitch;

    private AudioLowPassFilter _originalLowPass;
    private AudioLowPassFilter _damagedLowPass;
    private AudioLowPassFilter _dissonanceLowPass;

    private AudioReverbFilter _damagedReverb;
    private AudioReverbFilter _dissonanceReverb;
    private AudioReverbFilter _reverseReverb;

    private float _currentProgress => GameConditionManager.Instance.GameProgress;
    private float _progressVelocity;
    private float _duck = 1f;
    private bool _isStarted;
    private bool _isPaused;
    private double _nextGlitchDspTime;
    private Coroutine _glitchRoutine;

    private readonly float[] _flutterSeeds = { 12.31f, 43.87f, 91.17f };
    public bool IsPlaying => _isStarted && !_isPaused;

    private void Start()
    {
        if (playOnStart)
            PlayMusic();
    }

    private void Update()
    {
        ApplyAudioState(_currentProgress);
        UpdateGlitchScheduler(_currentProgress);
    }

    private void OnValidate()
    {
        progress = Mathf.Clamp01(progress);
        masterVolume = Mathf.Clamp01(masterVolume);
        progressSmoothTime = Mathf.Max(0.01f, progressSmoothTime);

        if (Application.isPlaying)
            ApplyAudioState(progress);
    }
    public void PlayMusic()
    {
        if (musicClip == null)
        {
            Debug.LogError(
                $" Music Clip이 비어 있습니다.",
                this
            );
            return;
        }

        StopSourcesOnly();

        double startTime = AudioSettings.dspTime + 0.15d;

        PrepareLoopSource(_original, musicClip);
        PrepareLoopSource(_damaged, musicClip);
        PrepareLoopSource(_dissonance, musicClip);

        _original.PlayScheduled(startTime);
        _damaged.PlayScheduled(startTime);
        _dissonance.PlayScheduled(startTime);

        if (noiseLoopClip != null)
        {
            PrepareLoopSource(_noise, noiseLoopClip);
            _noise.PlayScheduled(startTime);
        }

        if (reverseTextureClip != null)
        {
            PrepareLoopSource(_reverseTexture, reverseTextureClip);
            _reverseTexture.PlayScheduled(startTime);
        }

        _isStarted = true;
        _isPaused = false;
        _duck = 1f;
        _nextGlitchDspTime = startTime + 12d;
    }

    public void StopMusic()
    {
        if (_glitchRoutine != null)
        {
            StopCoroutine(_glitchRoutine);
            _glitchRoutine = null;
        }

        StopSourcesOnly();
        _isStarted = false;
        _isPaused = false;
        _duck = 1f;
    }

    public void PauseMusic()
    {
        if (!_isStarted || _isPaused)
            return;

        PauseSource(_original);
        PauseSource(_damaged);
        PauseSource(_dissonance);
        PauseSource(_noise);
        PauseSource(_reverseTexture);
        PauseSource(_glitch);

        _isPaused = true;
    }

    public void ResumeMusic()
    {
        if (!_isStarted || !_isPaused)
            return;

        UnPauseSource(_original);
        UnPauseSource(_damaged);
        UnPauseSource(_dissonance);
        UnPauseSource(_noise);
        UnPauseSource(_reverseTexture);
        UnPauseSource(_glitch);

        _isPaused = false;
        _nextGlitchDspTime = AudioSettings.dspTime + 3d;
    }

    public void TriggerGlitchNow(float strength = 1f)
    {
        if (!_isStarted || _isPaused || musicClip == null)
            return;

        StartGlitchBurst(Mathf.Clamp01(strength));
    }

    public void ResyncLayers()
    {
        if (!_isStarted || musicClip == null)
            return;

        int sample = GetSafeTimeSamples(_original, musicClip.samples);
        SetSafeTimeSamples(_damaged, sample);
        SetSafeTimeSamples(_dissonance, sample);
    }

    private void BuildAudioGraph()
    {
        _original = CreateSource("BGM - Original", true);
        _damaged = CreateSource("BGM - Damaged Copy", true);
        _dissonance = CreateSource("BGM - Dissonance", true);
        _noise = CreateSource("BGM - Computer Noise", true);
        _reverseTexture = CreateSource("BGM - Reverse Texture", true);
        _glitch = CreateSource("BGM - Glitch Fragment", false);

        _originalLowPass = _original.gameObject.AddComponent<AudioLowPassFilter>();
        _damagedLowPass = _damaged.gameObject.AddComponent<AudioLowPassFilter>();
        _dissonanceLowPass = _dissonance.gameObject.AddComponent<AudioLowPassFilter>();

        _damagedReverb = ConfigureReverb(
            _damaged.gameObject.AddComponent<AudioReverbFilter>()
        );
        _dissonanceReverb = ConfigureReverb(
            _dissonance.gameObject.AddComponent<AudioReverbFilter>()
        );
        _reverseReverb = ConfigureReverb(
            _reverseTexture.gameObject.AddComponent<AudioReverbFilter>()
        );
    }

    private AudioSource CreateSource(string objectName, bool loop)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.priority = 32;
        source.volume = 0f;
        return source;
    }

    private static AudioReverbFilter ConfigureReverb(AudioReverbFilter filter)
    {
        filter.reverbPreset = AudioReverbPreset.User;
        filter.dryLevel = 0f;
        filter.room = -1000f;
        filter.roomHF = -1500f;
        filter.decayTime = 1.2f;
        filter.reverbLevel = -10000f;
        return filter;
    }

    private void ApplyAudioState(float p)
    {
        if (_original == null)
            return;

        float damage = Smooth01(Mathf.InverseLerp(0.25f, 1f, p));
        float structureDamage = Smooth01(Mathf.InverseLerp(0.48f, 1f, p));
        float severe = Smooth01(Mathf.InverseLerp(0.68f, 1f, p));
        float finalStage = Smooth01(Mathf.InverseLerp(0.88f, 1f, p));

        float time = Time.unscaledTime;

        float originalFlutter = BipolarPerlin(_flutterSeeds[0], time * 0.13f);
        float damagedFlutter = BipolarPerlin(_flutterSeeds[1], time * 0.21f);
        float dissonanceFlutter = BipolarPerlin(_flutterSeeds[2], time * 0.17f);

        float originalCents = originalFlutter * Mathf.Lerp(0f, 3f, severe);
        float damagedCents =
            damagedFlutter * Mathf.Lerp(0f, maxFlutterCents, severe);
        float dissonanceCents =
            dissonanceFlutter * Mathf.Lerp(0f, maxFlutterCents * 0.65f, severe);

        _original.volume =
            masterVolume
            * Mathf.Lerp(1f, 0.42f, severe)
            * Mathf.Lerp(1f, 0.72f, finalStage)
            * _duck;

        _damaged.volume =
            masterVolume
            * Mathf.Lerp(0f, 0.52f, damage)
            * _duck;

        _dissonance.volume =
            masterVolume
            * Mathf.Lerp(0f, 0.22f, severe)
            * _duck;

        _noise.volume =
            noiseLoopClip == null
                ? 0f
                : masterVolume * Mathf.Lerp(0f, 0.15f, Smooth01(p));

        _reverseTexture.volume =
            reverseTextureClip == null
                ? 0f
                : masterVolume
                  * Mathf.Lerp(0f, 0.17f, structureDamage)
                  * Mathf.Lerp(1f, 1.3f, finalStage);

        _original.pitch = CentsToRatio(originalCents);

        float damagedBasePitch = Mathf.Lerp(1f, damagedFinalPitch, damage);
        _damaged.pitch = damagedBasePitch * CentsToRatio(damagedCents);

        float dissonanceRatio = SemitonesToRatio(dissonanceSemitones);
        float dissonanceBasePitch = Mathf.Lerp(1f, dissonanceRatio, severe);
        _dissonance.pitch =
            dissonanceBasePitch * CentsToRatio(dissonanceCents);

        _noise.pitch = Mathf.Lerp(1f, 0.94f, severe);
        _reverseTexture.pitch = Mathf.Lerp(1f, 0.88f, severe);

        _originalLowPass.cutoffFrequency =
            LogLerp(22000f, 5200f, structureDamage);

        _damagedLowPass.cutoffFrequency =
            LogLerp(18000f, 2500f, severe);

        _dissonanceLowPass.cutoffFrequency =
            LogLerp(4800f, 2200f, severe);

        _originalLowPass.lowpassResonanceQ =
            Mathf.Lerp(1f, 2.2f, structureDamage);

        _damagedLowPass.lowpassResonanceQ =
            Mathf.Lerp(1f, 5.5f, severe);

        _dissonanceLowPass.lowpassResonanceQ =
            Mathf.Lerp(1f, 3.8f, severe);

        ApplyReverb(
            _damagedReverb,
            damage,
            minDecay: 1.1f,
            maxDecay: 5.5f,
            maxLevel: -1100f
        );

        ApplyReverb(
            _dissonanceReverb,
            severe,
            minDecay: 2.2f,
            maxDecay: 7.5f,
            maxLevel: -700f
        );

        ApplyReverb(
            _reverseReverb,
            structureDamage,
            minDecay: 2.8f,
            maxDecay: 8.5f,
            maxLevel: -500f
        );

        float panDrift =
            Mathf.Sin(time * Mathf.Lerp(0.08f, 0.31f, severe))
            * 0.28f
            * severe;

        _original.panStereo = 0f;
        _damaged.panStereo = Mathf.Clamp(-0.12f * damage + panDrift, -1f, 1f);
        _dissonance.panStereo =
            Mathf.Clamp(0.18f * severe - panDrift * 0.8f, -1f, 1f);

        _noise.panStereo = -panDrift * 0.35f;
        _reverseTexture.panStereo = panDrift * 0.7f;
    }

    private void UpdateGlitchScheduler(float p)
    {
        if (!_isStarted || _isPaused || musicClip == null)
            return;

        if (p < glitchStartProgress)
            return;

        double now = AudioSettings.dspTime;
        if (now < _nextGlitchDspTime)
            return;

        float intensity = Smooth01(
            Mathf.InverseLerp(glitchStartProgress, 1f, p)
        );

        float interval = Mathf.Lerp(28f, 6f, intensity);
        _nextGlitchDspTime =
            now + interval * Random.Range(0.75f, 1.25f);

        float triggerChance = Mathf.Lerp(0.35f, 1f, intensity);
        if (Random.value <= triggerChance)
            StartGlitchBurst(intensity);
    }

    private void StartGlitchBurst(float intensity)
    {
        if (_glitchRoutine != null)
            StopCoroutine(_glitchRoutine);

        _glitchRoutine = StartCoroutine(GlitchBurst(intensity));
    }

    private IEnumerator GlitchBurst(float intensity)
    {
        int repeatCount = Mathf.RoundToInt(Mathf.Lerp(2f, 7f, intensity));
        float fragmentDuration = Mathf.Lerp(0.22f, 0.075f, intensity);
        float gap = Mathf.Lerp(0.045f, 0.008f, intensity);

        int currentSample = GetSafeTimeSamples(_original, musicClip.samples);
        int searchWindow = Mathf.RoundToInt(musicClip.frequency * 0.35f);
        int fragmentSample = Mathf.Clamp(
            currentSample + Random.Range(-searchWindow, searchWindow),
            0,
            Mathf.Max(0, musicClip.samples - 2)
        );

        _duck = Mathf.Lerp(0.72f, 0.18f, intensity);

        for (int i = 0; i < repeatCount; i++)
        {
            _glitch.Stop();
            _glitch.clip = musicClip;
            _glitch.loop = false;
            _glitch.volume =
                masterVolume
                * Mathf.Lerp(0.10f, 0.34f, intensity)
                * (1f - i * 0.035f);

            _glitch.pitch =
                Mathf.Lerp(1f, 0.93f, intensity)
                * CentsToRatio(Random.Range(-12f, 12f) * intensity);

            _glitch.panStereo =
                Random.Range(-0.75f, 0.75f) * intensity;

            SetSafeTimeSamples(_glitch, fragmentSample);
            _glitch.Play();

            yield return new WaitForSecondsRealtime(fragmentDuration);
            _glitch.Stop();

            fragmentDuration *= 1.04f;
            yield return new WaitForSecondsRealtime(gap);
        }

        if (intensity > 0.82f && Random.value < intensity)
        {
            _duck = 0f;
            yield return new WaitForSecondsRealtime(
                Random.Range(0.08f, 0.22f)
            );
        }

        _duck = 1f;
        _glitchRoutine = null;
    }

    private static void ApplyReverb(
        AudioReverbFilter filter,
        float amount,
        float minDecay,
        float maxDecay,
        float maxLevel
    )
    {
        if (filter == null)
            return;

        filter.decayTime = Mathf.Lerp(minDecay, maxDecay, amount);
        filter.reverbLevel = Mathf.Lerp(-10000f, maxLevel, amount);
        filter.roomHF = Mathf.Lerp(-1800f, -4200f, amount);
    }

    private static void PrepareLoopSource(AudioSource source, AudioClip clip)
    {
        source.Stop();
        source.clip = clip;
        source.loop = true;
        source.timeSamples = 0;
    }

    private void StopSourcesOnly()
    {
        StopSource(_original);
        StopSource(_damaged);
        StopSource(_dissonance);
        StopSource(_noise);
        StopSource(_reverseTexture);
        StopSource(_glitch);
    }

    private static void StopSource(AudioSource source)
    {
        if (source != null)
            source.Stop();
    }

    private static void PauseSource(AudioSource source)
    {
        if (source != null && source.isPlaying)
            source.Pause();
    }

    private static void UnPauseSource(AudioSource source)
    {
        if (source != null)
            source.UnPause();
    }

    private static int GetSafeTimeSamples(AudioSource source, int clipSamples)
    {
        if (source == null || source.clip == null || clipSamples <= 1)
            return 0;

        try
        {
            return Mathf.Clamp(source.timeSamples, 0, clipSamples - 2);
        }
        catch
        {
            return 0;
        }
    }

    private static void SetSafeTimeSamples(AudioSource source, int sample)
    {
        if (source == null || source.clip == null || source.clip.samples <= 1)
            return;

        try
        {
            source.timeSamples = Mathf.Clamp(
                sample,
                0,
                source.clip.samples - 2
            );
        }
        catch
        {
            // 일부 압축/스트리밍 설정에서 즉시 시킹이 실패할 수 있으므로 무시.
        }
    }

    private static float BipolarPerlin(float seed, float time)
    {
        return Mathf.PerlinNoise(seed, time) * 2f - 1f;
    }

    private static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static float LogLerp(float from, float to, float t)
    {
        from = Mathf.Max(0.001f, from);
        to = Mathf.Max(0.001f, to);
        return Mathf.Exp(Mathf.Lerp(Mathf.Log(from), Mathf.Log(to), t));
    }

    private static float CentsToRatio(float cents)
    {
        return Mathf.Pow(2f, cents / 1200f);
    }

    private static float SemitonesToRatio(float semitones)
    {
        return Mathf.Pow(2f, semitones / 12f);
    }
}
