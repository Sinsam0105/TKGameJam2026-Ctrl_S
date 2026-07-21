using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerLightController : LightController
{
    private void Awake()
    {
        Init();
    }

    public override void Init()
    {
        base.Init();
        
        _defaultColor = new Color32(0xE0, 0xAB, 0x62, 0xFF);    // 옅은 주황색
        _defaultIntensity = 1.5f;
        _defaultInnerRadius = 0.35f;
        _defaultOuterRadius = 3.2f;
        _defaultFalloff = 0.5f;
        ResetToDefault();

        _noiseSpeed = 1f;
        _smoothSpeed = 3f;

        _radiusVariation = 0.35f;
        
        _minIntensity = 1;
        _maxIntensity = _defaultIntensity;

        TurnOn();
    }
}
