// Recomposes the six face cameras of PanoramaRig into one wide angle image (past 180 degrees). The projection is a
// blend of a round fisheye of shape s and a cylinder (straight verticals) : see ProjectionMath.cs, kept identical.
Shader "Hyperspeed/Panorama"
{
    Properties
    {
        _Face0 ("Front", 2D) = "black" {}
        _Face1 ("Right", 2D) = "black" {}
        _Face2 ("Left", 2D) = "black" {}
        _Face3 ("Up", 2D) = "black" {}
        _Face4 ("Down", 2D) = "black" {}
        _Face5 ("Back", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Panorama"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Face0); TEXTURE2D(_Face1); TEXTURE2D(_Face2); TEXTURE2D(_Face3); TEXTURE2D(_Face4); TEXTURE2D(_Face5);
            SAMPLER(sampler_linear_clamp);

            // x edge radius, y shape, z cylinder blend, w aspect
            float4 _HS_Proj;
            // x tan of half a face's FOV (guard band), y 1 if the back face rendered
            float4 _HS_PanoFaces;
            float4 _HS_PanoEdgeColor;
            // per face : xy uv scale, zw uv offset (a face may render to part of its texture)
            float4 _HS_FaceST0, _HS_FaceST1, _HS_FaceST2, _HS_FaceST3, _HS_FaceST4, _HS_FaceST5;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.uv = input.positionOS.xy * 0.5 + 0.5;
                output.positionCS = float4(input.positionOS.xy, 0.5, 1.0);
                if (_ProjectionParams.x < 0.0) output.positionCS.y = -output.positionCS.y;
                return output;
            }

            float F(float theta, float s)
            {
                if (s > 0.001) return tan(s * theta) / s;
                if (s < -0.001) return sin(-s * theta) / -s;
                return theta;
            }

            float FInv(float r, float s)
            {
                if (s > 0.001) return atan(s * r) / s;
                if (s < -0.001) return asin(clamp(-s * r, -1.0, 1.0)) / -s;
                return r;
            }

            float3 ScreenToDir(float2 ndc, out float theta)
            {
                float edge = _HS_Proj.x;
                float s = _HS_Proj.y;
                float2 p = float2(ndc.x * edge, ndc.y * edge / _HS_Proj.w);
                float r = length(p);
                theta = FInv(r, s);
                float3 radial = r > 1e-6 ? float3(p / r * sin(theta), cos(theta)) : float3(0, 0, 1);
                float phi = FInv(p.x, s);
                float fx = F(phi, s);
                float k = abs(phi) > 1e-4 ? sin(phi) / fx : 1.0;
                float3 cyl = normalize(float3(sin(phi), p.y * k, cos(phi)));
                float c = _HS_Proj.z;
                theta = lerp(theta, acos(clamp(cyl.z, -1.0, 1.0)), c);
                return normalize(lerp(radial, cyl, c));
            }

            float3 SampleFace(TEXTURE2D_PARAM(tex, smp), float2 proj, float4 st)
            {
                float2 uv = proj / _HS_PanoFaces.x * 0.5 + 0.5;
                uv = uv * st.xy + st.zw;
                return SAMPLE_TEXTURE2D_LOD(tex, smp, uv, 0).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float theta;
                float3 d = ScreenToDir(input.uv * 2.0 - 1.0, theta);
                float3 a = abs(d);
                float3 col;
                // Face bases (camera space) : front +z (R +x, U +y) ; right +x (R -z, U +y) ; left -x (R +z, U +y) ;
                // up +y (R +x, U -z) ; down -y (R +x, U +z) ; back -z (R -x, U +y).
                if (a.z >= a.x && a.z >= a.y)
                {
                    if (d.z > 0.0) col = SampleFace(TEXTURE2D_ARGS(_Face0, sampler_linear_clamp), float2(d.x, d.y) / d.z, _HS_FaceST0);
                    else col = SampleFace(TEXTURE2D_ARGS(_Face5, sampler_linear_clamp), float2(-d.x, d.y) / -d.z, _HS_FaceST5);
                }
                else if (a.x >= a.y)
                {
                    if (d.x > 0.0) col = SampleFace(TEXTURE2D_ARGS(_Face1, sampler_linear_clamp), float2(-d.z, d.y) / d.x, _HS_FaceST1);
                    else col = SampleFace(TEXTURE2D_ARGS(_Face2, sampler_linear_clamp), float2(d.z, d.y) / -d.x, _HS_FaceST2);
                }
                else
                {
                    if (d.y > 0.0) col = SampleFace(TEXTURE2D_ARGS(_Face3, sampler_linear_clamp), float2(d.x, -d.z) / d.y, _HS_FaceST3);
                    else col = SampleFace(TEXTURE2D_ARGS(_Face4, sampler_linear_clamp), float2(d.x, d.z) / -d.y, _HS_FaceST4);
                }
                // Past the back of the sphere : melt into the edge colour.
                float outside = saturate((theta - 3.0) / 0.14);
                col = lerp(col, _HS_PanoEdgeColor.rgb, outside);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
