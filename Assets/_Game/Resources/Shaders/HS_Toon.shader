// Stylized toon shader : stepped lighting, halftone shadows, rim, toon specular,
// vertex color tint, jelly wobble, hit flash and inverted-hull outline.
Shader "Hyperspeed/Toon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _ShadeColor ("Shade Color", Color) = (0.55,0.5,0.75,1)
        _ShadeStep ("Shade Step", Range(0,1)) = 0.45
        _ShadeSoft ("Shade Softness", Range(0.001,0.5)) = 0.04
        _HalftoneSize ("Halftone Size (px)", Float) = 7
        _HalftoneAmount ("Halftone Amount", Range(0,1)) = 0.6
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 3
        _RimAmount ("Rim Amount", Range(0,2)) = 0.4
        _SpecColor ("Spec Color", Color) = (1,1,1,1)
        _SpecSize ("Spec Size", Range(0,1)) = 0
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,0)
        _OutlineColor ("Outline Color", Color) = (0.08,0.04,0.12,1)
        _OutlineWidth ("Outline Width", Range(0,0.2)) = 0.02
        _Wobble ("Wobble", Range(0,0.3)) = 0
        _WobbleSpeed ("Wobble Speed", Float) = 8
        _Flash ("Flash", Range(0,1)) = 0
        _UseVertexColor ("Use Vertex Color", Range(0,1)) = 1
        _RoadStreaks ("Road Speed Streaks", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadeColor;
            half _ShadeStep;
            half _ShadeSoft;
            float _HalftoneSize;
            half _HalftoneAmount;
            half4 _RimColor;
            half _RimPower;
            half _RimAmount;
            half4 _SpecColor;
            half _SpecSize;
            half4 _EmissionColor;
            half4 _OutlineColor;
            float _OutlineWidth;
            float _Wobble;
            float _WobbleSpeed;
            half _Flash;
            half _UseVertexColor;
            half _RoadStreaks;
        CBUFFER_END

        float _HS_RoadScroll;
        float _HS_RoadSpeed;

        float3 ApplyWobble(float3 positionOS, float3 normalOS)
        {
            float lPhase = _Time.y * _WobbleSpeed + positionOS.y * 7.0 + positionOS.x * 5.0 + positionOS.z * 3.0;
            return positionOS + normalOS * (sin(lPhase) * 0.6 + sin(lPhase * 1.7 + 1.3) * 0.4) * _Wobble;
        }
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
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
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
                float4 screenPos : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                float2 uv : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 lPos = ApplyWobble(input.positionOS.xyz, input.normalOS);
                VertexPositionInputs lPosInputs = GetVertexPositionInputs(lPos);
                output.positionCS = lPosInputs.positionCS;
                output.positionWS = lPosInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = lerp(half4(1,1,1,1), input.color, _UseVertexColor);
                output.screenPos = ComputeScreenPos(lPosInputs.positionCS);
                output.fogFactor = ComputeFogFactor(lPosInputs.positionCS.z);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 lNormal = normalize(input.normalWS);
                float3 lView = normalize(GetWorldSpaceViewDir(input.positionWS));
                float4 lShadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light lLight = GetMainLight(lShadowCoord);

                half lNdl = dot(lNormal, lLight.direction) * 0.5 + 0.5;
                half lLit = lNdl * lerp(0.35, 1.0, lLight.shadowAttenuation);
                half lBand = smoothstep(_ShadeStep - _ShadeSoft, _ShadeStep + _ShadeSoft, lLit);

                // Screen space halftone dots in the light/shade transition.
                float2 lScreen = input.screenPos.xy / max(input.screenPos.w, 0.0001) * _ScreenParams.xy / _HalftoneSize;
                float2 lRot = float2(lScreen.x - lScreen.y, lScreen.x + lScreen.y) * 0.7071;
                float lDot = length(frac(lRot) - 0.5);
                half lTone = saturate((lLit - (_ShadeStep - 0.25)) / 0.25);
                half lHalftone = step(lDot, lTone * 0.55) * _HalftoneAmount * (1.0 - lBand);
                lBand = saturate(lBand + lHalftone);

                half3 lAlbedo = _BaseColor.rgb * input.color.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half3 lShade = lAlbedo * _ShadeColor.rgb;
                half3 lColor = lerp(lShade, lAlbedo, lBand) * lerp(half3(1,1,1), lLight.color, 0.35);
                lColor += SampleSH(lNormal) * lAlbedo * 0.25;

                half lRim = pow(saturate(1.0 - dot(lNormal, lView)), _RimPower) * _RimAmount;
                lColor += _RimColor.rgb * lRim * lerp(0.4, 1.0, lBand);

                float3 lHalf = normalize(lLight.direction + lView);
                half lSpec = step(1.0 - _SpecSize * 0.08, dot(lNormal, lHalf)) * step(0.001, _SpecSize);
                lColor += _SpecColor.rgb * lSpec * lLight.shadowAttenuation;

                // Anime ground speed lines : dashes along the track that run faster than the player.
                if (_RoadStreaks > 0 && input.uv.x >= 0.0 && _HS_RoadSpeed > 0.01)
                {
                    float lColumn = floor(input.uv.x * 26.0);
                    float lRnd = frac(sin(lColumn * 91.17) * 4375.85);
                    float lLane = frac(input.uv.x * 26.0);
                    float lThin = smoothstep(0.5, 0.2, abs(lLane - 0.5)) ;
                    float lDash = frac(input.uv.y * (0.03 + lRnd * 0.03) + _HS_RoadScroll * (0.02 + lRnd * 0.02) + lRnd * 7.0);
                    float lStreak = step(0.62 - _HS_RoadSpeed * 0.25, lDash) * lThin * step(0.35, lRnd);
                    lColor = lerp(lColor, half3(1, 1, 1), lStreak * _HS_RoadSpeed * 0.55);
                }

                lColor += _EmissionColor.rgb;
                lColor = lerp(lColor, half3(1,1,1) * 1.5, _Flash);
                lColor = MixFog(lColor, input.fogFactor);
                return half4(lColor, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fogFactor : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 lPos = ApplyWobble(input.positionOS.xyz, input.normalOS);
                float3 lWorld = TransformObjectToWorld(lPos);
                float3 lNormal = TransformObjectToWorldNormal(input.normalOS);
                float lDist = distance(lWorld, GetCameraPositionWS());
                lWorld += lNormal * _OutlineWidth * clamp(lDist * 0.08, 0.6, 4.0);
                output.positionCS = TransformWorldToHClip(lWorld);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                if (_OutlineWidth <= 0.0001) output.positionCS = float4(0, 0, -10, 1);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 lColor = lerp(_OutlineColor.rgb, half3(1,1,1), _Flash);
                return half4(MixFog(lColor, input.fogFactor), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
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
                float3 lPos = ApplyWobble(input.positionOS.xyz, input.normalOS);
                float3 lWorld = TransformObjectToWorld(lPos);
                float3 lNormal = TransformObjectToWorldNormal(input.normalOS);
                float4 lClip = TransformWorldToHClip(ApplyShadowBias(lWorld, lNormal, _LightDirection));
                #if UNITY_REVERSED_Z
                    lClip.z = min(lClip.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    lClip.z = max(lClip.z, UNITY_NEAR_CLIP_VALUE);
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
            ZWrite On ColorMask R Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(ApplyWobble(input.positionOS.xyz, input.normalOS));
            }

            half4 Frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
