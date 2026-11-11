// Fullscreen UI overlay : multicolored anime speed streaks rushing out from (almost) the screen center.
// Two layers : thin white needles + thick colored wedges (cyan, magenta, violet, yellow). _Intensity from code.
Shader "Hyperspeed/SpeedLines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0,1)) = 0
        _Boost ("Boost", Range(0,1)) = 0
        _Burst ("Burst", Range(0,1)) = 0
        _LineColor ("Line Color", Color) = (1,1,1,1)
        _Density ("Density", Float) = 140
        _Center ("Center", Vector) = (0.5,0.52,0,0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off ZTest [unity_GUIZTestMode]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half _Intensity;
            half _Boost;
            half _Burst;
            half4 _LineColor;
            float _Density;
            float4 _Center;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            float Hash(float p) { return frac(sin(p * 127.1) * 43758.5453); }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half3 Palette(float h)
            {
                if (h < 0.3) return half3(0.2, 0.5, 1.0);
                if (h < 0.55) return half3(0.55, 0.25, 1.0);
                if (h < 0.8) return half3(1.0, 0.2, 0.72);
                return half3(0.25, 0.95, 1.0);
            }

            // One radial layer : streaks that travel outward along their ray.
            half4 Layer(float lAngle, float lRadius, float pDensity, float pSeed, float pSpeed, float pWidth, float pReach, bool pColored)
            {
                float lCell = floor(lAngle * pDensity);
                float lLocal = frac(lAngle * pDensity);
                float lRnd = Hash(lCell + pSeed);
                float lAlive = step(1.0 - _Intensity * (pColored ? 0.42 : 0.55), lRnd);
                float lWidth = pWidth * (0.4 + Hash(lCell * 1.7 + pSeed) * 0.8);
                float lShape = smoothstep(lWidth, 0.0, abs(lLocal - 0.5));
                // Each streak is a dash travelling outward ; inner start gets closer to the center with speed. A boost
                // throws them all the way in for an instant, then they settle back where the speed puts them.
                float lStart = lerp(0.55, pReach, sqrt(_Intensity)) + Hash(lCell * 3.1 + pSeed) * 0.08;
                lStart = max(0.2, lStart - _Burst * 0.22);
                float lPhase = frac(lRadius * (1.2 + Hash(lCell + 5.0)) - _Time.y * pSpeed * (0.7 + lRnd) + Hash(lCell * 7.3));
                float lDash = smoothstep(0.0, 0.08, lPhase) * smoothstep(0.75, 0.35, lPhase);
                float lAlpha = lAlive * lShape * lDash * smoothstep(lStart, lStart + 0.15, lRadius);
                half3 lColor = pColored ? Palette(Hash(lCell * 9.7 + pSeed)) : _LineColor.rgb;
                return half4(lColor, lAlpha);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 lDelta = (input.uv - _Center.xy) * float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                float lRadius = length(lDelta);
                float lAngle = atan2(lDelta.y, lDelta.x) / 6.2831853 + 0.5;

                half4 lThin = Layer(lAngle, lRadius, _Density, 0.0, 3.4, 0.2, 0.16, false);
                half4 lThick = Layer(lAngle, lRadius, _Density * 0.35, 31.0, 2.4, 0.44, 0.2, true);
                lThick.a *= saturate(_Intensity * 1.4 + _Boost * 0.6);
                // Near the top speed, huge colored wedges take over the whole frame.
                half4 lWide = Layer(lAngle, lRadius, _Density * 0.12, 57.0, 1.8, 0.48, 0.26, true);
                lWide.a *= smoothstep(0.55, 1.0, _Intensity) * 0.5;
                lThick.rgb = lerp(lThick.rgb, lWide.rgb, saturate(lWide.a * (1.0 - lThick.a * 0.5)));
                lThick.a = saturate(lThick.a + lWide.a);

                half4 lColor = lThick;
                lColor.rgb = lerp(lThick.rgb, lThin.rgb, saturate(lThin.a));
                // A clear window in the middle : you always see where you are going. The burst bites into it briefly.
                float lClear = lerp(0.5, 0.3, _Burst);
                lColor.a = saturate(lThick.a * 0.6 + lThin.a * 0.55) * smoothstep(lClear * 0.55, lClear, lRadius);
                return half4(lColor.rgb, lColor.a * _LineColor.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
