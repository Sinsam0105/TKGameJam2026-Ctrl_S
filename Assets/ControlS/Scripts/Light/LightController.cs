using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightController : MonoBehaviour
{
    public Transform Owner { get; protected set; }
    protected Light2D _light;

    protected Coroutine _coFlicker = null;

    #region Light2D 속성
    protected Color _defaultColor;
    public Color Color
    {
        get => _light.color;
        set => _light.color = value;
    }

    protected float _defaultIntensity;
    public float Intensity
    {
        get => _light.intensity;
        set => _light.intensity = value;
    }

    protected float _defaultInnerRadius;
    public float InnerRadius
    {
        get => _light.pointLightInnerRadius;
        set => _light.pointLightInnerRadius = value;
    }

    protected float _defaultOuterRadius;
    public float OuterRadius
    {
        get => _light.pointLightOuterRadius;
        set => _light.pointLightOuterRadius = value;
    }

    protected float _defaultFalloff;
    public float Falloff
    {
        get => _light.falloffIntensity;
        set => _light.falloffIntensity = value;
    }
    #endregion

    #region Flicker 속성
    [Header("테스트. 플레이 중일 때만 변경할 것.")]
    /// <summary>
    /// 빛의 밝기와 반경이 일렁이는 변화 속도 (목표가 움직이는 속도)
    /// </summary>
    [SerializeField] protected float _noiseSpeed;
    /// <summary>
    /// 목표 밝기와 반경을 따라가는 부드러움 및 반응 속도 (목표값에 도달하는 속도)
    /// </summary>
    [SerializeField] protected float _smoothSpeed;

    [SerializeField] protected float _radiusVariation;

    [SerializeField] protected float _minIntensity;
    [SerializeField] protected float _maxIntensity;
    #endregion

    void Awake()
    {
        Init();
    }

    public virtual void Init()
    {
        //Debug.Log("LightController Init");
        Owner ??= transform.parent;
        _light = GetComponent<Light2D>();
        transform.localPosition = Vector3.zero;
    }

    public void ResetToDefault()
    {
        Color = _defaultColor;
        Intensity = _defaultIntensity;
        InnerRadius = _defaultInnerRadius;
        OuterRadius = _defaultOuterRadius;
        Falloff = _defaultFalloff;
    }

    public void TurnOn(bool flicker = true)
    {
        if (_coFlicker != null) // 중복 방지
            return;

        gameObject.SetActive(true);
        if (flicker)
            _coFlicker = StartCoroutine(CoFlicker());
    }

    public void TurnOff()
    {
        if (_coFlicker != null)
        {
            StopCoroutine(_coFlicker);
            _coFlicker = null;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 빛 일렁이는 효과
    /// </summary>
    /// <returns></returns>
    IEnumerator CoFlicker()  // TODO: 진행도에 따라 일렁이는 효과 강화 (심리적 공포용)
    {
        float intensityNoiseOffset = Random.Range(0f, 1000f);
        float radiusNoiseOffset = Random.Range(0f, 1000f);

        while (true)
        {
            // 너무 일정해서 폐기
            //float wave = (Mathf.Sin(Time.time * _noiseSpeed) + 1f) * 0.5f;     // (0 ~ 1)
            //_light.intensity = Mathf.Lerp(_minIntensity, _maxIntensity, wave);
            //_light.pointLightOuterRadius = _defaultOuterRadius + Mathf.Lerp(-_radiusVariation, _radiusVariation, wave);

            float intensityNoise = Mathf.PerlinNoise(intensityNoiseOffset, Time.time * _noiseSpeed);
            float radiusNoise = Mathf.PerlinNoise(radiusNoiseOffset, Time.time * _noiseSpeed);

            float targetIntensity = Mathf.Lerp(_minIntensity, _maxIntensity, intensityNoise);
            float targetRadius = _defaultOuterRadius + Mathf.Lerp(-_radiusVariation, _radiusVariation, radiusNoise);
            
            _light.intensity = Mathf.Lerp(_light.intensity, targetIntensity, Time.deltaTime * _smoothSpeed);
            _light.pointLightOuterRadius = Mathf.Lerp(_light.pointLightOuterRadius, targetRadius, Time.deltaTime * _smoothSpeed);
            yield return null;
        }
    }
}
