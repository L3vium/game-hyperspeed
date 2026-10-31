// Fullscreen UI overlay : anime radial speed lines, intensity driven from code.
Shader "Hyperspeed/SpeedLines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0,1)) = 0
        _LineColor ("Line Color", Color) = (1,1,1,1)
        _Density ("Density", Float) = 120
        _Center ("Center", Vector) = (0.5,0.5,0,0)
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

            half4 Frag(Varyings input) : SV_Target
            {
                float2 lDelta = (input.uv - _Center.xy) * float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                float lRadius = length(lDelta);
                float lAngle = atan2(lDelta.y, lDelta.x) / 6.2831853 + 0.5;
                float lCell = floor(lAngle * _Density);
                float lFrame = floor(_Time.y * 24.0);
                float lRnd = Hash(lCell + lFrame * 13.37);
                float lWidth = frac(lAngle * _Density);
                float lThin = smoothstep(0.0, 0.15, lWidth) * smoothstep(0.55, 0.3, lWidth);
                float lStart = lerp(0.75, 0.32, _Intensity) + Hash(lCell * 3.1 + lFrame) * 0.25;
                float lAlpha = step(1.0 - _Intensity * 0.85, lRnd) * lThin * smoothstep(lStart, lStart + 0.3, lRadius);
                return half4(_LineColor.rgb, lAlpha * _LineColor.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
