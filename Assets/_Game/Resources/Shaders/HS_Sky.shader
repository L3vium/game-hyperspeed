// Procedural anime sky : 3 color gradient, sun disc, painterly cloud streaks and stars.
Shader "Hyperspeed/Sky"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.2,0.45,1,1)
        _HorizonColor ("Horizon", Color) = (0.75,0.9,1,1)
        _BottomColor ("Bottom", Color) = (0.9,0.95,1,1)
        [HDR] _SunColor ("Sun", Color) = (2,1.8,1.4,1)
        _SunDir ("Sun Direction", Vector) = (0.3,0.35,0.85,0)
        _CloudColor ("Cloud", Color) = (1,1,1,1)
        _CloudAmount ("Cloud Amount", Range(0,1)) = 0.5
        _Stars ("Stars", Range(0,1)) = 0
        _Rays ("Rays", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                half4 _SunColor;
                float4 _SunDir;
                half4 _CloudColor;
                half _CloudAmount;
                half _Stars;
                half _Rays;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.dir = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 lDir = normalize(input.dir);
                float lY = lDir.y;
                half3 lColor = lY > 0 ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(lY), 0.6))
                                      : lerp(_HorizonColor.rgb, _BottomColor.rgb, saturate(-lY * 3.0));

                float lAngle = atan2(lDir.x, lDir.z);
                // Brush-stroke clouds : stretched noise bands near the horizon.
                float2 lCloudUV = float2(lAngle * 3.0 + _Time.y * 0.01, lY * 14.0);
                float lCloud = Noise(lCloudUV) * 0.65 + Noise(lCloudUV * 2.3) * 0.35;
                float lBand = smoothstep(0.0, 0.25, lY) * smoothstep(0.75, 0.2, lY);
                lCloud = step(1.0 - _CloudAmount * 0.55, lCloud * lBand + lBand * 0.3);
                lColor = lerp(lColor, _CloudColor.rgb, lCloud * 0.85);

                float3 lSun = normalize(_SunDir.xyz);
                float lSunDot = dot(lDir, lSun);
                lColor += _SunColor.rgb * (smoothstep(0.9975, 0.999, lSunDot) + pow(saturate(lSunDot), 64) * 0.4);

                float lRays = step(0.5, frac(atan2(lDir.x - lSun.x, lDir.y - lSun.y) * 4.0)) * pow(saturate(lSunDot), 6);
                lColor += _SunColor.rgb * lRays * _Rays * 0.15;

                float2 lStarUV = float2(lAngle * 80.0, lY * 80.0);
                float lStar = step(0.985, Hash(floor(lStarUV))) * _Stars * saturate(lY * 4.0 + 0.3);
                lColor += lStar * (0.6 + 0.4 * sin(_Time.y * 3.0 + Hash(floor(lStarUV)) * 30.0));
                return half4(lColor, 1);
            }
            ENDHLSL
        }
    }
}
