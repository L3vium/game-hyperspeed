// Scene transition (2026-11-11) : the whole picture is sucked into the centre of the screen until it is black (the
// streaks die with it, nothing lingers), the screen stays black while loading, then the next scene bursts open from
// the centre with an inverted ink impact frame and streaks flying outward.
// _Mode 0 : close (samples the captured picture), 1 : black, 2 : open (a hole revealing the live scene), 3 : ink flash.
Shader "Hyperspeed/Wipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Captured picture", 2D) = "black" {}
        _Progress ("Progress", Range(0,1)) = 0
        _Mode ("Mode", Float) = 0
        _Ink ("Ink", Float) = 0
        _Streak ("Streak Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _Progress;
            float _Mode;
            float _Ink;
            float4 _Streak;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Hash(float pValue) { return frac(sin(pValue * 127.1) * 43758.5453); }

            float2 Rotate(float2 pV, float pA)
            {
                float lS = sin(pA), lC = cos(pA);
                return float2(pV.x * lC - pV.y * lS, pV.x * lS + pV.y * lC);
            }

            // Anime ink : lights white, middle tones in the streak colour, darks black ; inverted on the first frames.
            float3 Ink(float3 pColor, float pInvert)
            {
                float lLuma = dot(pColor, float3(0.299, 0.587, 0.114));
                float3 lInk = lLuma > 0.55 ? float3(1, 1, 1) : (lLuma > 0.22 ? _Streak.rgb : float3(0.02, 0.01, 0.04));
                return pInvert > 0.5 ? 1.0 - lInk : lInk;
            }

            float Streaks(float2 pCentered, float pDensity)
            {
                float lAngle = atan2(pCentered.y, pCentered.x) / 6.28318 + 0.5;
                float lCell = floor(lAngle * pDensity);
                float lWidth = frac(lAngle * pDensity);
                float lOn = step(0.62, Hash(lCell));
                return lOn * smoothstep(0.0, 0.25, lWidth) * smoothstep(1.0, 0.75, lWidth);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float lAspect = _ScreenParams.x / _ScreenParams.y;
                float2 lCentered = (input.uv - 0.5) * float2(lAspect, 1.0);
                float lRadius = length(lCentered);
                float lMaxRadius = 0.5 * length(float2(lAspect, 1.0));

                if (_Mode < 0.5)
                {
                    // Close : the picture shrinks into the centre with a twist, darkening ; streaks converge and die.
                    float lP = _Progress;
                    float lScale = max(0.0005, 1.0 - lP);
                    float2 lQ = Rotate(lCentered, -lP * lP * 2.2) / lScale;
                    float2 lUV = float2(lQ.x / lAspect, lQ.y) + 0.5;
                    float lInside = step(0.0, lUV.x) * step(lUV.x, 1.0) * step(0.0, lUV.y) * step(lUV.y, 1.0);
                    float3 lPicture = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(lUV)).rgb * lInside;
                    if (_Ink > 0.5) lPicture = Ink(lPicture, 1.0) * lInside;
                    // Streaks rushing in toward the shrinking picture.
                    float lRing = smoothstep(lScale * lMaxRadius * 0.9, lScale * lMaxRadius * 0.9 + 0.35, lRadius);
                    float lStreak = Streaks(lCentered, 90.0) * lRing * (1.0 - lP) * lP * 3.0;
                    float3 lColor = lerp(lPicture, _Streak.rgb, saturate(lStreak) * 0.8);
                    // Everything goes to black at the end.
                    lColor *= 1.0 - smoothstep(0.7, 1.0, lP);
                    return float4(lColor, 1.0);
                }
                if (_Mode < 1.5) return float4(0, 0, 0, 1);
                if (_Mode < 2.5)
                {
                    // Open : a jagged hole bursts from the centre ; a ring of streaks flies out at its edge.
                    float lP = _Progress;
                    float lAngle = atan2(lCentered.y, lCentered.x) / 6.28318 + 0.5;
                    float lJag = Hash(floor(lAngle * 140.0)) * 0.12 * (1.0 - lP);
                    float lEdge = lP * lMaxRadius * 1.25 + lJag;
                    float lCover = smoothstep(lEdge - 0.015, lEdge + 0.015, lRadius);
                    float lRingBand = exp(-pow((lRadius - lEdge) * 9.0, 2.0)) * (1.0 - lP);
                    float lStreak = Streaks(lCentered, 120.0) * smoothstep(lEdge, lEdge + 0.25, lRadius) * (1.0 - lP) * 0.7;
                    float3 lColor = lerp(float3(0, 0, 0), _Streak.rgb, saturate(lRingBand + lStreak));
                    return float4(lColor, saturate(lCover + lRingBand));
                }
                // Ink flash : the first frames of the new scene, inverted and inked, with a white burst from the centre.
                float lBurst = Streaks(lCentered, 60.0) * smoothstep(0.05, 0.5, lRadius);
                float3 lFlash = lerp(float3(0.02, 0.01, 0.04), float3(1, 1, 1), lBurst);
                return float4(lFlash, 1.0);
            }
            ENDHLSL
        }
    }
}
