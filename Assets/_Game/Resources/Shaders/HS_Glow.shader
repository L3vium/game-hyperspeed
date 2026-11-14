// Unlit emissive shader for boost pads, rings, rails, particles and the light form.
// _Soft fades the quad UVs into a round blob (particles), _Fresnel brightens edges.
Shader "Hyperspeed/Glow"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _Soft ("Soft Circle", Range(0,1)) = 0
        _Fresnel ("Fresnel", Range(0,4)) = 0
        _Scroll ("Stripe Scroll", Float) = 0
        _StripeFreq ("Stripe Frequency", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "Glow"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Soft;
                half _Fresnel;
                float _Scroll;
                float _StripeFreq;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                VertexPositionInputs lPos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = lPos.positionCS;
                output.uv = input.uv;
                output.color = input.color;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(lPos.positionWS);
                output.fogFactor = ComputeFogFactorZ0ToFar(distance(lPos.positionWS, GetCameraPositionWS()));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 lColor = _Color * input.color;
                half lCircle = saturate(1.0 - length(input.uv - 0.5) * 2.0);
                lColor.a *= lerp(1.0, lCircle * lCircle, _Soft);
                if (_Fresnel > 0)
                {
                    half lFres = pow(1.0 - abs(dot(normalize(input.normalWS), normalize(input.viewWS))), 2.0);
                    lColor.rgb *= 1.0 + lFres * _Fresnel;
                }
                if (_StripeFreq > 0)
                {
                    half lStripe = step(0.5, frac(input.uv.x * _StripeFreq - _Time.y * _Scroll));
                    lColor.rgb *= lerp(0.45, 1.25, lStripe);
                }
                lColor.rgb = MixFog(lColor.rgb, input.fogFactor * 0.5 + 0.5);
                return lColor;
            }
            ENDHLSL
        }
    }
}
