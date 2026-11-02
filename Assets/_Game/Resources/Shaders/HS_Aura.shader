// Speed aura around the runner : the skinned body pushed out along its normals and dragged backward
// (opposite to the motion), fresnel-bright edges, scrolling noise flames. Additive, driven per frame.
Shader "Hyperspeed/Aura"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.4, 1.4, 2.2, 1)
        [HDR] _TipColor ("Tip Color", Color) = (2.2, 0.6, 1.8, 1)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _Push ("Normal Push", Float) = 0.04
        _Drag ("Backward Drag", Float) = 1.5
        _MotionDir ("Motion Direction (world)", Vector) = (0, 0, 1, 0)
        _NoiseScale ("Noise Scale", Float) = 6
        _NoiseSpeed ("Noise Speed", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Aura"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _TipColor;
                half _Intensity;
                float _Push;
                float _Drag;
                float4 _MotionDir;
                float _NoiseScale;
                float _NoiseSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
                float drag : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), f.x), lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x), lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 lPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 lNormal = TransformObjectToWorldNormal(input.normalOS);
                float3 lMotion = normalize(_MotionDir.xyz + float3(0, 0.0001, 0));
                // Faces looking backward get dragged the most : the aura streams out behind the runner.
                float lBack = saturate(-dot(lNormal, lMotion) * 0.5 + 0.5);
                float lFlicker = Noise(lPos * _NoiseScale + lMotion * _Time.y * _NoiseSpeed * 4.0);
                float lDrag = _Drag * _Intensity * lBack * lBack * (0.4 + lFlicker);
                lPos += lNormal * _Push * (0.6 + lFlicker * 0.8) * _Intensity;
                lPos -= lMotion * lDrag;
                output.positionCS = TransformWorldToHClip(lPos);
                output.normalWS = lNormal;
                output.viewWS = GetWorldSpaceViewDir(lPos);
                output.drag = saturate(lDrag / max(0.001, _Drag));
                output.positionWS = lPos;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 lView = normalize(input.viewWS);
                float lFresnel = pow(1.0 - saturate(abs(dot(normalize(input.normalWS), lView))), 2.0);
                float lNoise = Noise(input.positionWS * _NoiseScale * 1.7 + float3(0, -_Time.y * _NoiseSpeed, 0));
                half3 lColor = lerp(_Color.rgb, _TipColor.rgb, saturate(input.drag * 1.6));
                float lAlpha = (lFresnel * 0.9 + input.drag * 0.6) * (0.55 + lNoise * 0.7) * _Intensity * (1.0 - input.drag * 0.65);
                return half4(lColor * lAlpha, 1);
            }
            ENDHLSL
        }
    }
}
