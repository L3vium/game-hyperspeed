// Scene wipe : streaks rush in from every side of the screen toward the center until they fill it, like hitting light
// speed, then the center opens first as the next scene arrives.
Shader "Hyperspeed/Wipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0,1)) = 0
        _Inner ("Inner Color", Color) = (0.05,0.03,0.08,1)
        _Streak ("Streak Color", Color) = (1,1,1,1)
        _Opening ("Opening", Float) = 0
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

            float _Progress;
            float _Opening;
            float4 _Inner;
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

            float Hash(float pValue)
            {
                return frac(sin(pValue * 127.1) * 43758.5453);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 lCentered = (input.uv - 0.5) * float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                float lRadius = length(lCentered) / 0.92;
                float lAngle = atan2(lCentered.y, lCentered.x) / 6.28318 + 0.5;
                float lCell = floor(lAngle * 180.0);
                float lJag = Hash(lCell) * 0.55;
                // Closing : a spike reaches further in as progress grows. Opening : the hole grows from the center.
                float lReach = _Opening > 0.5
                    ? _Progress * 1.6 - lJag * 0.6
                    : (1.0 - _Progress) * 1.6 - lJag * 0.6;
                float lCover = _Opening > 0.5 ? step(lReach, lRadius) : step(lReach, lRadius);
                float lEdge = saturate(1.0 - abs(lRadius - lReach) * 7.0);
                float3 lColor = lerp(_Inner.rgb, _Streak.rgb, lEdge);
                return float4(lColor, lCover);
            }
            ENDHLSL
        }
    }
}
