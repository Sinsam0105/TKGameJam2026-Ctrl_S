using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerLightController : LightController
{
    public override void Init()
    {
        base.Init();
        
        _defaultColor = new Color32(0xE0, 0xAB, 0x62, 0xFF);    // 옅은 주황색
        _defaultIntensity = 1.5f;
        _defaultInnerRadius = 0.5f;
        _defaultOuterRadius = 5f;
        _defaultFalloff = 0.5f;
        ResetToDefault();

        _noiseSpeed = 1f;
        _smoothSpeed = 3f;

        _radiusVariation = 0.7f;
        
        _minIntensity = 1;
        _maxIntensity = _defaultIntensity;

        // test
        {
            GameObject.Find("Global Light 2D").SetActive(false);
            TurnOn();
        }
    }
}
