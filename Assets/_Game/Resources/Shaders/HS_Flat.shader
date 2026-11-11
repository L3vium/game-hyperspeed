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

        float HashF(float2 pP) { return frac(sin(dot(pP, float2(127.1, 311.7))) * 43758.5453); }

        // Smooth value noise : soft blobs, no blocks.
        float SmoothNoise(float2 pP)
        {
            float2 lI = floor(pP);
            float2 lF = frac(pP);
            float2 lU = lF * lF * (3.0 - 2.0 * lF);
            float lA = HashF(lI);
            float lB = HashF(lI + float2(1, 0));
            float lC = HashF(lI + float2(0, 1));
            float lD = HashF(lI + float2(1, 1));
            return lerp(lerp(lA, lB, lU.x), lerp(lC, lD, lU.x), lU.y);
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

                // Organic ground on the floor of the track (uv = lateral, distance in metres) : big soft-edged patches of
                // two tones, and mown chevrons pointing the way, like a well kept lawn. Flat colours, no grain, no tiles.
                if (_RoadStreaks > 0 && (abs(input.uv.x + 1.0) + abs(input.uv.y)) > 0.001)
                {
                    float lFade = saturate(1.0 - (distance(input.positionWS, GetCameraPositionWS()) - 600.0) / 2400.0);
                    float2 lP = input.uv / float2(70.0, 110.0);
                    float lN = SmoothNoise(lP) * 0.65 + SmoothNoise(lP * 2.3 + 17.0) * 0.35;
                    float lEdge = max(fwidth(lN) * 1.5, 0.004);
                    float lPatch = smoothstep(0.52 - lEdge, 0.52 + lEdge, lN);
                    lColor *= lerp(1.0, lerp(0.93, 1.06, lPatch), 0.4 + 0.6 * lFade);
                    // Chevrons : lanes of V marks, the point toward where you run.
                    float lLane = input.uv.x / 46.0;
                    float lLaneId = floor(lLane);
                    float lInLane = (frac(lLane) - 0.5) * 46.0;
                    float lOn = step(0.5, frac(sin(lLaneId * 37.31) * 917.13));
                    float lV = (input.uv.y - abs(lInLane) * 0.75) / 16.0;
                    float lVw = max(fwidth(lV) * 1.2, 0.01);
                    float lMark = smoothstep(0.62 - lVw, 0.62, frac(lV)) * (1.0 - smoothstep(0.86, 0.86 + lVw, frac(lV)));
                    lMark *= lOn * (1.0 - smoothstep(19.0, 22.0, abs(lInLane)));
                    lColor = lerp(lColor, lColor * 1.13 + 0.03, lMark * lFade * 0.8);
                }

                // Wind combing the ground : thin zigzag threads drifting along the route, broken into gusts, a little
                // brighter than the grass. Never straight speed lines : only a hint of motion that grows with speed.
                if (_RoadStreaks > 0 && _HS_RoadSpeed > 0.01)
                {
                    const float SPACING = 34.0;
                    float lLane = input.uv.x / SPACING;
                    float lId = floor(lLane);
                    float lRnd = frac(sin(lId * 91.17) * 4375.85);
                    float lY = input.uv.y + _HS_RoadScroll * (0.18 + lRnd * 0.22) + lRnd * 300.0;
                    float lPeriod = 18.0 + lRnd * 14.0;
                    float lZig = (abs(frac(lY / lPeriod) - 0.5) * 2.0 - 0.5) * (5.0 + lRnd * 4.0);
                    float lAcross = abs((frac(lLane) - 0.5) * SPACING - lZig);
                    float lW = max(fwidth(lAcross) * 1.2, 0.05);
                    float lThread = 1.0 - smoothstep(0.35, 0.35 + lW, lAcross);
                    float lGust = frac(lY * (0.0035 + lRnd * 0.002));
                    float lOn = smoothstep(0.0, 0.06, lGust) * (1.0 - smoothstep(0.3 + _HS_RoadSpeed * 0.25, 0.36 + _HS_RoadSpeed * 0.25, lGust));
                    float lNear = saturate(1.0 - distance(input.positionWS, GetCameraPositionWS()) / 450.0);
                    lColor = lerp(lColor, lColor * 1.25 + 0.06, lThread * lOn * step(0.3, lRnd) * _HS_RoadSpeed * 0.6 * lNear);
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
