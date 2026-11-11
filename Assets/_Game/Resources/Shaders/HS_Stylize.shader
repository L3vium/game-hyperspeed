// Fullscreen stylize pass (URP Full Screen Pass Renderer Feature, after post processing).
// Chromatic aberration, radial speed blur, ordered dithering, impact frames and screen flash.
// Driven by global floats set from PostFX.cs.
Shader "Hyperspeed/Stylize"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "Stylize"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _HS_Aberration;
            float _HS_RadialBlur;
            float _HS_Dither;
            float _HS_Impact;
            float _HS_Flash;
            float4 _HS_FlashColor;
            float4 _HS_ImpactColor;
            float _HS_Rewind;
            float _HS_Desaturate;
            float _HS_Fisheye;
            float _HS_Fade;
            float _HS_Letterbox;
            float _HS_Intensity;
            float _HS_WrongWay;

            static const float BAYER[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            half3 SampleScene(float2 pUV)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pUV).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 lUV = input.texcoord;
                if (_HS_Fisheye > 0.001)
                {
                    // Light speed : the frame bulges like a fisheye, the world wraps around the runner.
                    float2 lD = lUV - 0.5;
                    lD.x *= _ScreenParams.x / _ScreenParams.y;
                    float lR2 = dot(lD, lD);
                    float lK = _HS_Fisheye * 0.9;
                    lD *= (1.0 + lK * lR2) / (1.0 + lK * 0.62);
                    lD.x /= _ScreenParams.x / _ScreenParams.y;
                    lUV = 0.5 + lD;
                }
                float2 lDelta = lUV - 0.5;
                float lEdge = dot(lDelta, lDelta) * 4.0;

                float2 lOffset = lDelta * _HS_Aberration * 0.03 * (0.3 + lEdge);
                half3 lColor;
                lColor.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, lUV - lOffset).r;
                lColor.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, lUV).g;
                lColor.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, lUV + lOffset).b;

                if (_HS_RadialBlur > 0.001)
                {
                    // The world tears into long radial strands : each direction stretches by its own amount, which keeps
                    // hard streaks of colour instead of a soft blur. Only the middle, around the runner, stays crisp.
                    half3 lAccum = lColor;
                    float lAngle = atan2(lDelta.y, lDelta.x) * 42.0;
                    float lCell = floor(lAngle);
                    float lStrand = lerp(frac(sin(lCell * 12.9898) * 43758.5453), frac(sin((lCell + 1.0) * 12.9898) * 43758.5453), smoothstep(0.0, 1.0, frac(lAngle)));
                    float lMask = smoothstep(0.07, 0.75, lEdge);
                    float lStrength = _HS_RadialBlur * 0.055 * lMask * (0.25 + lStrand * lStrand * 2.1);
                    [unroll] for (int i = 1; i <= 14; i++)
                    {
                        lAccum += SampleScene(lUV - lDelta * lStrength * (i / 14.0) * 8.0);
                    }
                    lColor = lAccum / 15.0;
                }

                // Ordered dithering + light posterization for a printed / anime look.
                uint2 lPixel = (uint2)(lUV * _ScreenParams.xy);
                float lBayer = BAYER[(lPixel.x % 4) + (lPixel.y % 4) * 4] / 16.0 - 0.5;
                float lLevels = 24.0;
                half3 lDithered = floor(lColor * lLevels + lBayer + 0.5) / lLevels;
                lColor = lerp(lColor, lDithered, _HS_Dither);

                if (_HS_Impact > 0.001)
                {
                    half lLuma = dot(lColor, half3(0.299, 0.587, 0.114));
                    // Inked from the scene itself : its lights stay white, its middle takes the colour of the place.
                    half3 lInk = lLuma > 0.55 ? half3(1, 1, 1) : (lLuma > 0.22 ? _HS_ImpactColor.rgb : half3(0.02, 0.01, 0.04));
                    // First frame in negative, then plain ink, then the scene comes back through it.
                    if (_HS_Impact > 0.5) lInk = 1.0 - lInk;
                    lColor = lerp(lColor, lInk, _HS_Impact > 0.5 ? 1.0 : 0.82);
                }

                if (_HS_Rewind > 0.001)
                {
                    half lGrey = dot(lColor, half3(0.299, 0.587, 0.114));
                    lColor = lerp(lColor, half3(lGrey * 0.85, lGrey * 0.95, lGrey * 1.15), saturate(_HS_Desaturate));
                    float lScan = 0.85 + 0.15 * step(0.5, frac(lUV.y * _ScreenParams.y * 0.25 + _Time.y * 30.0));
                    lColor *= lerp(1.0, lScan, _HS_Rewind);
                }

                if (_HS_Intensity > 0.001)
                {
                    // Neck and neck / victory : colours pushed hard, contrast up.
                    half lGray = dot(lColor, half3(0.299, 0.587, 0.114));
                    lColor = lerp(lColor, lerp(lGray.xxx, lColor, 1.0 + _HS_Intensity * 0.9), 1.0);
                    lColor = saturate((lColor - 0.5) * (1.0 + _HS_Intensity * 0.35) + 0.5);
                }

                if (_HS_WrongWay > 0.001)
                {
                    // Wrong way : a deep red closing in from the edges. Round whatever the aspect, a very wide and soft
                    // falloff (smootherstep over most of the radius), pulsing slowly, and a hair of noise so the
                    // gradient never shows bands.
                    float lAspect = _ScreenParams.x / _ScreenParams.y;
                    float2 lV = (input.texcoord - 0.5) * float2(lAspect, 1.0);
                    float lR = length(lV) / (0.5 * length(float2(lAspect, 1.0)));
                    float lT = saturate((lR - 0.18) / 0.9);
                    float lFall = lT * lT * lT * (lT * (lT * 6.0 - 15.0) + 10.0);
                    float lPulse = 0.78 + 0.22 * sin(_Time.y * 6.5);
                    float lAmount = saturate(_HS_WrongWay) * lFall * lPulse;
                    half3 lRed = half3(0.9, 0.03, 0.07);
                    lColor = lerp(lColor, lRed * (0.55 + 0.45 * lColor), lAmount * 0.92);
                    lColor += (frac(sin(dot(input.texcoord * _ScreenParams.xy, float2(12.9898, 78.233))) * 43758.5453) - 0.5) / 255.0 * saturate(_HS_WrongWay);
                }

                lColor = lerp(lColor, _HS_FlashColor.rgb, saturate(_HS_Flash));
                lColor *= 1.0 - saturate(_HS_Fade);
                // Cinema bars, top and bottom.
                float lBar = _HS_Letterbox * 0.5;
                if (input.texcoord.y < lBar || input.texcoord.y > 1.0 - lBar) lColor = half3(0, 0, 0);
                return half4(lColor, 1);
            }
            ENDHLSL
        }
    }
}
