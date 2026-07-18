using System.Collections;
using UnityEditor;
using UnityEngine;

public class FullScreenGlitchController : MonoBehaviour
{
    [SerializeField] Material _material;

    readonly int IntensityId = Shader.PropertyToID("_Intensity");

    Coroutine _coPlay = null;
    Coroutine _coPlayBurst = null;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        if (_material == null)
        {
            string name = transform.name.Replace("(Clone)", "").Trim();
            _material = Resources.Load<Material>($"Material/{name}");
        }

        Debug.Log($"FullScreenGlitchController Init: {_material}");
        Stop();

        // test
        PlayBurst(1f);
        //Play(0.5f, 0.35f);
    }

    /// <summary>
    /// 화면 전체에 글리치 효과를 무한 재생한다.
    /// </summary>
    /// <param name="intensity">글리치 강도 (0 ~ 1)</param>
    public void Play(float intensity)
    {
        Play(-1, intensity);
    }

    /// <summary>
    /// 몇 초 동안 화면 전체에 글리치 효과를 재생한다.
    /// </summary>
    /// <param name="duration">지속 시간</param>
    /// <param name="intensity">글리치 강도 (0 ~ 1)</param>
    /// <returns></returns>
    public void Play(float duration, float intensity)
    {
        Stop();
        intensity = Mathf.Clamp(intensity, 0.5f, 1f);
        _coPlay = StartCoroutine(CoPlay(duration, intensity));
    }

    /// <summary>
    /// 화면 전체가 랜덤으로 깜빡거리며 글리치 효과를 무한 재생한다.
    /// </summary>
    /// <param name="maxIntensity"></param>
    public void PlayBurst(float maxIntensity)
    {
        PlayBurst(-1, maxIntensity);
    }

    /// <summary>
    /// 몇 초 동안 화면 전체가 랜덤으로 깜빡거리며 글리치 효과를 재생한다.
    /// </summary>
    /// <param name="duration">지속 시간</param>
    /// <param name="maxIntensity">최대 글리치 강도 (0 ~ 1)</param>
    public void PlayBurst(float duration, float maxIntensity)
    {
        Stop();
        maxIntensity = Mathf.Clamp(maxIntensity, 0.5f, 1f);
        if (duration == -1)
            _coPlayBurst = StartCoroutine(CoPlayBurst(maxIntensity));
        else
            _coPlayBurst = StartCoroutine(CoPlayBurst(duration, maxIntensity));
    }

    /// <summary>
    /// 재생 중인 글리치 효과를 끈다.
    /// </summary>
    public void Stop()
    {
        if (_coPlay != null)
        {
            StopCoroutine(_coPlay);
            _coPlay = null;
        }

        if (_coPlayBurst != null)
        {
            StopCoroutine(_coPlayBurst);
            _coPlayBurst = null;
        }
    }

    IEnumerator CoPlay(float duration, float intensity)
    {
        SetIntensity(intensity);
        // TODO: To 팀장님. 나중에 글리치 효과음 넣어주세용 (지지지직)

        if (duration != -1) // -1이면 멈추지 않고 무한 재생
        {
            yield return new WaitForSeconds(duration);
            SetIntensity(0f);   // 정상 화면
            _coPlay = null;
        }
    }

    IEnumerator CoPlayBurst(float maxIntensity)
    {
        while (true)
        {
            float chance = Random.Range(0.1f, 0.5f);
            float interval = Random.Range(0.03f, 0.2f);

            if (Random.value <= chance)
            {
                float minIntensity = Mathf.Min(0.5f, maxIntensity);
                SetIntensity(Random.Range(minIntensity, maxIntensity));
            }
            else
                SetIntensity(0f);

            yield return new WaitForSeconds(interval);
        }
    }

    IEnumerator CoPlayBurst(float duration, float maxIntensity)
    {
        int minCount = Mathf.Max(1, Mathf.FloorToInt(duration * 3f));
        int maxCount = Mathf.Max(minCount + 1, Mathf.CeilToInt(duration * 6f));
        
        int burstCount = Random.Range(minCount, maxCount);
        float interval = duration / burstCount;
        
        for (int i = 0; i < burstCount; i++)
        {
            float playDuration = interval * Random.Range(0.3f, 0.7f);
            float stopDuration = interval - playDuration;
            float minIntensity = Mathf.Min(0.5f, maxIntensity);

            SetIntensity(Random.Range(minIntensity, maxIntensity));
            yield return new WaitForSeconds(playDuration);

            SetIntensity(0f);
            yield return new WaitForSeconds(stopDuration);
        }

        SetIntensity(0f);
        _coPlayBurst = null;
    }

    /// <summary>
    /// 0(정상 화면) ~ 1(글리치 강도 최대)
    /// </summary>
    /// <param name="intensity"></param>
    void SetIntensity(float intensity)
    {
        _material.SetFloat(IntensityId, Mathf.Clamp01(intensity));
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    // 에디터에서 플레이 모드가 끝나면 글리치 강도 초기화
    // Stop()으로 실행 중인 코루틴을 먼저 멈추지 않으면, 코루틴이 한 번 더 틱하며 SetIntensity(0f)를 덮어쓴다 
    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Stop();
            SetIntensity(0f);
        }
    }
#endif
}