// Flat colours : one solid colour per facet, one hard shadow step, fog into the sky.
// Ground speed lines drawn along the route (uv.y = distance along it) when the terrain flag is on.
Shader "Hyperspeed/Flat"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ShadeColor ("Shade Color", Color) = (0.6,0.55,0.8,1)
        _ShadeStep ("Shade Step", Range(0,1)) = 0.42
        _Flash ("Flash", Range(0,1)) = 0
        _RoadStreaks ("Ground Speed Lines", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ShadeColor;
            half _ShadeStep;
            half _Flash;
            half _RoadStreaks;
        CBUFFER_END

        float _HS_RoadScroll;
        float _HS_RoadSpeed;
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                VertexPositionInputs lPos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = lPos.positionCS;
                output.positionWS = lPos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(lPos.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 lNormal = normalize(input.normalWS);
                Light lLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half lNdl = dot(lNormal, lLight.direction) * 0.5 + 0.5;
                half lLit = lNdl * lerp(0.4, 1.0, lLight.shadowAttenuation);
                half lBand = step(_ShadeStep, lLit);
                half3 lAlbedo = _BaseColor.rgb * input.color.rgb;
                half3 lColor = lerp(lAlbedo * _ShadeColor.rgb, lAlbedo, lBand);

                // Thin lines rushing along the route, denser with speed. Fade with distance : a texture-free ground.
                if (_RoadStreaks > 0 && _HS_RoadSpeed > 0.01)
                {
                    float lColumn = floor(input.positionWS.x * 0.06 + input.positionWS.z * 0.06);
                    float lRnd = frac(sin(lColumn * 91.17) * 4375.85);
                    float lThin = step(frac(input.positionWS.x * 0.06 + input.positionWS.z * 0.06), 0.1);
                    float lDash = frac(input.uv.y * (0.002 + lRnd * 0.003) + _HS_RoadScroll * (0.002 + lRnd * 0.002) + lRnd * 7.0);
                    float lStreak = step(0.72 - _HS_RoadSpeed * 0.3, lDash) * lThin * step(0.45, lRnd);
                    float lNear = saturate(1.0 - distance(input.positionWS, GetCameraPositionWS()) * 0.0025);
                    lColor = lerp(lColor, half3(1, 1, 1), lStreak * _HS_RoadSpeed * 0.7 * lNear);
                }

                lColor = lerp(lColor, half3(1, 1, 1) * 1.5, _Flash);
                lColor = MixFog(lColor, input.fogFactor);
                return half4(lColor, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 lWorld = TransformObjectToWorld(input.positionOS.xyz);
                float3 lNormal = TransformObjectToWorldNormal(input.normalOS);
                float4 lClip = TransformWorldToHClip(ApplyShadowBias(lWorld, lNormal, _LightDirection));
                #if UNITY_REVERSED_Z
                    lClip.z = min(lClip.z, lClip.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    lClip.z = max(lClip.z, lClip.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return lClip;
            }

            half4 Frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half4 Frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}
