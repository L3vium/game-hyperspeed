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

            static const float BAYER[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            half3 SampleScene(float2 pUV)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pUV).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 lUV = input.texcoord;
                float2 lDelta = lUV - 0.5;
                float lEdge = dot(lDelta, lDelta) * 4.0;

                float2 lOffset = lDelta * _HS_Aberration * 0.03 * (0.3 + lEdge);
                half3 lColor;
                lColor.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, lUV - lOffset).r;
                lColor.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, lUV).g;
                lColor.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, lUV + lOffset).b;

                if (_HS_RadialBlur > 0.001)
                {
                    half3 lAccum = lColor;
                    float lStrength = _HS_RadialBlur * 0.06 * saturate(lEdge * 1.5);
                    [unroll] for (int i = 1; i <= 6; i++)
                    {
                        lAccum += SampleScene(lUV - lDelta * lStrength * i);
                    }
                    lColor = lAccum / 7.0;
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
                    half3 lInk = lLuma > 0.45 ? half3(1, 1, 1) : (lLuma > 0.2 ? _HS_ImpactColor.rgb : half3(0, 0, 0));
                    // First impact frames are inverted (black sky, white ink), then plain ink.
                    if (_HS_Impact > 0.5) lInk = 1.0 - lInk;
                    lColor = lInk;
                }

                lColor = lerp(lColor, _HS_FlashColor.rgb, saturate(_HS_Flash));
                return half4(lColor, 1);
            }
            ENDHLSL
        }
    }
}
