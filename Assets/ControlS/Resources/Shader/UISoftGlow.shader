Shader "UI/SoftGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Glow Color", Color) = (0.45, 0.75, 1, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1
        _Falloff ("Falloff", Range(0.1, 8)) = 2

        [Header(Flicker)]
        _Flicker ("Flicker Amount", Range(0, 1)) = 0.08
        _FlickerSpeed ("Flicker Speed", Range(0, 10)) = 3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One One

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex         : SV_POSITION;
                fixed4 color          : COLOR;
                float2 texcoord       : TEXCOORD0;
                float4 worldPosition  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _ClipRect;

            float _Intensity;
            float _Falloff;
            float _Flicker;
            float _FlickerSpeed;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 사각형 중심에서부터의 거리로 부드러운 원형 그라디언트를 만든다 (텍스처 불필요).
                float2 centered = IN.texcoord - 0.5;
                float dist = length(centered) * 2.0;
                float glow = pow(saturate(1.0 - dist), _Falloff);

                // 모니터 불빛이 은은하게 숨쉬듯 깜빡이는 느낌.
                float flicker = 1.0 + sin(_Time.y * _FlickerSpeed) * _Flicker;

                half3 rgb = _Color.rgb * glow * _Intensity * flicker;
                half alpha = glow * _Color.a * IN.color.a;

                half4 color = half4(rgb * alpha, alpha);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
