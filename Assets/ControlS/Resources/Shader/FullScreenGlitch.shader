Shader "FullScreenGlitch"
{
    // 쉐이더가 적용된 머테리얼은 Inspector에서 속성 값 조정 가능
    Properties
    {
        _Intensity ("Intensity", Range(0, 1)) = 0   // 글리치 강도. 0이면 정상 화면이고, 1에 가까울수록 글리치가 강해진다
        _RGBSplit ("RGB Split", Range(0, 0.05)) = 0.008 // RGB 색상 분리 거리
        _HorizontalJump ("Horizontal Jump", Range(0, 0.2)) = 0.05   // 화면이 좌우로 밀리는 거리
        _BlockCount ("Block Count", Range(1, 200)) = 80 // 화면을 나누는 가로 구간 수
        _NoiseAmount ("Noise Amount", Range(0, 1)) = 0.15
        _Speed ("Speed", Range(0, 30)) = 15
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" // 불투명한 화면 처리
            "RenderPipeline" = "UniversalPipeline"  // URP용 셰이더
        }

        ZWrite Off  // 이미 그려진 화면 위에 덮어씌우므로 z값을 새로 기록할 필요가 없다
        Cull Off    // 폴리곤의 앞면이나 뒷면을 제거하지 않는다

        Pass
        {
            Name "FullScreenGlitch"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag   // 화면의 각 픽셀마다 따로 실행하여 각 픽셀 색상을 계산

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            float _RGBSplit;
            float _HorizontalJump;
            float _BlockCount;
            float _NoiseAmount;
            float _Speed;

            float Random(float2 value)
            {
                return frac(sin(dot(value, float2(12.9898, 78.233))) * 43758.5453); // 입력값 → 0 ~ 1
            }

            // 화면의 각 픽셀마다 따로 실행하여 각 픽셀 색상을 계산
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord; // 화면에서 현재 픽셀 위치

                // 시간 값을 계단식으로 만들어 프레임마다 불규칙하게 변하게 한다
                float glitchTime = floor(_Time.y * _Speed); // 경과 시간 * 속도

                // 화면을 가로줄 블록으로 나눈다
                float blockY = floor(uv.y * _BlockCount);

                // 각 가로 블록마다 난수를 생성한다
                float blockNoise = Random(float2(blockY, glitchTime));

                // 일부 가로줄만 좌우로 크게 밀어낸다 => 현재 화면 픽셀에 원래 자기 위치의 색을 그리지 않고, 조금 옆에 있는 원본 화면의 색을 가져와 그린다
                float2 distortedUV = uv;    // 기준 위치. 원본 UV를 글리치용으로 변형하기 위한 좌표
                float isActiveBlock = step(0.82, blockNoise);   // blockNoise가 0.82 이상인 가로줄만 활성화 (true, false)
                float horizontalOffset = (blockNoise - 0.5) * _HorizontalJump * _Intensity * isActiveBlock;
                distortedUV.x += horizontalOffset;

                // 글리치로 UV 위치를 움직였을 때 화면 밖으로 나가지 않도록 한다
                distortedUV = saturate(distortedUV);    // 0 ~ 1 사이로 제한

                // RGB 채널 분리 거리. 분리하지 않으면 세 색이 정확히 겹쳐서 원래 색으로 보인다
                float splitNoise = Random(float2(glitchTime, blockY + 31.7));
                float rgbOffset = (splitNoise * 2.0 - 1.0) * _RGBSplit * _Intensity;
                float2 redUV = saturate(distortedUV + float2(rgbOffset, 0));    // 기준보다 오른쪽
                float2 blueUV = saturate(distortedUV - float2(rgbOffset, 0));   // 기준보다 왼쪽

                // 렌더링된 원본 화면에서 특정 위치의 색상을 가져온다
                half red = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, redUV).r;  
                half green = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, distortedUV).g;
                half blue = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, blueUV).b;
                half4 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // 서로 다른 위치의 R, G, B를 합쳐서 색상 적용. 물체 가장자리에 빨강과 파랑이 벌어지는 효과
                half4 glitchColor = half4(red, green, blue, original.a);

                // 화면 전체에 미세한 노이즈를 추가한다
                float pixelNoise = Random(floor(uv * _ScreenParams.xy * 0.25) + glitchTime);    // UV 좌표 -> 실제 화면 픽셀 단위
                glitchColor.rgb += (pixelNoise - 0.5) * _NoiseAmount * _Intensity;

                // 가는 수평 스캔라인
                float scanLine = sin(uv.y * _ScreenParams.y * 1.5);
                glitchColor.rgb -= scanLine * 0.025 * _Intensity;

                // 원본 화면과 글리치 화면을 Intensity만큼 섞는다
                return lerp(original, glitchColor, _Intensity);
            }

            ENDHLSL
        }
    }
}