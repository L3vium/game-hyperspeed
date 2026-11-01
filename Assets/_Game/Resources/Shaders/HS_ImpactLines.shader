// Manga impact overlay : thick black radial wedges converging on the center, with a white core.
Shader "Hyperspeed/ImpactLines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0,1)) = 1
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off ZTest [unity_GUIZTestMode]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half _Intensity;
            float _Seed;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            float Hash(float p) { return frac(sin(p * 127.1 + _Seed) * 43758.5453); }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 lDelta = (input.uv - 0.5) * float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                float lRadius = length(lDelta);
                float lAngle = atan2(lDelta.y, lDelta.x) / 6.2831853 + 0.5;
                float lCell = floor(lAngle * 90.0);
                float lLocal = frac(lAngle * 90.0);
                float lWidth = 0.25 + Hash(lCell) * 0.6;
                float lWedge = step(abs(lLocal - 0.5), lWidth * 0.5);
                float lStart = 0.12 + Hash(lCell + 3.7) * 0.25;
                float lInk = lWedge * smoothstep(lStart, lStart + 0.05, lRadius) * step(0.35, Hash(lCell + 9.1));
                half3 lColor = lerp(half3(1, 1, 1), half3(0, 0, 0), lInk);
                half lAlpha = (lInk * 0.95 + (1.0 - smoothstep(0.0, 0.18, lRadius)) * 0.6) * _Intensity;
                return half4(lColor, lAlpha);
            }
            ENDHLSL
        }
    }
}
