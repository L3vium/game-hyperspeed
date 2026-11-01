// Boost energy cone around the runner : scrolling streaks, fresnel edges, fades toward the back.
Shader "Hyperspeed/EnergyCone"
{
    Properties
    {
        [HDR] _ColorA ("Color A", Color) = (0.4, 2.2, 3, 1)
        [HDR] _ColorB ("Color B", Color) = (3, 0.6, 2.4, 1)
        _Intensity ("Intensity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off Cull Off

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA;
                half4 _ColorB;
                half _Intensity;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 viewWS : TEXCOORD2; };

            float Hash(float p) { return frac(sin(p * 91.3458) * 47453.5453); }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs lPos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = lPos.positionCS;
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(lPos.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float lColumn = floor(input.uv.x * 48.0);
                float lSpeed = 2.5 + Hash(lColumn) * 4.0;
                float lStreak = frac(input.uv.y * (1.0 + Hash(lColumn + 7.0) * 2.0) - _Time.y * lSpeed + Hash(lColumn * 3.1));
                float lLine = smoothstep(0.0, 0.1, lStreak) * smoothstep(0.55, 0.15, lStreak);
                float lFres = pow(1.0 - abs(dot(normalize(input.normalWS), normalize(input.viewWS))), 1.5);
                float lFade = (1.0 - input.uv.y) * smoothstep(0.0, 0.08, input.uv.y);
                half3 lColor = lerp(_ColorA.rgb, _ColorB.rgb, Hash(lColumn + 13.0));
                half lAlpha = saturate((lLine * 0.8 + lFres * 0.35) * lFade * _Intensity);
                return half4(lColor, lAlpha);
            }
            ENDHLSL
        }
    }
}
