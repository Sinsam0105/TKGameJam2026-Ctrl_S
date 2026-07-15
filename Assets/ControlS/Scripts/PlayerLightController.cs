using ControlS;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PlayerLightController : MonoBehaviour
{
    TopDownPlayer _player;
    Light2D _light;

    Color _color = new Color(224f / 255f, 171f / 255f, 98f / 255f, 255f / 255f);    // 옅은 주황색
    public Color Color
    { 
        get => _color; 
        set
        {
            _color = value;
            _light.color = _color;
        }
    }

    float _intensity = 1.5f;
    public float Intensity
    { 
        get => _intensity; 
        set
        {
            _intensity = value;
            _light.intensity = _intensity;
        }
    }

    float _innerRadius = 0.5f;
    public float InnerRadius
    {
        get => _innerRadius;
        set
        {
            _innerRadius = value;
            _light.pointLightInnerRadius = _innerRadius;
        }
    }

    float _outerRadius = 5f;
    public float OuterRadius
    { 
        get => _outerRadius; 
        set
        {
            _outerRadius = value;
            _light.pointLightOuterRadius = _outerRadius;
        }
    }

    float _falloff = 0.5f;
    public float Falloff
    {
        get => _falloff;
        set
        {
            _falloff = value;
            _light.falloffIntensity = _falloff;
        }
    }

    void Awake()
    {
        Init();
    }

    void Init()
    {
        Debug.Log("LightController Init");
        _player = GetComponentInParent<TopDownPlayer>();
        _light = GetComponent<Light2D>();

        // 기본값 적용
        _light.color = _color;
        _light.intensity = _intensity;
        _light.pointLightOuterRadius = _outerRadius;
        _light.pointLightInnerRadius = _innerRadius;
        _light.falloffIntensity = _falloff;

        // test
        GameObject target = GameObject.Find("Global Light 2D");
        target.SetActive(false);
    }

    public void TurnOn()
    {
        _light.enabled = true;
    }

    public void TurnOff()
    {
        _light.enabled = false;
    }

    // TODO: 혹시 빛의 범위 or 밝기가 서서히 변하는 효과가 필요한가? 나중에 기획에 따라 추가할 예정
    // TODO: 플레이어가 바라보는 방향으로만 빛을 쐬어야 할까?
}
