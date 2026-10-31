// Made with Amplify Shader Editor v1.9.9.12
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Stylized Toon"
{
	Properties
	{
		[Toggle( _USERIMLIGHT_ON )] _UseRimLight( "UseRim Light", Float ) = 0
		[Toggle( _USEOUTLINE_ON )] _UseOutline( "UseOutline", Float ) = 0
		[Toggle( _USESPECULAR_ON )] _UseSpecular( "UseSpecular Highlights", Float ) = 1
		_SpecColor( "Specular Value", Color ) = ( 1, 1, 1, 0 )
		_Thicnkess( "Thickness", Range( 0, 0.1 ) ) = 0
		_AdaptiveThicnkess( "Adaptive Thickness", Range( 0, 1 ) ) = 0
		[KeywordEnum( Normal,Position,UVBaked )] _OutlineType( "Outline Type", Float ) = 0
		[HDR] _OutlineColor( "Outline Color", Color ) = ( 0, 0, 0, 0 )
		_Color( "Color", Color ) = ( 0.6792453, 0.6792453, 0.6792453, 1 )
		_SpecularFaloff( "Specular Falloff", Range( 0, 1 ) ) = 0
		_LightRampOffset( "Light Ramp Offset", Range( -1, 1 ) ) = 0
		_MainTex( "Albedo Texture", 2D ) = "white" {}
		[NoScaleOffset][SingleLineTexture] _LightRampTexture( "Light Ramp Texture", 2D ) = "white" {}
		_SideShineHardness( "Side Shine Hardness", Range( 0, 0.5 ) ) = 0
		_BacklightHardness( "Backlight Hardness", Range( 0, 0.5 ) ) = 0
		_StepOffset( "Step Offset", Range( -0.5, 0.5 ) ) = 0
		[KeywordEnum( Step,DiffuseRamp,Posterize )] _UseLightRamp( "Shading Mode", Float ) = 0
		[HideInInspector] _RampDiffuseTextureLoaded( "RampDiffuseTextureLoaded", Float ) = 1
		[NoScaleOffset][SingleLineTexture] _DiffuseWarpNoise( "Diffuse Warp Noise", 2D ) = "white" {}
		[NoScaleOffset][SingleLineTexture] _SpecularMaskTexture( "Specular Mask Texture", 2D ) = "white" {}
		_SpecularMaskStrength( "Specular Mask Strength", Range( 0, 1 ) ) = 0.1856417
		_WarpStrength( "Warp Strength", Range( -1, 1 ) ) = 0
		_SpecularMaskScale( "Specular Mask Tiling", Float ) = 1
		_WarpTextureScale( "UV Tiling", Float ) = 1
		[Toggle] _UseDiffuseWarp( "UseDiffuse Warp", Float ) = 0
		[Toggle] _UseSpecularMask( "UseSpecular Mask", Float ) = 0
		[HDR] _RimColor( "Rim Color", Color ) = ( 1, 1, 1, 0 )
		_RimThickness( "Rim Thickness", Range( 0, 3 ) ) = 1
		_RimPower( "Rim Power", Range( 1, 12 ) ) = 2
		_RimSmoothness( "Rim Smoothness", Range( 0, 0.5 ) ) = 0
		[Normal] _BumpMap( "Normal Map", 2D ) = "bump" {}
		_NormalMapStrength( "Normal Map Strength", Float ) = 1
		_SpecularPosterizeSteps( "Specular Posterize Steps", Range( 0, 15 ) ) = 0
		_OutlineNoiseScale( "Outline Noise Scale", Float ) = 10
		_OutlineNoiseIntesity( "Outline Noise Intensity", Range( 0, 2 ) ) = 1
		_OutlineDynamicSpeed( "Outline Dynamic Speed", Range( 0, 0.05 ) ) = 0
		[Toggle( _USEDYNAMICOUTLINE_ON )] _UseDynamicOutline( "Use Sketch Outline", Float ) = 0
		[Toggle( _USEENVIRONMENTREFLETION_ON )] _UseEnvironmentRefletion( "UseEnvironment Reflections", Float ) = 0
		_Strength( "Strength", Range( 0, 1 ) ) = 0
		[NoScaleOffset][SingleLineTexture] _SpecGlossMap( "Specular Map", 2D ) = "white" {}
		_Glossiness( "Smoothness", Range( 0, 1 ) ) = 0.5
		_Cutoff( "Alpha Clip Threshold", Range( 0, 1 ) ) = 0
		[HDR] _EmissionColor( "Emission Color", Color ) = ( 0, 0, 0, 0 )
		[HDR][NoScaleOffset][SingleLineTexture] _EmissionMap( "Emission Map", 2D ) = "white" {}
		_UseEmission( "UseEmission", Float ) = 0
		_IndirectLightStrength( "Indirect Light Strength", Range( 0, 1 ) ) = 1
		_SpecularShadowMask( "Specular Shadow Mask", Range( 0, 1 ) ) = 0
		_WarpTextureRotation( "UV Rotation", Range( 0, 180 ) ) = 0
		_SpecularMaskRotation( "Specular Mask Rotation", Range( 0, 180 ) ) = 0
		_BacklightAmount( "Backlight Amount", Range( 0, 1 ) ) = 0.5
		[Toggle] _UseShadows( "Use Shadows", Float ) = 1
		_BacklightIntensity( "Backlight Intensity", Range( 0, 4 ) ) = 1
		[Toggle( _USEBACKLIGHT_ON )] _UseBacklight( "Rim As Backlight & Side Shine", Float ) = 0
		_SideShineIntensity( "Side Shine Intensity", Range( 0, 4 ) ) = 1
		_SideShineAmount( "Side Shine Amount", Range( 0, 0.7 ) ) = 0.2717647
		[NoScaleOffset][SingleLineTexture] _HalftoneTexture( "Halftone Texture", 2D ) = "white" {}
		_HalftoneSmoothness( "Halftone Smoothness", Range( 0, 2 ) ) = 0.3
		_HalftoneEdgeOffset( "Halftone Edge Offset", Range( -1, 1 ) ) = 0.1
		_HalftoneThreshold( "Halftone Threshold", Range( 0, 0.15 ) ) = 0.1
		_HalftoneColor( "Halftone Color", Color ) = ( 0, 0, 0, 1 )
		[NoScaleOffset] _Hatch2( "Hatch Texture Brighter", 2D ) = "white" {}
		[NoScaleOffset] _Hatch1( "Hatch Texture Darker", 2D ) = "white" {}
		_MaxScaleDependingOnCamera( "Max Scale Depends On Camera", Range( 1, 10 ) ) = 1
		[Toggle( _USEHATCHINGCONSTANTSCALE_ON )] _UseHatchingConstantScale( "Hatching Constant Scale", Float ) = 1
		[KeywordEnum( None,Haftone,Hatching )] _OverlayMode1( "Overlay Mode", Float ) = 0
		_ShapeSmoothness( "Transition Smoothness", Range( 0, 1 ) ) = 0.2
		_OverlayRotation( "UV Rotation", Range( 0, 180 ) ) = 0
		_OverlayUVTilling( "UV Tiling", Float ) = 0
		[Toggle( _USESCREENUVS_ON )] _UseScreenUvs( "Screen Uvs", Float ) = 0
		[Toggle( _USESCREENUVSSPECULAR_ON )] _UseScreenUvsSpecular( "Screen Uvs", Float ) = 0
		_Darken( "Darken", Range( -1, 1 ) ) = 0
		_Lighten( "Lighten", Range( -1, 1 ) ) = 0
		_AdditionalLightsSmoothnessMultiplier( "Additional Lights Specular Size", Range( 0, 2 ) ) = 1
		_SmoothnessMultiplier( "Main Specular Size", Range( 0, 2 ) ) = 1
		_AdditionalLightsIntesity( "Additional Lights Intensity", Range( 0, 6 ) ) = 1
		[Toggle( _USEADDITIONALLIGHTSDIFFUSE_ON )] _UseAdditionalLightsDiffuse( "UseAdditional Lights", Float ) = 1
		_AdditionalLightsAmount( "Additional Lights Size", Range( 0, 1 ) ) = 1
		_AdditionalLightsFaloff( "Additional Lights Falloff", Range( 0, 1 ) ) = 1
		_HatchingColor( "Hatching Color", Color ) = ( 0, 0, 0, 1 )
		[KeywordEnum( None,Haftone,Hatching )] _OverlayMode( "Overlay Mode", Float ) = 0
		[Toggle( _USEPURESKETCH_ON )] _UsePureSketch( "Pure Sketch", Float ) = 1
		_UVAnimationSpeedWarp( "UV Animation Speed", Range( 0, 5 ) ) = 0
		_UVAnimationSpeedSpec( "UV Animation Speed", Range( 0, 5 ) ) = 0
		_UVAnimationSpeed( "UV Animation Speed", Range( 0, 5 ) ) = 0
		_UVSrcrollAngleSpec( "UV Scroll Angle", Range( 0, 360 ) ) = 0
		_UVSrcrollAngle( "UV Scroll Angle", Range( 0, 360 ) ) = 0
		_UVSrcrollAngleWarp( "UV Scroll Angle", Range( 0, 360 ) ) = 0
		_UVScrollSpeedWarp( "UV Scroll Speed", Range( 0, 0.1 ) ) = 0
		_UVScrollSpeed( "UV Scroll Speed", Range( 0, 0.1 ) ) = 0
		_UVScrollSpeedSpec( "UV Scroll Speed", Range( 0, 0.1 ) ) = 0
		_DiffusePosterizeSteps( "Posterize Steps", Range( 1, 10 ) ) = 3
		_DiffusePosterizePower( "Posterize Power", Range( 0.5, 3 ) ) = 1
		_DiffusePosterizeOffset( "Posterize Offset", Range( -0.5, 0.5 ) ) = 0
		_MainLightIntesity( "Main Light Intensity", Range( 0, 6 ) ) = 1
		_OutlineTextureStrength( "Texture Strength ", Range( 0, 1 ) ) = 0
		_ShadowColor( "Shadow Color", Color ) = ( 0, 0, 0, 0 )
		_HalftoneToonAffect( "Toon Affect", Range( 0, 1 ) ) = 0
		_IndirectLightAffectOnHatch( "Indirect Light Affect On Hatch", Range( 0, 1 ) ) = 0
		[Toggle( _USEDIFFUSEWARPASOVERLAY_ON )] _UseDiffuseWarpAsOverlay( "Impact Shadows", Float ) = 0
		[Toggle( _DIFFUSEWARPAFFECTHALFTONE_ON )] _DiffuseWarpAffectHalftone( "Diffuse Warp Affect Halftone", Float ) = 0
		_DiffuseWarpBrightnessOffset( "Brightness Offset", Float ) = 1.12
		[Toggle( _USESCREENUVSWARP_ON )] _UseScreenUvsWarp( "Screen Uvs", Float ) = 0
		[Toggle] _StepHalftoneTexture( "Step Halftone Texture", Float ) = 0
		_HaltonePatternSize( "Halftone Pattern Size", Range( 0, 1 ) ) = 0
		_RimShadowColor( "Rim Shadow Color", Color ) = ( 0, 0.05551431, 0.9622642, 0 )
		[KeywordEnum( NoSplit,MultiplyWithDiffuse,UseSecondColor )] _RimSplitColor( "Rim Split Color", Float ) = 0
		_OcclusionMap( "Occlusion Map", 2D ) = "white" {}
		_OcclusionStrength( "Occlusion Strength ", Range( 0, 1 ) ) = 1
		[Toggle( _USEADAPTIVESCREENUVS_ON )] _UseAdaptiveScreenUvs( "Adaptive Screen Uvs", Float ) = 0
		[Toggle( _USEADAPTIVEUVSSPECULAR_ON )] _UseAdaptiveUvsSpecular( "Adaptive Screen Uvs", Float ) = 0
		[Toggle( _USEADAPTIVESCREENUVSWARP_ON )] _UseAdaptiveScreenUvsWarp( "Adaptive Screen Uvs ", Float ) = 0
		[Toggle( _INVERSEMASK_ON )] _InverseMask( "Inverse Mask", Float ) = 0
		_PaperTilling( "Paper Tiling", Float ) = 1
		[NoScaleOffset] _PaperTexture( "Paper Texture", 2D ) = "white" {}


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		//[HideInInspector][ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0

		//[HideInInspector][ToggleUI] _AddPrecomputedVelocity("Add Precomputed Velocity", Float) = 1
		//[HideInInspector] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 1

		//[HideInInspector] _AlphaClip("__clip", Float) = 0.0
	}

	SubShader
	{
		PackageRequirements
		{
			"com.unity.render-pipelines.universal": "[17.0,18.0]"
		}

		

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Unlit" }

	LOD 0

		ZWrite On
		Cull Back
		AlphaToMask Off
		ColorMask RGBA
		Blend One Zero, One Zero
		BlendOp Add, Add

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#define ASE_ADJUST_CLIP_POSITION( x ) x

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			Name "ExtraPrePass"
			

			Cull Front
			ZWrite On
			ZTest LEqual
			Offset 0,0
			ColorMask RGBA

			

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170004


			#pragma vertex vert
			#pragma fragment frag

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES3
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES3
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#pragma shader_feature_local _USEOUTLINE_ON
			#pragma shader_feature_local _USEDYNAMICOUTLINE_ON
			#pragma shader_feature_local _OUTLINETYPE_NORMAL _OUTLINETYPE_POSITION _OUTLINETYPE_UVBAKED


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 positionWSAndFogFactor : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				half4 tangentWS : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RimShadowColor;
			float4 _HalftoneColor;
			float4 _RimColor;
			float4 _Color;
			float4 _OcclusionMap_ST;
			float4 _OutlineColor;
			float4 _ShadowColor;
			float4 _HatchingColor;
			float4 _EmissionColor;
			float4 _SpecColor;
			float _SmoothnessMultiplier;
			float _MainLightIntesity;
			float _UseSpecularMask;
			float _AdditionalLightsIntesity;
			float _StepHalftoneTexture;
			float _SpecularMaskScale;
			float _SpecularFaloff;
			float _UVAnimationSpeedSpec;
			float _UVScrollSpeedSpec;
			float _UVSrcrollAngleSpec;
			float _SpecularMaskRotation;
			float _HaltonePatternSize;
			float _SpecularMaskStrength;
			float _RampDiffuseTextureLoaded;
			float _Strength;
			float _Lighten;
			float _Darken;
			float _IndirectLightAffectOnHatch;
			float _UseEmission;
			float _SideShineAmount;
			float _SideShineHardness;
			float _SpecularShadowMask;
			float _SideShineIntensity;
			float _BacklightHardness;
			float _BacklightIntensity;
			float _RimPower;
			float _RimThickness;
			float _RimSmoothness;
			float _SpecularPosterizeSteps;
			float _BacklightAmount;
			float _AdditionalLightsSmoothnessMultiplier;
			float _UVAnimationSpeed;
			float _OverlayRotation;
			float _WarpStrength;
			float _WarpTextureRotation;
			float _UVAnimationSpeedWarp;
			float _UVSrcrollAngleWarp;
			float _UVScrollSpeedWarp;
			float _WarpTextureScale;
			float _UseDiffuseWarp;
			float _StepOffset;
			float _OcclusionStrength;
			float _OutlineTextureStrength;
			float _Thicnkess;
			float _OutlineNoiseIntesity;
			float _OutlineNoiseScale;
			float _OutlineDynamicSpeed;
			float _AdaptiveThicnkess;
			float _NormalMapStrength;
			float _Glossiness;
			float _UseShadows;
			float _LightRampOffset;
			float _MaxScaleDependingOnCamera;
			float _UVSrcrollAngle;
			float _UVScrollSpeed;
			float _OverlayUVTilling;
			float _HalftoneSmoothness;
			float _HalftoneEdgeOffset;
			float _HalftoneThreshold;
			float _ShapeSmoothness;
			float _HalftoneToonAffect;
			float _IndirectLightStrength;
			float _AdditionalLightsFaloff;
			float _AdditionalLightsAmount;
			float _DiffusePosterizeSteps;
			float _DiffusePosterizePower;
			float _DiffusePosterizeOffset;
			float _DiffuseWarpBrightnessOffset;
			float _PaperTilling;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _MainTex;
			sampler2D _OcclusionMap;


			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				float lerpResult59 = lerp( 1.0 , distance( _WorldSpaceCameraPos , ase_positionWS ) , _AdaptiveThicnkess);
				#if defined( _OUTLINETYPE_NORMAL )
				float3 staticSwitch57 = input.normalOS;
				#elif defined( _OUTLINETYPE_POSITION )
				float3 staticSwitch57 = input.positionOS.xyz;
				#elif defined( _OUTLINETYPE_UVBAKED )
				float3 staticSwitch57 = float3( input.ase_texcoord3.xy ,  0.0 );
				#else
				float3 staticSwitch57 = input.normalOS;
				#endif
				float2 temp_cast_1 = (( ( _TimeParameters.x ) * _OutlineDynamicSpeed )).xx;
				float2 texCoord591 = input.ase_texcoord.xy * float2( 1,1 ) + temp_cast_1;
				float simplePerlin2D590 = snoise( texCoord591*_OutlineNoiseScale );
				simplePerlin2D590 = simplePerlin2D590*0.5 + 0.5;
				#ifdef _USEDYNAMICOUTLINE_ON
				float3 staticSwitch606 = ( ( simplePerlin2D590 * _OutlineNoiseIntesity ) * staticSwitch57 );
				#else
				float3 staticSwitch606 = staticSwitch57;
				#endif
				#ifdef _USEOUTLINE_ON
				float3 staticSwitch365 = ( lerpResult59 * ( staticSwitch606 * _Thicnkess ) );
				#else
				float3 staticSwitch365 = float3( 0,0,0 );
				#endif
				
				output.ase_texcoord3.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord3.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = staticSwitch365;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				float fogFactor = 0;
				#if defined(ASE_FOG) && !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				#endif

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.positionWSAndFogFactor = float4( vertexInput.positionWS, fogFactor );
				output.normalWS = normalInput.normalWS;
				output.tangentWS = half4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord3 = input.ase_texcoord3;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord3 = patch[0].ase_texcoord3 * bary.x + patch[1].ase_texcoord3 * bary.y + patch[2].ase_texcoord3 * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input 
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined( _SURFACE_TYPE_TRANSPARENT )
					const bool isTransparent = true;
				#else
					const bool isTransparent = false;
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord = TransformWorldToShadowCoord(input.positionWSAndFogFactor.xyz);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 PositionWS = input.positionWSAndFogFactor.xyz;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				half3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float3 NormalWS = input.normalWS * renormFactor;

				float2 uv_OcclusionMap = input.ase_texcoord3.xy * _OcclusionMap_ST.xy + _OcclusionMap_ST.zw;
				float4 tex2DNode362 = tex2D( _MainTex, uv_OcclusionMap );
				float4 lerpResult1448 = lerp( float4( 1,1,1,0 ) , tex2DNode362 , _OutlineTextureStrength);
				
				float lerpResult1655 = lerp( 1.0 , tex2D( _OcclusionMap, uv_OcclusionMap ).r , _OcclusionStrength);
				float4 appendResult1656 = (float4(lerpResult1655 , lerpResult1655 , lerpResult1655 , 1.0));
				float4 MainTexture364 = ( _Color * tex2DNode362 * appendResult1656 );
				float temp_output_673_0 = ( MainTexture364.a * 1.0 );
				#ifdef _USEOUTLINE_ON
				float staticSwitch1349 = temp_output_673_0;
				#else
				float staticSwitch1349 = -2.0;
				#endif
				

				float3 Color = ( _OutlineColor * lerpResult1448 ).rgb;
				float Alpha = staticSwitch1349;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
				inputData.normalWS = NormalWS;
				inputData.viewDirectionWS = ViewDirWS;

				#ifdef ASE_FOG
					inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.positionWSAndFogFactor.w);

					#ifdef TERRAIN_SPLAT_ADDPASS
						Color.rgb = MixFogColor(Color.rgb, half3(0,0,0), inputData.fogCoord);
					#else
						Color.rgb = MixFog(Color.rgb, inputData.fogCoord);
					#endif
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				#if defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, OutputAlpha( Alpha, isTransparent ) );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }

			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA
			Blend One Zero, One Zero
			BlendOp Add, Add

			

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#define UNLIT_DEFAULT_SSAO
			#define UNLIT_DEFAULT_DECAL_BLENDING
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170004


			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

			#pragma multi_compile_fragment _ DEBUG_DISPLAY

			#pragma vertex vert
			#pragma fragment frag

			// Option "Keep Lighting Variants" @david please keep this note for future changes in Unlit  lighting
			//#define UNLIT_REALTIME_LIGHTING 1
			//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			//#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			//#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			//#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
			//#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
			//#pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
			//#pragma multi_compile _ REFLECTION_PROBE_ROTATION
			//#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			//#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			//#pragma multi_compile _ SHADOWS_SHADOWMASK
			//#pragma multi_compile_fragment _ _LIGHT_LAYERS
			//#pragma multi_compile_fragment _ _LIGHT_COOKIES
			//#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

			#define SHADERPASS SHADERPASS_UNLIT

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#else
			#pragma multi_compile_fog
			#endif
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_SCREEN_POSITION
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_WORLD_TANGENT
			#define ASE_NEEDS_WORLD_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_BITANGENT
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_VIEW_DIR
			#define ASE_NEEDS_FRAG_SHADOWCOORDS
			#define ASE_NEEDS_FRAG_POSITION
			#pragma shader_feature_local _OVERLAYMODE_NONE _OVERLAYMODE_HAFTONE _OVERLAYMODE_HATCHING
			#pragma shader_feature_local _USELIGHTRAMP_STEP _USELIGHTRAMP_DIFFUSERAMP _USELIGHTRAMP_POSTERIZE
			#pragma shader_feature_local _USESCREENUVSWARP_ON
			#pragma shader_feature_local _USEADAPTIVESCREENUVSWARP_ON
			#pragma shader_feature_local _USEDIFFUSEWARPASOVERLAY_ON
			#pragma shader_feature_local _USEADDITIONALLIGHTSDIFFUSE_ON
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			#if UNITY_VERSION >= 60010000
			#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
			#endif
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON
			#pragma shader_feature_local _DIFFUSEWARPAFFECTHALFTONE_ON
			#pragma shader_feature_local _USESCREENUVS_ON
			#pragma shader_feature_local _USEADAPTIVESCREENUVS_ON
			#pragma shader_feature_local _USESPECULAR_ON
			#pragma shader_feature_local _INVERSEMASK_ON
			#pragma shader_feature_local _USESCREENUVSSPECULAR_ON
			#pragma shader_feature_local _USEADAPTIVEUVSSPECULAR_ON
			#pragma shader_feature_local _OVERLAYMODE1_NONE _OVERLAYMODE1_HAFTONE _OVERLAYMODE1_HATCHING
			#pragma shader_feature_local _USEENVIRONMENTREFLETION_ON
			#if UNITY_VERSION < 60010000
			#pragma multi_compile _ _FORWARD_PLUS
			#endif
			#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
			#pragma shader_feature_local _RIMSPLITCOLOR_NOSPLIT _RIMSPLITCOLOR_MULTIPLYWITHDIFFUSE _RIMSPLITCOLOR_USESECONDCOLOR
			#pragma shader_feature_local _USERIMLIGHT_ON
			#pragma shader_feature_local _USEBACKLIGHT_ON
			#pragma shader_feature_local _USEPURESKETCH_ON
			#pragma shader_feature_local _USEHATCHINGCONSTANTSCALE_ON
			#pragma multi_compile _ _LIGHT_COOKIES 
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
			#pragma multi_compile _ REFLECTION_PROBE_ROTATION
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile_fragment _ _LIGHT_LAYERS


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 positionWSAndFogFactor : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				half4 tangentWS : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 lightmapUVOrVertexSH : TEXCOORD5;
				float4 dynamicLightmapUV : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RimShadowColor;
			float4 _HalftoneColor;
			float4 _RimColor;
			float4 _Color;
			float4 _OcclusionMap_ST;
			float4 _OutlineColor;
			float4 _ShadowColor;
			float4 _HatchingColor;
			float4 _EmissionColor;
			float4 _SpecColor;
			float _SmoothnessMultiplier;
			float _MainLightIntesity;
			float _UseSpecularMask;
			float _AdditionalLightsIntesity;
			float _StepHalftoneTexture;
			float _SpecularMaskScale;
			float _SpecularFaloff;
			float _UVAnimationSpeedSpec;
			float _UVScrollSpeedSpec;
			float _UVSrcrollAngleSpec;
			float _SpecularMaskRotation;
			float _HaltonePatternSize;
			float _SpecularMaskStrength;
			float _RampDiffuseTextureLoaded;
			float _Strength;
			float _Lighten;
			float _Darken;
			float _IndirectLightAffectOnHatch;
			float _UseEmission;
			float _SideShineAmount;
			float _SideShineHardness;
			float _SpecularShadowMask;
			float _SideShineIntensity;
			float _BacklightHardness;
			float _BacklightIntensity;
			float _RimPower;
			float _RimThickness;
			float _RimSmoothness;
			float _SpecularPosterizeSteps;
			float _BacklightAmount;
			float _AdditionalLightsSmoothnessMultiplier;
			float _UVAnimationSpeed;
			float _OverlayRotation;
			float _WarpStrength;
			float _WarpTextureRotation;
			float _UVAnimationSpeedWarp;
			float _UVSrcrollAngleWarp;
			float _UVScrollSpeedWarp;
			float _WarpTextureScale;
			float _UseDiffuseWarp;
			float _StepOffset;
			float _OcclusionStrength;
			float _OutlineTextureStrength;
			float _Thicnkess;
			float _OutlineNoiseIntesity;
			float _OutlineNoiseScale;
			float _OutlineDynamicSpeed;
			float _AdaptiveThicnkess;
			float _NormalMapStrength;
			float _Glossiness;
			float _UseShadows;
			float _LightRampOffset;
			float _MaxScaleDependingOnCamera;
			float _UVSrcrollAngle;
			float _UVScrollSpeed;
			float _OverlayUVTilling;
			float _HalftoneSmoothness;
			float _HalftoneEdgeOffset;
			float _HalftoneThreshold;
			float _ShapeSmoothness;
			float _HalftoneToonAffect;
			float _IndirectLightStrength;
			float _AdditionalLightsFaloff;
			float _AdditionalLightsAmount;
			float _DiffusePosterizeSteps;
			float _DiffusePosterizePower;
			float _DiffusePosterizeOffset;
			float _DiffuseWarpBrightnessOffset;
			float _PaperTilling;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _DiffuseWarpNoise;
			sampler2D _BumpMap;
			sampler2D _OcclusionMap;
			sampler2D _LightRampTexture;
			sampler2D _MainTex;
			sampler2D _HalftoneTexture;
			sampler2D _SpecGlossMap;
			sampler2D _SpecularMaskTexture;
			sampler2D _EmissionMap;
			sampler2D _Hatch1;
			sampler2D _Hatch2;
			sampler2D _PaperTexture;


			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
			float Posterize1331( float In, float Steps )
			{
				return  floor(In / (1 / Steps)) * (1 / Steps);
			}
			
			half4 CalculateShadowMask1_g61147( half2 LightmapUV )
			{
				#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
				return SAMPLE_SHADOWMASK( LightmapUV.xy );
				#elif !defined (LIGHTMAP_ON)
				return unity_ProbesOcclusion;
				#else
				return half4( 1, 1, 1, 1 );
				#endif
			}
			
			float3 AdditionalLight( float3 WorldPosition, float3 WorldNormal, float3 LightWrapVector, float SMin, float SMax, float Faloff, float4 shadowmask, float2 ScreenUV )
			{
				float3 Color = 0;
					
					
				#define SUM_LIGHT(Light)\
						float3 DotVector = dot(light.direction,WorldNormal);	\
					half3 AttLightColor = (light.shadowAttenuation * light.distanceAttenuation);\
					 float3 colout = max(float3(0.f,0.f,0.f),LightWrapVector + (1-LightWrapVector) * DotVector )*AttLightColor*light.color; \
					float maxColor = max(colout.r,max(colout.g,colout.b));\
					float3 outColor = smoothstep(SMin,SMax,maxColor)*light.color;\
					 Color += outColor;
					InputData inputData = (InputData)0;
					inputData.normalizedScreenSpaceUV = ScreenUV;
					inputData.positionWS = WorldPosition;
					uint meshRenderingLayers = GetMeshRenderingLayer();
					uint pixelLightCount = GetAdditionalLightsCount();
					#if USE_FORWARD_PLUS
					for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
					{
						FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
						Light light = GetAdditionalLight(lightIndex, WorldPosition,shadowmask);
						#ifdef _LIGHT_LAYERS
						if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
						#endif
						{
							SUM_LIGHT( light );
						}
					}
					#endif
					LIGHT_LOOP_BEGIN( pixelLightCount )
						Light light = GetAdditionalLight(lightIndex, WorldPosition,shadowmask);
						#ifdef _LIGHT_LAYERS
						if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
						#endif
						{
							SUM_LIGHT( light );
						}
					LIGHT_LOOP_END
				return Color;
			}
			
			half3 ASEIndirectDiffuse( PackedVaryings input, half3 normalWS, float3 positionWS, half3 viewDirWS )
			{
			#if defined( DYNAMICLIGHTMAP_ON )
				return SAMPLE_GI( input.lightmapUVOrVertexSH.xy, input.dynamicLightmapUV.xy, 0, normalWS );
			#elif defined( LIGHTMAP_ON )
				return SAMPLE_GI( input.lightmapUVOrVertexSH.xy, 0, normalWS );
			#elif defined( PROBE_VOLUMES_L1 ) || defined( PROBE_VOLUMES_L2 )
				return SampleProbeVolumePixel( SampleSH( normalWS ), positionWS, normalWS, viewDirWS, input.positionCS.xy );
			#else
				return SampleSH( normalWS );
			#endif
			}
			
			float3 AdditionalLightsSpecularMy( float3 WorldPosition, float3 WorldNormal, float3 WorldView, float3 SpecColor, float Smoothness, float Steps, float SpecFaloff )
			{
				float3 Color = 0;
				Smoothness = exp2(10 * Smoothness + 1);
				int numLights = GetAdditionalLightsCount();
				for(int i = 0; i<numLights;i++)
				{
					
							#if VERSION_GREATER_EQUAL(10, 1)
							Light light = GetAdditionalLight(i, WorldPosition, half4(1,1,1,1));
							// see AdditionalLights_float for explanation of this
						#else
							Light light = GetAdditionalLight(i, WorldPosition);
						#endif
					
					half3 AttLightColor = light.color *(light.distanceAttenuation * light.shadowAttenuation);
					Color += LightingSpecular(AttLightColor, light.direction, WorldNormal, WorldView, half4(SpecColor, 0), Smoothness);	
				}
				float IN = max(Color.b,max(Color.r,Color.g));
				float minOut = 0.5 * SpecFaloff - 0.005;
				float faloff = lerp(IN, smoothstep(minOut, 0.5, IN), SpecFaloff);
				if(Steps < 1)
				{
				    return Color *faloff;
				}
				else
				{
				    return  Color *floor(faloff / (1 / Steps)) * (1 / Steps);
				}
			}
			
			float FaloffPosterize( float IN, float SpecFaloff, float Steps )
			{
				float minOut = 0.5 * SpecFaloff - 0.005;
				float faloff = lerp(IN, smoothstep(minOut, 0.5, IN), SpecFaloff);
				if(Steps < 1)
				{
				    return faloff;
				}
				else
				{
				    return  floor(faloff / (1 / Steps)) * (1 / Steps);
				}
			}
			
			float3 Hatching476( float2 _uv, float color, sampler2D _Hatch0, sampler2D _Hatch1 )
			{
				float intensity = color;
				    float3 hatch0 = tex2D(_Hatch0, _uv).rgb;
				    float3 hatch1 = tex2D(_Hatch1, _uv).rgb;
				    float3 overbright = max(0, intensity - 1.0);
				    float3 weightsA = saturate((intensity * 6.0) + float3(-0, -1, -2));
				    float3 weightsB = saturate((intensity * 6.0) + float3(-3, -4, -5));
				    weightsA.xy -= weightsA.yz;
				    weightsA.z -= weightsB.x;
				    weightsB.xy -= weightsB.yz;
				    hatch0 = hatch0 * weightsA;
				    hatch1 = hatch1 * weightsB;
				    float3 hatching = overbright + hatch0.r +
				        hatch0.g + hatch0.b +
				        hatch1.r + hatch1.g +
				        hatch1.b;
				    return hatching;
				    return hatching;
			}
			
			float3 HatchingConstantScale491( float2 _uv, float _intensity, sampler2D _Hatch0, sampler2D _Hatch1, float _dist, float _MaxScaleDependingOnCamera )
			{
					float log2_dist = log2(_dist)-0.2;
					
					float2 floored_log_dist = floor( (log2_dist + float2(0.0, 1.0) ) * 0.5) *2.0 - float2(0.0, 1.0);				
					float2 uv_scale = min(_MaxScaleDependingOnCamera, pow(2.0, floored_log_dist));
					
					float uv_blend = abs(frac(log2_dist * 0.5) * 2.0 - 1.0);
					
					float2 scaledUVA = _uv / uv_scale.x; // 16
					float2 scaledUVB = _uv / uv_scale.y; // 8 
					float3 hatch0A = tex2D(_Hatch0, scaledUVA).rgb;
					float3 hatch1A = tex2D(_Hatch1, scaledUVA).rgb;
					float3 hatch0B = tex2D(_Hatch0, scaledUVB).rgb;
					float3 hatch1B = tex2D(_Hatch1, scaledUVB).rgb;
					float3 hatch0 = lerp(hatch0A, hatch0B, uv_blend);
					float3 hatch1 = lerp(hatch1A, hatch1B, uv_blend);
					float3 overbright = max(0, _intensity - 1.0);
					float3 weightsA = saturate((_intensity * 6.0) + float3(-0, -1, -2));
					float3 weightsB = saturate((_intensity * 6.0) + float3(-3, -4, -5));
					weightsA.xy -= weightsA.yz;
					weightsA.z -= weightsB.x;
					weightsB.xy -= weightsB.yz;
					hatch0 = hatch0 * weightsA;
					hatch1 = hatch1 * weightsB;
					float3 hatching = overbright + hatch0.r +
						hatch0.g + hatch0.b +
						hatch1.r + hatch1.g +
						hatch1.b;
					return hatching;
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 ase_normalWS = TransformObjectToWorldNormal( input.normalOS );
				OUTPUT_LIGHTMAP_UV( input.ase_texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy );
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				#if !defined( OUTPUT_SH4 )
				OUTPUT_SH( ase_positionWS, ase_normalWS, GetWorldSpaceNormalizeViewDir( ase_positionWS ), output.lightmapUVOrVertexSH.xyz );
				#elif UNITY_VERSION > 60000009
				OUTPUT_SH4( ase_positionWS, ase_normalWS, GetWorldSpaceNormalizeViewDir( ase_positionWS ), output.lightmapUVOrVertexSH.xyz, output.probeOcclusion );
				#else
				OUTPUT_SH4( ase_positionWS, ase_normalWS, GetWorldSpaceNormalizeViewDir( ase_positionWS ), output.lightmapUVOrVertexSH.xyz );
				#endif
				#if defined( DYNAMICLIGHTMAP_ON )
				output.dynamicLightmapUV.xy = input.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif
				
				output.ase_texcoord3.xy = input.ase_texcoord.xy;
				output.ase_texcoord4 = input.positionOS;
				output.ase_texcoord3.zw = input.ase_texcoord1.xy;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				#ifdef ASE_CUSTOM_MOTION_VECTOR
					// Declared so the Motion Vector output port surfaces on the master node; only consumed by the motion vector passes.
					float3 aseCustomMotionVector = float3(0, 0, 0);
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				float fogFactor = 0;
				#if defined(ASE_FOG) && !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				#endif

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.positionWSAndFogFactor = float4( vertexInput.positionWS, fogFactor );
				output.normalWS = normalInput.normalWS;
				output.tangentWS = half4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1 = input.ase_texcoord1;
				output.texcoord2 = input.texcoord2;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				output.texcoord2 = patch[0].texcoord2 * bary.x + patch[1].texcoord2 * bary.y + patch[2].texcoord2 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						#if ( UNITY_VERSION >= 60020000 )
						, out uint outRenderingLayers : SV_Target1
						#else
						, out float4 outRenderingLayers : SV_Target1
						#endif
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined( _SURFACE_TYPE_TRANSPARENT )
					const bool isTransparent = true;
				#else
					const bool isTransparent = false;
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					float4 shadowCoord = TransformWorldToShadowCoord( input.positionWSAndFogFactor.xyz );
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 PositionWS = input.positionWSAndFogFactor.xyz;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				half3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float3 NormalWS = input.normalWS * renormFactor;

				float ase_lightIntensity = max( max( _MainLightColor.r, _MainLightColor.g ), _MainLightColor.b ) + 1e-7;
				float4 ase_lightColor = float4( _MainLightColor.rgb / ase_lightIntensity, ase_lightIntensity );
				float4 temp_output_1885_0 = max( ( ase_lightColor * ase_lightColor.a ), float4( 0,0,0,0 ) );
				float temp_output_371_0 = ( _StepOffset + 0.5 );
				float2 temp_cast_0 = (_WarpTextureScale).xx;
				float mulTime1523 = _TimeParameters.x * _UVScrollSpeedWarp;
				float temp_output_1518_0 = radians( _UVSrcrollAngleWarp );
				float2 appendResult1520 = (float2(cos( temp_output_1518_0 ) , sin( temp_output_1518_0 )));
				float2 temp_output_1522_0 = ( mulTime1523 * appendResult1520 );
				float2 texCoord1495 = input.ase_texcoord3.xy * temp_cast_0 + temp_output_1522_0;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ScreenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float2 appendResult1847 = (float2(( _WarpTextureScale * ( _ScaledScreenParams.x / _ScaledScreenParams.y ) ) , _WarpTextureScale));
				float4 unityObjectToClipPos2_g211 = TransformWorldToHClip( TransformObjectToWorld( ( input.ase_texcoord4.xyz ).xyz ) );
				float4 computeScreenPos3_g211 = ComputeScreenPos( unityObjectToClipPos2_g211 );
				float temp_output_29_0_g211 = ( _WarpTextureScale / 4.0 );
				float4 unityObjectToClipPos13_g211 = TransformWorldToHClip( TransformObjectToWorld( ( float3( 0, 0, 0 ) ).xyz ) );
				float4 computeScreenPos17_g211 = ComputeScreenPos( unityObjectToClipPos13_g211 );
				float4 transform20_g211 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float4 break22_g211 = ( ( ( ( computeScreenPos3_g211 / (computeScreenPos3_g211).w ) * temp_output_29_0_g211 ) - ( temp_output_29_0_g211 * ( computeScreenPos17_g211 / (computeScreenPos17_g211).w ) ) ) * distance( ( float4( _WorldSpaceCameraPos , 0.0 ) - transform20_g211 ) , float4( 0,0,0,0 ) ) );
				float4 break26_g211 = _ScaledScreenParams;
				float4 appendResult24_g211 = (float4(( break22_g211.x * ( break26_g211.x / break26_g211.y ) ) , break22_g211.y , break22_g211.z , break22_g211.w));
				#ifdef _USEADAPTIVESCREENUVSWARP_ON
				float4 staticSwitch1664 = appendResult24_g211;
				#else
				float4 staticSwitch1664 = ( ase_grabScreenPosNorm * float4( appendResult1847, 0.0 , 0.0 ) );
				#endif
				float mulTime1525 = _TimeParameters.x * _UVAnimationSpeedWarp;
				float temp_output_1527_0 = ( floor( ( mulTime1525 % 2.0 ) ) * 0.5 );
				float2 appendResult1531 = (float2(temp_output_1527_0 , temp_output_1527_0));
				#ifdef _USESCREENUVSWARP_ON
				float4 staticSwitch1514 = ( staticSwitch1664 + float4( appendResult1531, 0.0 , 0.0 ) + float4( temp_output_1522_0, 0.0 , 0.0 ) );
				#else
				float4 staticSwitch1514 = float4( texCoord1495, 0.0 , 0.0 );
				#endif
				float cos1489 = cos( radians( _WarpTextureRotation ) );
				float sin1489 = sin( radians( _WarpTextureRotation ) );
				float2 rotator1489 = mul( staticSwitch1514.xy - float2( 0.5,0.5 ) , float2x2( cos1489 , -sin1489 , sin1489 , cos1489 )) + float2( 0.5,0.5 );
				float BNLightWarpVector250 = ( _UseDiffuseWarp == 1.0 ? ( tex2D( _DiffuseWarpNoise, rotator1489 ).r * _WarpStrength ) : 0.0 );
				float2 uv_OcclusionMap = input.ase_texcoord3.xy * _OcclusionMap_ST.xy + _OcclusionMap_ST.zw;
				float3 lerpResult1536 = lerp( float3( 0, 0, 1 ) , UnpackNormalScale( tex2D( _BumpMap, uv_OcclusionMap ), 1.0f ) , _NormalMapStrength);
				float3 tanToWorld0 = float3( TangentWS.x, BitangentWS.x, NormalWS.x );
				float3 tanToWorld1 = float3( TangentWS.y, BitangentWS.y, NormalWS.y );
				float3 tanToWorld2 = float3( TangentWS.z, BitangentWS.z, NormalWS.z );
				float3 tanNormal1537 = lerpResult1536;
				float3 worldNormal1537 = normalize( float3( dot( tanToWorld0, tanNormal1537 ), dot( tanToWorld1, tanNormal1537 ), dot( tanToWorld2, tanNormal1537 ) ) );
				float3 BNCurrentNormal1538 = worldNormal1537;
				float dotResult234 = dot( BNCurrentNormal1538 , _MainLightPosition.xyz );
				float BNNDotL233 = dotResult234;
				float localLightAttenuation1412 = ( 0.0 );
				float3 WorldPos1412 = PositionWS;
				float DistanceAtten1412 = 0;
				float ShadowAtten1412 = 0;
				{
				    #if SHADOWS_SCREEN
				        half4 clipPos = TransformWorldToHClip(WorldPos1412);
				        half4 shadowCoord = ComputeScreenPos(clipPos);
				    #else
				        half4 shadowCoord = TransformWorldToShadowCoord(WorldPos1412);
				    #endif
				    Light mainLight = GetMainLight(shadowCoord);
				    DistanceAtten1412 = mainLight.distanceAttenuation;
				    ShadowAtten1412 = mainLight.shadowAttenuation;
				}
				float3 BNAttenuationColor244 = ( ase_lightColor.rgb * DistanceAtten1412 );
				float3 temp_output_260_0 = ( max( ( BNLightWarpVector250 + ( ( 1.0 - BNLightWarpVector250 ) * BNNDotL233 ) ), float3( 0, 0, 0 ) ) * BNAttenuationColor244 );
				float3 break3_g205 = temp_output_260_0;
				float temp_output_1188_0 = max( max( break3_g205.x, break3_g205.y ), break3_g205.z );
				float smoothstepResult444 = smoothstep( ( temp_output_371_0 - 0.009 ) , temp_output_371_0 , temp_output_1188_0);
				float4 lerpResult1619 = lerp( _ShadowColor , temp_output_1885_0 , saturate( smoothstepResult444 ));
				float4 temp_cast_7 = (ShadowAtten1412).xxxx;
				float4 ShadowAtten1415 = ( 1.0 == _UseShadows ? temp_cast_7 : float4( 1, 1, 1, 1 ) );
				#ifdef _USEDIFFUSEWARPASOVERLAY_ON
				float4 staticSwitch1470 = saturate( ( ShadowAtten1415 + BNLightWarpVector250 + ( _DiffuseWarpBrightnessOffset * float4( -1,0,0,0 ) * ( 1.0 - ShadowAtten1415 ) ) ) );
				#else
				float4 staticSwitch1470 = ShadowAtten1415;
				#endif
				float4 lerpResult1626 = lerp( _ShadowColor , lerpResult1619 , staticSwitch1470);
				float2 appendResult356 = (float2(( _LightRampOffset + temp_output_1188_0 ) , 0.0));
				float2 temp_cast_9 = (0.01).xx;
				float2 temp_cast_10 = (0.98).xx;
				float2 clampResult358 = clamp( appendResult356 , temp_cast_9 , temp_cast_10 );
				float4 lerpResult1617 = lerp( tex2D( _LightRampTexture, float2( 0.02,0 ) ) , ( tex2D( _LightRampTexture, clampResult358 ) * temp_output_1885_0 ) , staticSwitch1470);
				float In1331 = pow( saturate( ( temp_output_1188_0 + ( _DiffusePosterizeOffset * -1.0 ) ) ) , _DiffusePosterizePower );
				float Steps1331 = round( _DiffusePosterizeSteps );
				float localPosterize1331 = Posterize1331( In1331 , Steps1331 );
				float4 lerpResult1629 = lerp( _ShadowColor , temp_output_1885_0 , localPosterize1331);
				float4 lerpResult1628 = lerp( _ShadowColor , lerpResult1629 , staticSwitch1470);
				#if defined( _USELIGHTRAMP_STEP )
				float4 staticSwitch372 = lerpResult1626;
				#elif defined( _USELIGHTRAMP_DIFFUSERAMP )
				float4 staticSwitch372 = lerpResult1617;
				#elif defined( _USELIGHTRAMP_POSTERIZE )
				float4 staticSwitch372 = lerpResult1628;
				#else
				float4 staticSwitch372 = lerpResult1626;
				#endif
				float3 WorldPosition1181 = PositionWS;
				float3 WorldNormal1181 = BNCurrentNormal1538;
				float3 temp_cast_13 = (BNLightWarpVector250).xxx;
				float3 LightWrapVector1181 = temp_cast_13;
				float temp_output_1203_0 = ( 1.0 - (  (2.0 + ( _AdditionalLightsAmount - 0.0 ) * ( 2.9 - 2.0 ) / ( 1.0 - 0.0 ) ) + -2.0 ) );
				float temp_output_1241_0 = ( ( temp_output_1203_0 * _AdditionalLightsFaloff ) - 0.005 );
				float SMin1181 = temp_output_1241_0;
				float SMax1181 = temp_output_1203_0;
				float Faloff1181 = 0.0;
				half2 LightmapUV1_g61147 = (input.ase_texcoord3.zw*(unity_LightmapST).xy + (unity_LightmapST).zw);
				half4 localCalculateShadowMask1_g61147 = CalculateShadowMask1_g61147( LightmapUV1_g61147 );
				float4 Shadowmask1839 = localCalculateShadowMask1_g61147;
				float4 shadowmask1181 = Shadowmask1839;
				float2 ScreenUV1181 = (ScreenPosNorm).xy;
				float3 localAdditionalLight1181 = AdditionalLight( WorldPosition1181 , WorldNormal1181 , LightWrapVector1181 , SMin1181 , SMax1181 , Faloff1181 , shadowmask1181 , ScreenUV1181 );
				#ifdef _USEADDITIONALLIGHTSDIFFUSE_ON
				float3 staticSwitch1143 = localAdditionalLight1181;
				#else
				float3 staticSwitch1143 = float3( 0,0,0 );
				#endif
				float3 AdditionalLightsDiffuse1144 = staticSwitch1143;
				float4 BNDiffuse391 = ( staticSwitch372 + float4( AdditionalLightsDiffuse1144 , 0.0 ) );
				#if defined( _OVERLAYMODE_NONE )
				float4 staticSwitch1266 = BNDiffuse391;
				#elif defined( _OVERLAYMODE_HAFTONE )
				float4 staticSwitch1266 = BNDiffuse391;
				#elif defined( _OVERLAYMODE_HATCHING )
				float4 staticSwitch1266 = BNDiffuse391;
				#else
				float4 staticSwitch1266 = BNDiffuse391;
				#endif
				float4 tex2DNode362 = tex2D( _MainTex, uv_OcclusionMap );
				float lerpResult1655 = lerp( 1.0 , tex2D( _OcclusionMap, uv_OcclusionMap ).r , _OcclusionStrength);
				float4 appendResult1656 = (float4(lerpResult1655 , lerpResult1655 , lerpResult1655 , 1.0));
				float4 MainTexture364 = ( _Color * tex2DNode362 * appendResult1656 );
				float3 bakedGI276 = ASEIndirectDiffuse( input, BNCurrentNormal1538, PositionWS, ViewDirWS );
				Light ase_mainLight = GetMainLight( ShadowCoord );
				MixRealtimeAndBakedGI( ase_mainLight, BNCurrentNormal1538, bakedGI276, half4( 0, 0, 0, 0 ) );
				float IndirectLightStrength1221 = _IndirectLightStrength;
				float3 lerpResult692 = lerp( float3( 0,0,0 ) , bakedGI276 , IndirectLightStrength1221);
				float4 IndirectDiffuseLight1269 = ( MainTexture364 * float4( lerpResult692 , 0.0 ) );
				float4 BNFinalDiffuse239 = ( ( staticSwitch1266 * MainTexture364 ) + IndirectDiffuseLight1269 );
				float4 BNDiffuseNoAdditionalLights1554 = staticSwitch372;
				float4 lerpResult1462 = lerp( ase_lightColor , BNDiffuseNoAdditionalLights1554 , _HalftoneToonAffect);
				float4 temp_output_1225_0 = ( ( lerpResult1462 + float4( AdditionalLightsDiffuse1144 , 0.0 ) ) * MainTexture364 );
				float4 lerpResult1784 = lerp( temp_output_1225_0 , _HalftoneColor , _HalftoneColor.a);
				float3 break3_g210 = AdditionalLightsDiffuse1144;
				#ifdef _DIFFUSEWARPAFFECTHALFTONE_ON
				float4 staticSwitch1498 = ( float4( temp_output_260_0 , 0.0 ) * ShadowAtten1415 );
				#else
				float4 staticSwitch1498 = ( float4( max( BNNDotL233, float3( 0, 0, 0 ) ) , 0.0 ) * float4( BNAttenuationColor244 , 0.0 ) * ShadowAtten1415 );
				#endif
				float4 CompleteDiffuseLight965 = ( max( max( break3_g210.x, break3_g210.y ), break3_g210.z ) + staticSwitch1498 );
				float3 break3_g209 = CompleteDiffuseLight965.xyz;
				float smoothstepResult525 = smoothstep( _HalftoneEdgeOffset , ( _HalftoneEdgeOffset + _HalftoneSmoothness ) , max( max( break3_g209.x, break3_g209.y ), break3_g209.z ));
				float2 temp_cast_22 = (_OverlayUVTilling).xx;
				float mulTime1287 = _TimeParameters.x * _UVScrollSpeed;
				float temp_output_1293_0 = radians( _UVSrcrollAngle );
				float2 appendResult1294 = (float2(cos( temp_output_1293_0 ) , sin( temp_output_1293_0 )));
				float2 temp_output_1288_0 = ( mulTime1287 * appendResult1294 );
				float2 texCoord478 = input.ase_texcoord3.xy * temp_cast_22 + temp_output_1288_0;
				float2 appendResult1849 = (float2(( _OverlayUVTilling * ( _ScaledScreenParams.x / _ScaledScreenParams.y ) ) , _OverlayUVTilling));
				float4 unityObjectToClipPos2_g203 = TransformWorldToHClip( TransformObjectToWorld( ( input.ase_texcoord4.xyz ).xyz ) );
				float4 computeScreenPos3_g203 = ComputeScreenPos( unityObjectToClipPos2_g203 );
				float temp_output_29_0_g203 = ( _OverlayUVTilling / 4.0 );
				float4 unityObjectToClipPos13_g203 = TransformWorldToHClip( TransformObjectToWorld( ( float3( 0, 0, 0 ) ).xyz ) );
				float4 computeScreenPos17_g203 = ComputeScreenPos( unityObjectToClipPos13_g203 );
				float4 transform20_g203 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float4 break22_g203 = ( ( ( ( computeScreenPos3_g203 / (computeScreenPos3_g203).w ) * temp_output_29_0_g203 ) - ( temp_output_29_0_g203 * ( computeScreenPos17_g203 / (computeScreenPos17_g203).w ) ) ) * distance( ( float4( _WorldSpaceCameraPos , 0.0 ) - transform20_g203 ) , float4( 0,0,0,0 ) ) );
				float4 break26_g203 = _ScaledScreenParams;
				float4 appendResult24_g203 = (float4(( break22_g203.x * ( break26_g203.x / break26_g203.y ) ) , break22_g203.y , break22_g203.z , break22_g203.w));
				#ifdef _USEADAPTIVESCREENUVS_ON
				float4 staticSwitch1661 = appendResult24_g203;
				#else
				float4 staticSwitch1661 = ( float4( appendResult1849, 0.0 , 0.0 ) * ase_grabScreenPosNorm );
				#endif
				float mulTime1278 = _TimeParameters.x * _UVAnimationSpeed;
				float temp_output_1281_0 = ( floor( ( mulTime1278 % 2.0 ) ) * 0.5 );
				#if defined( _OVERLAYMODE_NONE )
				float staticSwitch1277 = 0.0;
				#elif defined( _OVERLAYMODE_HAFTONE )
				float staticSwitch1277 = temp_output_1281_0;
				#elif defined( _OVERLAYMODE_HATCHING )
				float staticSwitch1277 = temp_output_1281_0;
				#else
				float staticSwitch1277 = 0.0;
				#endif
				float2 appendResult1803 = (float2(staticSwitch1277 , staticSwitch1277));
				#ifdef _USESCREENUVS_ON
				float4 staticSwitch488 = ( staticSwitch1661 + float4( appendResult1803, 0.0 , 0.0 ) + float4( temp_output_1288_0, 0.0 , 0.0 ) );
				#else
				float4 staticSwitch488 = float4( texCoord478, 0.0 , 0.0 );
				#endif
				float cos1047 = cos( radians( _OverlayRotation ) );
				float sin1047 = sin( radians( _OverlayRotation ) );
				float2 rotator1047 = mul( staticSwitch488.xy - float2( 0.5,0.5 ) , float2x2( cos1047 , -sin1047 , sin1047 , cos1047 )) + float2( 0.5,0.5 );
				float2 OverlayUVs1051 = rotator1047;
				float smoothstepResult1045 = smoothstep( ( ( _ShapeSmoothness * -0.5 ) + 0.5 ) , ( ( _ShapeSmoothness * 0.5 ) + 0.5 ) , ( 0.1 / ( ( _HalftoneThreshold / smoothstepResult525 ) *  (0.0 + ( tex2D( _HalftoneTexture, OverlayUVs1051 ).r - 0.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.0 ) ) ) ));
				float4 lerpResult1774 = lerp( lerpResult1784 , temp_output_1225_0 , smoothstepResult1045);
				float4 Halftone1022 = ( lerpResult1774 + IndirectDiffuseLight1269 );
				#if defined( _OVERLAYMODE_NONE )
				float4 staticSwitch1231 = BNFinalDiffuse239;
				#elif defined( _OVERLAYMODE_HAFTONE )
				float4 staticSwitch1231 = Halftone1022;
				#elif defined( _OVERLAYMODE_HATCHING )
				float4 staticSwitch1231 = BNFinalDiffuse239;
				#else
				float4 staticSwitch1231 = BNFinalDiffuse239;
				#endif
				float3 WorldPosition1573 = PositionWS;
				float3 WorldNormal1573 = BNCurrentNormal1538;
				float3 WorldView1573 = ViewDirWS;
				float3 SpecColor1573 = float3( 1, 1, 1 );
				float Smoothness638 = ( tex2D( _SpecGlossMap, uv_OcclusionMap ).r * _Glossiness );
				float Smoothness1573 = ( Smoothness638 * ( 2.0 - _AdditionalLightsSmoothnessMultiplier ) );
				float temp_output_588_0 = round( _SpecularPosterizeSteps );
				float Steps1573 = temp_output_588_0;
				float SpecFaloff1573 = _SpecularFaloff;
				float3 localAdditionalLightsSpecularMy1573 = AdditionalLightsSpecularMy( WorldPosition1573 , WorldNormal1573 , WorldView1573 , SpecColor1573 , Smoothness1573 , Steps1573 , SpecFaloff1573 );
				float3 normalizeResult222 = normalize( _MainLightPosition.xyz );
				float3 normalizeResult238 = normalize( ( normalizeResult222 + ViewDirWS ) );
				float3 BNHalfDirection265 = normalizeResult238;
				float dotResult252 = dot( BNHalfDirection265 , BNCurrentNormal1538 );
				float IN1578 = ( pow( max( dotResult252, 0.0 ) , ( exp2( ( ( Smoothness638 * 10.0 * ( 2.0 - _SmoothnessMultiplier ) ) + -2.0 ) ) * 2.0 ) ) * ( _SmoothnessMultiplier == 0.0 ? 0.0 : 1.0 ) );
				float SpecFaloff1578 = _SpecularFaloff;
				float Steps1578 = temp_output_588_0;
				float localFaloffPosterize1578 = FaloffPosterize( IN1578 , SpecFaloff1578 , Steps1578 );
				float2 temp_cast_30 = (_SpecularMaskScale).xx;
				float2 texCoord1123 = input.ase_texcoord3.xy * temp_cast_30 + float2( 0,0 );
				float2 appendResult1842 = (float2(( _SpecularMaskScale * ( _ScaledScreenParams.x / _ScaledScreenParams.y ) ) , _SpecularMaskScale));
				float4 unityObjectToClipPos2_g202 = TransformWorldToHClip( TransformObjectToWorld( ( input.ase_texcoord4.xyz ).xyz ) );
				float4 computeScreenPos3_g202 = ComputeScreenPos( unityObjectToClipPos2_g202 );
				float temp_output_29_0_g202 = ( _SpecularMaskScale / 4.0 );
				float4 unityObjectToClipPos13_g202 = TransformWorldToHClip( TransformObjectToWorld( ( float3( 0, 0, 0 ) ).xyz ) );
				float4 computeScreenPos17_g202 = ComputeScreenPos( unityObjectToClipPos13_g202 );
				float4 transform20_g202 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float4 break22_g202 = ( ( ( ( computeScreenPos3_g202 / (computeScreenPos3_g202).w ) * temp_output_29_0_g202 ) - ( temp_output_29_0_g202 * ( computeScreenPos17_g202 / (computeScreenPos17_g202).w ) ) ) * distance( ( float4( _WorldSpaceCameraPos , 0.0 ) - transform20_g202 ) , float4( 0,0,0,0 ) ) );
				float4 break26_g202 = _ScaledScreenParams;
				float4 appendResult24_g202 = (float4(( break22_g202.x * ( break26_g202.x / break26_g202.y ) ) , break22_g202.y , break22_g202.z , break22_g202.w));
				#ifdef _USEADAPTIVEUVSSPECULAR_ON
				float4 staticSwitch1665 = appendResult24_g202;
				#else
				float4 staticSwitch1665 = ( float4( appendResult1842, 0.0 , 0.0 ) * ase_grabScreenPosNorm );
				#endif
				float mulTime1814 = _TimeParameters.x * _UVAnimationSpeedSpec;
				float temp_output_1817_0 = ( floor( ( mulTime1814 % 2.0 ) ) * 0.5 );
				#if defined( _OVERLAYMODE1_NONE )
				float staticSwitch1812 = temp_output_1817_0;
				#elif defined( _OVERLAYMODE1_HAFTONE )
				float staticSwitch1812 = temp_output_1817_0;
				#elif defined( _OVERLAYMODE1_HATCHING )
				float staticSwitch1812 = temp_output_1817_0;
				#else
				float staticSwitch1812 = temp_output_1817_0;
				#endif
				float2 appendResult1818 = (float2(staticSwitch1812 , staticSwitch1812));
				#ifdef _USESCREENUVSSPECULAR_ON
				float4 staticSwitch1132 = ( staticSwitch1665 + float4( appendResult1818, 0.0 , 0.0 ) );
				#else
				float4 staticSwitch1132 = float4( texCoord1123, 0.0 , 0.0 );
				#endif
				float mulTime1811 = _TimeParameters.x * _UVScrollSpeedSpec;
				float temp_output_1807_0 = radians( _UVSrcrollAngleSpec );
				float2 appendResult1808 = (float2(cos( temp_output_1807_0 ) , sin( temp_output_1807_0 )));
				float cos1124 = cos( radians( _SpecularMaskRotation ) );
				float sin1124 = sin( radians( _SpecularMaskRotation ) );
				float2 rotator1124 = mul( ( staticSwitch1132 + float4( ( mulTime1811 * appendResult1808 ), 0.0 , 0.0 ) ).xy - float2( 0.5,0.5 ) , float2x2( cos1124 , -sin1124 , sin1124 , cos1124 )) + float2( 0.5,0.5 );
				float2 SpecularUVs1125 = rotator1124;
				float4 tex2DNode441 = tex2D( _SpecularMaskTexture, SpecularUVs1125 );
				float temp_output_1634_0 = ( 1.0 - _HaltonePatternSize );
				float smoothstepResult1557 = smoothstep( ( temp_output_1634_0 - 0.3 ) , temp_output_1634_0 , tex2DNode441.r);
				float lerpResult446 = lerp( 1.0 , (( _StepHalftoneTexture )?( smoothstepResult1557 ):( tex2DNode441.r )) , _SpecularMaskStrength);
				#ifdef _INVERSEMASK_ON
				float staticSwitch1681 = ( 1.0 - lerpResult446 );
				#else
				float staticSwitch1681 = lerpResult446;
				#endif
				float SpecularMask902 = ( _UseSpecularMask == 1.0 ? staticSwitch1681 : 1.0 );
				float4 SpecularColor1388 = _SpecColor;
				#ifdef _USESPECULAR_ON
				float4 staticSwitch627 = max( ( float4( ( ( localAdditionalLightsSpecularMy1573 * _AdditionalLightsIntesity * ( _AdditionalLightsSmoothnessMultiplier == 0.0 ? 0.0 : 1.0 ) ) + ( ase_lightColor.rgb * _MainLightIntesity * localFaloffPosterize1578 ) ) , 0.0 ) * SpecularMask902 * Smoothness638 * SpecularColor1388 ), float4( 0,0,0,0 ) );
				#else
				float4 staticSwitch627 = float4( 0,0,0,0 );
				#endif
				float4 BNspecularFinalColor243 = staticSwitch627;
				float4 lerpResult1216 = lerp( _HalftoneColor , lerpResult1462 , smoothstepResult1045);
				float4 HalftoneDiffuseShadowMask1236 = lerpResult1216;
				#if defined( _OVERLAYMODE_NONE )
				float4 staticSwitch1237 = BNDiffuse391;
				#elif defined( _OVERLAYMODE_HAFTONE )
				float4 staticSwitch1237 = HalftoneDiffuseShadowMask1236;
				#elif defined( _OVERLAYMODE_HATCHING )
				float4 staticSwitch1237 = BNDiffuse391;
				#else
				float4 staticSwitch1237 = BNDiffuse391;
				#endif
				float grayscale1797 = dot( staticSwitch1237.rgb, float3( 0.299, 0.587, 0.114 ) );
				float lerpResult695 = lerp( 1.0 , grayscale1797 , _SpecularShadowMask);
				half3 reflectVector618 = reflect( -ViewDirWS, BNCurrentNormal1538 );
				float3 indirectSpecular618 = GlossyEnvironmentReflection( reflectVector618, PositionWS, 1.0 - Smoothness638, 0.75, ScreenPosNorm.xy );
				#ifdef _USEENVIRONMENTREFLETION_ON
				float3 staticSwitch621 = ( indirectSpecular618 * _Strength * Smoothness638 );
				#else
				float3 staticSwitch621 = float3( 0,0,0 );
				#endif
				float3 IndirectSpecular1364 = staticSwitch621;
				float4 BNBlinnPhongLightning274 = ( staticSwitch1231 + ( BNspecularFinalColor243 * lerpResult695 ) + float4( IndirectSpecular1364 , 0.0 ) );
				float grayscale1648 = Luminance( BNDiffuseNoAdditionalLights1554.rgb );
				float4 lerpResult1650 = lerp( _RimShadowColor , _RimColor , grayscale1648);
				#if defined( _RIMSPLITCOLOR_NOSPLIT )
				float4 staticSwitch1646 = _RimColor;
				#elif defined( _RIMSPLITCOLOR_MULTIPLYWITHDIFFUSE )
				float4 staticSwitch1646 = ( _RimColor * BNDiffuseNoAdditionalLights1554 );
				#elif defined( _RIMSPLITCOLOR_USESECONDCOLOR )
				float4 staticSwitch1646 = lerpResult1650;
				#else
				float4 staticSwitch1646 = _RimColor;
				#endif
				float4 RimColor1642 = staticSwitch1646;
				float fresnelNdotV454 = dot( normalize( BNCurrentNormal1538 ), ViewDirWS );
				float fresnelNode454 = ( 0.0 + _RimThickness * pow( max( 1.0 - fresnelNdotV454 , 0.0001 ), _RimPower ) );
				float smoothstepResult462 = smoothstep( ( ( 1.0 - _RimSmoothness ) - 0.5 ) , 0.5 , fresnelNode454);
				float FresnelValue738 = smoothstepResult462;
				float dotResult704 = dot( ViewDirWS , _MainLightPosition.xyz );
				float smoothstepResult726 = smoothstep( _BacklightHardness , 0.5 , saturate( ( 1.0 - ( dotResult704 - ( ( _BacklightAmount * 2.0 ) + -2.0 ) ) ) ));
				float dotResult749 = dot( BNCurrentNormal1538 , _MainLightPosition.xyz );
				float temp_output_766_0 = ( 1.0 - ( _SideShineAmount - -0.3 ) );
				float dotResult745 = dot( ( ViewDirWS * -1.0 ) , _MainLightPosition.xyz );
				float clampResult753 = clamp( ( ( ( dotResult749 - temp_output_766_0 ) * 4.0 ) + ( dotResult745 - temp_output_766_0 ) ) , 0.0 , 1.1 );
				float smoothstepResult759 = smoothstep( _SideShineHardness , 0.5 , ( clampResult753 * FresnelValue738 ));
				#ifdef _USEBACKLIGHT_ON
				float staticSwitch732 = ( ( _BacklightIntensity * ( FresnelValue738 * smoothstepResult726 ) ) + ( _SideShineIntensity * smoothstepResult759 ) );
				#else
				float staticSwitch732 = FresnelValue738;
				#endif
				#ifdef _USERIMLIGHT_ON
				float staticSwitch464 = staticSwitch732;
				#else
				float staticSwitch464 = 0.0;
				#endif
				float RimLight460 = staticSwitch464;
				float4 lerpResult1635 = lerp( BNBlinnPhongLightning274 , RimColor1642 , RimLight460);
				float4 Emission680 = ( _UseEmission == 1.0 ? ( tex2D( _EmissionMap, uv_OcclusionMap ) * _EmissionColor ) : float4( 0,0,0,0 ) );
				float4 temp_output_282_0 = ( lerpResult1635 + Emission680 );
				float3 IndirectHatching1466 = lerpResult692;
				float3 lerpResult1468 = lerp( float3( 0,0,0 ) , IndirectHatching1466 , _IndirectLightAffectOnHatch);
				float4 temp_output_1270_0 = ( _HatchingColor + float4( lerpResult1468 , 0.0 ) );
				float4 lerpResult1264 = lerp( BNBlinnPhongLightning274 , temp_output_1270_0 , _HatchingColor.a);
				float2 _uv476 = OverlayUVs1051;
				float3 break3_g204 = ( RimLight460 + BNDiffuse391 + Emission680 + BNspecularFinalColor243 ).rgb;
				float temp_output_1064_0 =  (( _Darken * -2.0 ) + ( max( max( break3_g204.x, break3_g204.y ), break3_g204.z ) - 0.0 ) * ( ( ( _Lighten * 2.0 ) + 1.0 ) - ( _Darken * -2.0 ) ) / ( 1.0 - 0.0 ) );
				float color476 = temp_output_1064_0;
				sampler2D _Hatch0476 = _Hatch1;
				sampler2D _Hatch1476 = _Hatch2;
				float3 localHatching476 = Hatching476( _uv476 , color476 , _Hatch0476 , _Hatch1476 );
				float2 _uv491 = OverlayUVs1051;
				float _intensity491 = temp_output_1064_0;
				sampler2D _Hatch0491 = _Hatch1;
				sampler2D _Hatch1491 = _Hatch2;
				float _dist491 = distance( _WorldSpaceCameraPos , PositionWS );
				float _MaxScaleDependingOnCamera491 = _MaxScaleDependingOnCamera;
				float3 localHatchingConstantScale491 = HatchingConstantScale491( _uv491 , _intensity491 , _Hatch0491 , _Hatch1491 , _dist491 , _MaxScaleDependingOnCamera491 );
				#ifdef _USEHATCHINGCONSTANTSCALE_ON
				float3 staticSwitch490 = localHatchingConstantScale491;
				#else
				float3 staticSwitch490 = localHatching476;
				#endif
				float3 Hatching1025 = saturate( staticSwitch490 );
				float4 lerpResult1260 = lerp( lerpResult1264 , BNBlinnPhongLightning274 , Hatching1025.x);
				float2 appendResult1857 = (float2(( _PaperTilling * ( _ScaledScreenParams.x / _ScaledScreenParams.y ) ) , _PaperTilling));
				float4 tex2DNode1795 = tex2D( _PaperTexture, ( ase_grabScreenPosNorm * float4( appendResult1857, 0.0 , 0.0 ) ).xy );
				float4 lerpResult1254 = lerp( temp_output_1270_0 , tex2DNode1795 , Hatching1025.x);
				float4 lerpResult1257 = lerp( lerpResult1254 , tex2DNode1795 , ( 1.0 - _HatchingColor.a ));
				#ifdef _USEPURESKETCH_ON
				float4 staticSwitch1258 = lerpResult1257;
				#else
				float4 staticSwitch1258 = lerpResult1260;
				#endif
				float4 lerpResult1637 = lerp( staticSwitch1258 , RimColor1642 , RimLight460);
				#if defined( _OVERLAYMODE_NONE )
				float4 staticSwitch1024 = temp_output_282_0;
				#elif defined( _OVERLAYMODE_HAFTONE )
				float4 staticSwitch1024 = temp_output_282_0;
				#elif defined( _OVERLAYMODE_HATCHING )
				float4 staticSwitch1024 = ( lerpResult1637 + Emission680 );
				#else
				float4 staticSwitch1024 = temp_output_282_0;
				#endif
				
				float temp_output_673_0 = ( MainTexture364.a * 1.0 );
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = staticSwitch1024.rgb;
				float3 Normal = float3(0, 0, 1);
				float Alpha = temp_output_673_0;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
					float AlphaClipThresholdShadow = 0.5;
				#endif


				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_CHANGES_WORLD_POS)
					ShadowCoord = TransformWorldToShadowCoord( PositionWS );
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
				inputData.normalWS = NormalWS;
				inputData.viewDirectionWS = ViewDirWS;

				#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT) && defined(UNLIT_DEFAULT_SSAO)
					float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
					AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
					Color.rgb *= aoFactor.directAmbientOcclusion;
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.positionWSAndFogFactor.w);
				#endif

				#if defined(_DBUFFER) && defined(UNLIT_DEFAULT_DECAL_BLENDING)
					ApplyDecalToBaseColor(input.positionCS, Color);
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						Color.rgb = MixFogColor(Color.rgb, half3(0,0,0), inputData.fogCoord);
					#else
						Color.rgb = MixFog(Color.rgb, inputData.fogCoord);
					#endif
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					#if ( UNITY_VERSION >= 60020000 )
					outRenderingLayers = EncodeMeshRenderingLayer();
					#else
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
					#endif
				#endif

				#if defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, OutputAlpha( Alpha, isTransparent ) );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off
			ColorMask 0

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170004


			#pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW // @diogo: removed _vertex for POM node

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_SHADOWCASTER

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RimShadowColor;
			float4 _HalftoneColor;
			float4 _RimColor;
			float4 _Color;
			float4 _OcclusionMap_ST;
			float4 _OutlineColor;
			float4 _ShadowColor;
			float4 _HatchingColor;
			float4 _EmissionColor;
			float4 _SpecColor;
			float _SmoothnessMultiplier;
			float _MainLightIntesity;
			float _UseSpecularMask;
			float _AdditionalLightsIntesity;
			float _StepHalftoneTexture;
			float _SpecularMaskScale;
			float _SpecularFaloff;
			float _UVAnimationSpeedSpec;
			float _UVScrollSpeedSpec;
			float _UVSrcrollAngleSpec;
			float _SpecularMaskRotation;
			float _HaltonePatternSize;
			float _SpecularMaskStrength;
			float _RampDiffuseTextureLoaded;
			float _Strength;
			float _Lighten;
			float _Darken;
			float _IndirectLightAffectOnHatch;
			float _UseEmission;
			float _SideShineAmount;
			float _SideShineHardness;
			float _SpecularShadowMask;
			float _SideShineIntensity;
			float _BacklightHardness;
			float _BacklightIntensity;
			float _RimPower;
			float _RimThickness;
			float _RimSmoothness;
			float _SpecularPosterizeSteps;
			float _BacklightAmount;
			float _AdditionalLightsSmoothnessMultiplier;
			float _UVAnimationSpeed;
			float _OverlayRotation;
			float _WarpStrength;
			float _WarpTextureRotation;
			float _UVAnimationSpeedWarp;
			float _UVSrcrollAngleWarp;
			float _UVScrollSpeedWarp;
			float _WarpTextureScale;
			float _UseDiffuseWarp;
			float _StepOffset;
			float _OcclusionStrength;
			float _OutlineTextureStrength;
			float _Thicnkess;
			float _OutlineNoiseIntesity;
			float _OutlineNoiseScale;
			float _OutlineDynamicSpeed;
			float _AdaptiveThicnkess;
			float _NormalMapStrength;
			float _Glossiness;
			float _UseShadows;
			float _LightRampOffset;
			float _MaxScaleDependingOnCamera;
			float _UVSrcrollAngle;
			float _UVScrollSpeed;
			float _OverlayUVTilling;
			float _HalftoneSmoothness;
			float _HalftoneEdgeOffset;
			float _HalftoneThreshold;
			float _ShapeSmoothness;
			float _HalftoneToonAffect;
			float _IndirectLightStrength;
			float _AdditionalLightsFaloff;
			float _AdditionalLightsAmount;
			float _DiffusePosterizeSteps;
			float _DiffusePosterizePower;
			float _DiffusePosterizeOffset;
			float _DiffuseWarpBrightnessOffset;
			float _PaperTilling;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _MainTex;
			sampler2D _OcclusionMap;


			float3 _LightDirection;
			float3 _LightPosition;

			
			PackedVaryings VertexFunction( Attributes input )
			{
				PackedVaryings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

				output.ase_texcoord.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );
				half3 normalWS = TransformObjectToWorldDir(input.normalOS);

				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDirectionWS = normalize(_LightPosition - positionWS);
				#else
					float3 lightDirectionWS = _LightDirection;
				#endif

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

				#if UNITY_REVERSED_Z
					positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#else
					positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#endif

				output.positionCS = ASE_ADJUST_CLIP_POSITION( positionCS );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float2 uv_OcclusionMap = input.ase_texcoord.xy * _OcclusionMap_ST.xy + _OcclusionMap_ST.zw;
				float4 tex2DNode362 = tex2D( _MainTex, uv_OcclusionMap );
				float lerpResult1655 = lerp( 1.0 , tex2D( _OcclusionMap, uv_OcclusionMap ).r , _OcclusionStrength);
				float4 appendResult1656 = (float4(lerpResult1655 , lerpResult1655 , lerpResult1655 , 1.0));
				float4 MainTexture364 = ( _Color * tex2DNode362 * appendResult1656 );
				float temp_output_673_0 = ( MainTexture364.a * 1.0 );
				

				float Alpha = temp_output_673_0;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
					float AlphaClipThresholdShadow = 0.5;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					#if defined( _ALPHATEST_SHADOW_ON )
						AlphaDiscard( Alpha, AlphaClipThresholdShadow );
					#else
						AlphaDiscard( Alpha, AlphaClipThreshold );
					#endif
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170004


			#pragma vertex vert
			#pragma fragment frag

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RimShadowColor;
			float4 _HalftoneColor;
			float4 _RimColor;
			float4 _Color;
			float4 _OcclusionMap_ST;
			float4 _OutlineColor;
			float4 _ShadowColor;
			float4 _HatchingColor;
			float4 _EmissionColor;
			float4 _SpecColor;
			float _SmoothnessMultiplier;
			float _MainLightIntesity;
			float _UseSpecularMask;
			float _AdditionalLightsIntesity;
			float _StepHalftoneTexture;
			float _SpecularMaskScale;
			float _SpecularFaloff;
			float _UVAnimationSpeedSpec;
			float _UVScrollSpeedSpec;
			float _UVSrcrollAngleSpec;
			float _SpecularMaskRotation;
			float _HaltonePatternSize;
			float _SpecularMaskStrength;
			float _RampDiffuseTextureLoaded;
			float _Strength;
			float _Lighten;
			float _Darken;
			float _IndirectLightAffectOnHatch;
			float _UseEmission;
			float _SideShineAmount;
			float _SideShineHardness;
			float _SpecularShadowMask;
			float _SideShineIntensity;
			float _BacklightHardness;
			float _BacklightIntensity;
			float _RimPower;
			float _RimThickness;
			float _RimSmoothness;
			float _SpecularPosterizeSteps;
			float _BacklightAmount;
			float _AdditionalLightsSmoothnessMultiplier;
			float _UVAnimationSpeed;
			float _OverlayRotation;
			float _WarpStrength;
			float _WarpTextureRotation;
			float _UVAnimationSpeedWarp;
			float _UVSrcrollAngleWarp;
			float _UVScrollSpeedWarp;
			float _WarpTextureScale;
			float _UseDiffuseWarp;
			float _StepOffset;
			float _OcclusionStrength;
			float _OutlineTextureStrength;
			float _Thicnkess;
			float _OutlineNoiseIntesity;
			float _OutlineNoiseScale;
			float _OutlineDynamicSpeed;
			float _AdaptiveThicnkess;
			float _NormalMapStrength;
			float _Glossiness;
			float _UseShadows;
			float _LightRampOffset;
			float _MaxScaleDependingOnCamera;
			float _UVSrcrollAngle;
			float _UVScrollSpeed;
			float _OverlayUVTilling;
			float _HalftoneSmoothness;
			float _HalftoneEdgeOffset;
			float _HalftoneThreshold;
			float _ShapeSmoothness;
			float _HalftoneToonAffect;
			float _IndirectLightStrength;
			float _AdditionalLightsFaloff;
			float _AdditionalLightsAmount;
			float _DiffusePosterizeSteps;
			float _DiffusePosterizePower;
			float _DiffusePosterizeOffset;
			float _DiffuseWarpBrightnessOffset;
			float _PaperTilling;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _MainTex;
			sampler2D _OcclusionMap;


			float3 _LightDirection;
			float3 _LightPosition;

			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_texcoord.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float2 uv_OcclusionMap = input.ase_texcoord.xy * _OcclusionMap_ST.xy + _OcclusionMap_ST.zw;
				float4 tex2DNode362 = tex2D( _MainTex, uv_OcclusionMap );
				float lerpResult1655 = lerp( 1.0 , tex2D( _OcclusionMap, uv_OcclusionMap ).r , _OcclusionStrength);
				float4 appendResult1656 = (float4(lerpResult1655 , lerpResult1655 , lerpResult1655 , 1.0));
				float4 MainTexture364 = ( _Color * tex2DNode362 * appendResult1656 );
				float temp_output_673_0 = ( MainTexture364.a * 1.0 );
				

				float Alpha = temp_output_673_0;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "SceneSelectionPass"
			Tags { "LightMode"="SceneSelectionPass" }

			Cull Off
			AlphaToMask Off

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170004


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RimShadowColor;
			float4 _HalftoneColor;
			float4 _RimColor;
			float4 _Color;
			float4 _OcclusionMap_ST;
			float4 _OutlineColor;
			float4 _ShadowColor;
			float4 _HatchingColor;
			float4 _EmissionColor;
			float4 _SpecColor;
			float _SmoothnessMultiplier;
			float _MainLightIntesity;
			float _UseSpecularMask;
			float _AdditionalLightsIntesity;
			float _StepHalftoneTexture;
			float _SpecularMaskScale;
			float _SpecularFaloff;
			float _UVAnimationSpeedSpec;
			float _UVScrollSpeedSpec;
			float _UVSrcrollAngleSpec;
			float _SpecularMaskRotation;
			float _HaltonePatternSize;
			float _SpecularMaskStrength;
			float _RampDiffuseTextureLoaded;
			float _Strength;
			float _Lighten;
			float _Darken;
			float _IndirectLightAffectOnHatch;
			float _UseEmission;
			float _SideShineAmount;
			float _SideShineHardness;
			float _SpecularShadowMask;
			float _SideShineIntensity;
			float _BacklightHardness;
			float _BacklightIntensity;
			float _RimPower;
			float _RimThickness;
			float _RimSmoothness;
			float _SpecularPosterizeSteps;
			float _BacklightAmount;
			float _AdditionalLightsSmoothnessMultiplier;
			float _UVAnimationSpeed;
			float _OverlayRotation;
			float _WarpStrength;
			float _WarpTextureRotation;
			float _UVAnimationSpeedWarp;
			float _UVSrcrollAngleWarp;
			float _UVScrollSpeedWarp;
			float _WarpTextureScale;
			float _UseDiffuseWarp;
			float _StepOffset;
			float _OcclusionStrength;
			float _OutlineTextureStrength;
			float _Thicnkess;
			float _OutlineNoiseIntesity;
			float _OutlineNoiseScale;
			float _OutlineDynamicSpeed;
			float _AdaptiveThicnkess;
			float _NormalMapStrength;
			float _Glossiness;
			float _UseShadows;
			float _LightRampOffset;
			float _MaxScaleDependingOnCamera;
			float _UVSrcrollAngle;
			float _UVScrollSpeed;
			float _OverlayUVTilling;
			float _HalftoneSmoothness;
			float _HalftoneEdgeOffset;
			float _HalftoneThreshold;
			float _ShapeSmoothness;
			float _HalftoneToonAffect;
			float _IndirectLightStrength;
			float _AdditionalLightsFaloff;
			float _AdditionalLightsAmount;
			float _DiffusePosterizeSteps;
			float _DiffusePosterizePower;
			float _DiffusePosterizeOffset;
			float _DiffuseWarpBrightnessOffset;
			float _PaperTilling;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _MainTex;
			sampler2D _OcclusionMap;


			
			int _ObjectId;
			int _PassValue;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_texcoord.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 uv_OcclusionMap = input.ase_texcoord.xy * _OcclusionMap_ST.xy + _OcclusionMap_ST.zw;
				float4 tex2DNode362 = tex2D( _MainTex, uv_OcclusionMap );
				float lerpResult1655 = lerp( 1.0 , tex2D( _OcclusionMap, uv_OcclusionMap ).r , _OcclusionStrength);
				float4 appendResult1656 = (float4(lerpResult1655 , lerpResult1655 , lerpResult1655 , 1.0));
				float4 MainTexture364 = ( _Color * tex2DNode362 * appendResult1656 );
				float temp_output_673_0 = ( MainTexture364.a * 1.0 );
				

				surfaceDescription.Alpha = temp_output_673_0;
				#if defined( _ALPHATEST_ON )
					surfaceDescription.AlphaClipThreshold = _Cutoff;
				#endif

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				return outColor;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ScenePickingPass"
			Tags { "LightMode"="Picking" }

			AlphaToMask Off

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170004


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT

			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RimShadowColor;
			float4 _HalftoneColor;
			float4 _RimColor;
			float4 _Color;
			float4 _OcclusionMap_ST;
			float4 _OutlineColor;
			float4 _ShadowColor;
			float4 _HatchingColor;
			float4 _EmissionColor;
			float4 _SpecColor;
			float _SmoothnessMultiplier;
			float _MainLightIntesity;
			float _UseSpecularMask;
			float _AdditionalLightsIntesity;
			float _StepHalftoneTexture;
			float _SpecularMaskScale;
			float _SpecularFaloff;
			float _UVAnimationSpeedSpec;
			float _UVScrollSpeedSpec;
			float _UVSrcrollAngleSpec;
			float _SpecularMaskRotation;
			float _HaltonePatternSize;
			float _SpecularMaskStrength;
			float _RampDiffuseTextureLoaded;
			float _Strength;
			float _Lighten;
			float _Darken;
			float _IndirectLightAffectOnHatch;
			float _UseEmission;
			float _SideShineAmount;
			float _SideShineHardness;
			float _SpecularShadowMask;
			float _SideShineIntensity;
			float _BacklightHardness;
			float _BacklightIntensity;
			float _RimPower;
			float _RimThickness;
			float _RimSmoothness;
			float _SpecularPosterizeSteps;
			float _BacklightAmount;
			float _AdditionalLightsSmoothnessMultiplier;
			float _UVAnimationSpeed;
			float _OverlayRotation;
			float _WarpStrength;
			float _WarpTextureRotation;
			float _UVAnimationSpeedWarp;
			float _UVSrcrollAngleWarp;
			float _UVScrollSpeedWarp;
			float _WarpTextureScale;
			float _UseDiffuseWarp;
			float _StepOffset;
			float _OcclusionStrength;
			float _OutlineTextureStrength;
			float _Thicnkess;
			float _OutlineNoiseIntesity;
			float _OutlineNoiseScale;
			float _OutlineDynamicSpeed;
			float _AdaptiveThicnkess;
			float _NormalMapStrength;
			float _Glossiness;
			float _UseShadows;
			float _LightRampOffset;
			float _MaxScaleDependingOnCamera;
			float _UVSrcrollAngle;
			float _UVScrollSpeed;
			float _OverlayUVTilling;
			float _HalftoneSmoothness;
			float _HalftoneEdgeOffset;
			float _HalftoneThreshold;
			float _ShapeSmoothness;
			float _HalftoneToonAffect;
			float _IndirectLightStrength;
			float _AdditionalLightsFaloff;
			float _AdditionalLightsAmount;
			float _DiffusePosterizeSteps;
			float _DiffusePosterizePower;
			float _DiffusePosterizeOffset;
			float _DiffuseWarpBrightnessOffset;
			float _PaperTilling;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _MainTex;
			sampler2D _OcclusionMap;


			
			float4 _SelectionID;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_texcoord.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 uv_OcclusionMap = input.ase_texcoord.xy * _OcclusionMap_ST.xy + _OcclusionMap_ST.zw;
				float4 tex2DNode362 = tex2D( _MainTex, uv_OcclusionMap );
				float lerpResult1655 = lerp( 1.0 , tex2D( _OcclusionMap, uv_OcclusionMap ).r , _OcclusionStrength);
				float4 appendResult1656 = (float4(lerpResult1655 , lerpResult1655 , lerpResult1655 , 1.0));
				float4 MainTexture364 = ( _Color * tex2DNode362 * appendResult1656 );
				float temp_output_673_0 = ( MainTexture364.a * 1.0 );
				

				surfaceDescription.Alpha = temp_output_673_0;
				#if defined( _ALPHATEST_ON )
					surfaceDescription.AlphaClipThreshold = _Cutoff;
				#endif

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				half4 outColor = 0;
				outColor = unity_SelectionID;

				return outColor;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormalsOnly" }

			ZTest LEqual
			ZWrite On

			HLSLPROGRAM

        	#define _NORMAL_DROPOFF_TS 1
        	#pragma multi_compile_instancing
        	#pragma multi_compile _ LOD_FADE_CROSSFADE
        	#define ASE_VERSION 19912
        	#define ASE_SRP_VERSION 170004


        	#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define VARYINGS_NEED_NORMAL_WS
			#define VARYINGS_NEED_TANGENT_WS

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				half3 normalWS : TEXCOORD0;
				float4 tangentWS : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RimShadowColor;
			float4 _HalftoneColor;
			float4 _RimColor;
			float4 _Color;
			float4 _OcclusionMap_ST;
			float4 _OutlineColor;
			float4 _ShadowColor;
			float4 _HatchingColor;
			float4 _EmissionColor;
			float4 _SpecColor;
			float _SmoothnessMultiplier;
			float _MainLightIntesity;
			float _UseSpecularMask;
			float _AdditionalLightsIntesity;
			float _StepHalftoneTexture;
			float _SpecularMaskScale;
			float _SpecularFaloff;
			float _UVAnimationSpeedSpec;
			float _UVScrollSpeedSpec;
			float _UVSrcrollAngleSpec;
			float _SpecularMaskRotation;
			float _HaltonePatternSize;
			float _SpecularMaskStrength;
			float _RampDiffuseTextureLoaded;
			float _Strength;
			float _Lighten;
			float _Darken;
			float _IndirectLightAffectOnHatch;
			float _UseEmission;
			float _SideShineAmount;
			float _SideShineHardness;
			float _SpecularShadowMask;
			float _SideShineIntensity;
			float _BacklightHardness;
			float _BacklightIntensity;
			float _RimPower;
			float _RimThickness;
			float _RimSmoothness;
			float _SpecularPosterizeSteps;
			float _BacklightAmount;
			float _AdditionalLightsSmoothnessMultiplier;
			float _UVAnimationSpeed;
			float _OverlayRotation;
			float _WarpStrength;
			float _WarpTextureRotation;
			float _UVAnimationSpeedWarp;
			float _UVSrcrollAngleWarp;
			float _UVScrollSpeedWarp;
			float _WarpTextureScale;
			float _UseDiffuseWarp;
			float _StepOffset;
			float _OcclusionStrength;
			float _OutlineTextureStrength;
			float _Thicnkess;
			float _OutlineNoiseIntesity;
			float _OutlineNoiseScale;
			float _OutlineDynamicSpeed;
			float _AdaptiveThicnkess;
			float _NormalMapStrength;
			float _Glossiness;
			float _UseShadows;
			float _LightRampOffset;
			float _MaxScaleDependingOnCamera;
			float _UVSrcrollAngle;
			float _UVScrollSpeed;
			float _OverlayUVTilling;
			float _HalftoneSmoothness;
			float _HalftoneEdgeOffset;
			float _HalftoneThreshold;
			float _ShapeSmoothness;
			float _HalftoneToonAffect;
			float _IndirectLightStrength;
			float _AdditionalLightsFaloff;
			float _AdditionalLightsAmount;
			float _DiffusePosterizeSteps;
			float _DiffusePosterizePower;
			float _DiffusePosterizeOffset;
			float _DiffuseWarpBrightnessOffset;
			float _PaperTilling;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _MainTex;
			sampler2D _OcclusionMap;


			
			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_texcoord2.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord2.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.normalWS = normalInput.normalWS;
				output.tangentWS = float4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			void frag(PackedVaryings input
						, out half4 outNormalWS : SV_Target0
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						#if ( UNITY_VERSION >= 60020000 )
						, out uint outRenderingLayers : SV_Target1
						#else
						, out float4 outRenderingLayers : SV_Target1
						#endif
						#endif
						 )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 NormalWS = input.normalWS * renormFactor;
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float2 uv_OcclusionMap = input.ase_texcoord2.xy * _OcclusionMap_ST.xy + _OcclusionMap_ST.zw;
				float4 tex2DNode362 = tex2D( _MainTex, uv_OcclusionMap );
				float lerpResult1655 = lerp( 1.0 , tex2D( _OcclusionMap, uv_OcclusionMap ).r , _OcclusionStrength);
				float4 appendResult1656 = (float4(lerpResult1655 , lerpResult1655 , lerpResult1655 , 1.0));
				float4 MainTexture364 = ( _Color * tex2DNode362 * appendResult1656 );
				float temp_output_673_0 = ( MainTexture364.a * 1.0 );
				

				float3 Normal = float3(0, 0, 1);
				float Alpha = temp_output_673_0;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				#if defined(_GBUFFER_NORMALS_OCT)
					float2 octNormalWS = PackNormalOctQuadEncode(NormalWS);
					float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
					half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
					outNormalWS = half4(packedNormalWS, 0.0);
				#else
					#if defined(_NORMALMAP)
						#if _NORMAL_DROPOFF_TS
							float3 normalWS = TransformTangentToWorld(Normal, half3x3(TangentWS, BitangentWS, NormalWS));
						#elif _NORMAL_DROPOFF_OS
							float3 normalWS = TransformObjectToWorldNormal(Normal);
						#elif _NORMAL_DROPOFF_WS
							float3 normalWS = Normal;
						#endif
					#else
						float3 normalWS = NormalWS;
					#endif
					outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					#if ( UNITY_VERSION >= 60020000 )
					outRenderingLayers = EncodeMeshRenderingLayer();
					#else
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
					#endif
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "MotionVectors"
			Tags { "LightMode"="MotionVectors" }

			ColorMask RG

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#define ASE_TIME_BASED_MOTION_VECTORS
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170004


			#pragma vertex vert
			#pragma fragment frag

            #define SHADERPASS SHADERPASS_MOTION_VECTORS

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"

			#if ( UNITY_VERSION < 60010000 )
				#define APPLICATION_SPACE_WARP_MOTION APLICATION_SPACE_WARP_MOTION
			#endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 positionOld : TEXCOORD4;
				#if _ADD_PRECOMPUTED_VELOCITY
					float3 alembicMotionVector : TEXCOORD5;
				#endif
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 positionCSNoJitter : TEXCOORD0;
				float4 previousPositionCSNoJitter : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RimShadowColor;
			float4 _HalftoneColor;
			float4 _RimColor;
			float4 _Color;
			float4 _OcclusionMap_ST;
			float4 _OutlineColor;
			float4 _ShadowColor;
			float4 _HatchingColor;
			float4 _EmissionColor;
			float4 _SpecColor;
			float _SmoothnessMultiplier;
			float _MainLightIntesity;
			float _UseSpecularMask;
			float _AdditionalLightsIntesity;
			float _StepHalftoneTexture;
			float _SpecularMaskScale;
			float _SpecularFaloff;
			float _UVAnimationSpeedSpec;
			float _UVScrollSpeedSpec;
			float _UVSrcrollAngleSpec;
			float _SpecularMaskRotation;
			float _HaltonePatternSize;
			float _SpecularMaskStrength;
			float _RampDiffuseTextureLoaded;
			float _Strength;
			float _Lighten;
			float _Darken;
			float _IndirectLightAffectOnHatch;
			float _UseEmission;
			float _SideShineAmount;
			float _SideShineHardness;
			float _SpecularShadowMask;
			float _SideShineIntensity;
			float _BacklightHardness;
			float _BacklightIntensity;
			float _RimPower;
			float _RimThickness;
			float _RimSmoothness;
			float _SpecularPosterizeSteps;
			float _BacklightAmount;
			float _AdditionalLightsSmoothnessMultiplier;
			float _UVAnimationSpeed;
			float _OverlayRotation;
			float _WarpStrength;
			float _WarpTextureRotation;
			float _UVAnimationSpeedWarp;
			float _UVSrcrollAngleWarp;
			float _UVScrollSpeedWarp;
			float _WarpTextureScale;
			float _UseDiffuseWarp;
			float _StepOffset;
			float _OcclusionStrength;
			float _OutlineTextureStrength;
			float _Thicnkess;
			float _OutlineNoiseIntesity;
			float _OutlineNoiseScale;
			float _OutlineDynamicSpeed;
			float _AdaptiveThicnkess;
			float _NormalMapStrength;
			float _Glossiness;
			float _UseShadows;
			float _LightRampOffset;
			float _MaxScaleDependingOnCamera;
			float _UVSrcrollAngle;
			float _UVScrollSpeed;
			float _OverlayUVTilling;
			float _HalftoneSmoothness;
			float _HalftoneEdgeOffset;
			float _HalftoneThreshold;
			float _ShapeSmoothness;
			float _HalftoneToonAffect;
			float _IndirectLightStrength;
			float _AdditionalLightsFaloff;
			float _AdditionalLightsAmount;
			float _DiffusePosterizeSteps;
			float _DiffusePosterizePower;
			float _DiffusePosterizeOffset;
			float _DiffuseWarpBrightnessOffset;
			float _PaperTilling;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _MainTex;
			sampler2D _OcclusionMap;


			
			// Applies the graph's vertex stage at a given time so the motion vector pass can
			// evaluate the current frame and re-evaluate the previous frame (procedural / time-based animation).
			Attributes ASEApplyVertexModification( Attributes input, float3 timeParameters, inout PackedVaryings output, out float3 customMotionVector  )
			{
				float3 currentTimeParameters = _TimeParameters.xyz;
				_TimeParameters.xyz = timeParameters;

				output.ase_texcoord3.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord3.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				customMotionVector = float3(0, 0, 0);

				_TimeParameters.xyz = currentTimeParameters;
				return input;
			}

			PackedVaryings VertexFunction( Attributes input )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				Attributes defaultInput = input;
				float3 currentMotionVector;
				input = ASEApplyVertexModification( input, _TimeParameters.xyz, output, currentMotionVector );

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(APPLICATION_SPACE_WARP_MOTION)
					float4 positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.positionOS));
					float4 positionCS = positionCSNoJitter;
				#else
					float4 positionCS = vertexInput.positionCS;
					float4 positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.positionOS));
				#endif

				// Custom output and automatic time-based motion are mutually exclusive.
				#if defined(ASE_CUSTOM_MOTION_VECTOR)
					float3 prevPositionOS = ( unity_MotionVectorsParams.x == 1 ) ? input.positionOld : input.positionOS.xyz;
					prevPositionOS -= currentMotionVector;
				#else
					float3 prevPositionOS = ( unity_MotionVectorsParams.x == 1 ) ? input.positionOld : defaultInput.positionOS.xyz;
					#ifdef ASE_TIME_BASED_MOTION_VECTORS
						Attributes prevInput = defaultInput;
						prevInput.positionOS.xyz = prevPositionOS;
						PackedVaryings prevOutput = (PackedVaryings)0;
						float3 prevMotionVector;
						prevInput = ASEApplyVertexModification( prevInput, _LastTimeParameters.xyz, prevOutput, prevMotionVector );
						prevPositionOS = prevInput.positionOS.xyz;
					#endif
				#endif
				#if _ADD_PRECOMPUTED_VELOCITY
					prevPositionOS -= input.alembicMotionVector;
				#endif
				float4 previousPositionCSNoJitter = mul( _PrevViewProjMatrix, mul( UNITY_PREV_MATRIX_M, float4( prevPositionOS, 1 ) ) );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( positionCS );
				output.positionCSNoJitter = ASE_ADJUST_CLIP_POSITION( positionCSNoJitter );
				output.previousPositionCSNoJitter = ASE_ADJUST_CLIP_POSITION( previousPositionCSNoJitter );
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}

			half4 frag(	PackedVaryings input
				#if defined( ASE_WRITE_DEPTH )
				,out float outputDepth : ASE_SV_DEPTH
				#endif
				 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;

				float2 uv_OcclusionMap = input.ase_texcoord3.xy * _OcclusionMap_ST.xy + _OcclusionMap_ST.zw;
				float4 tex2DNode362 = tex2D( _MainTex, uv_OcclusionMap );
				float lerpResult1655 = lerp( 1.0 , tex2D( _OcclusionMap, uv_OcclusionMap ).r , _OcclusionStrength);
				float4 appendResult1656 = (float4(lerpResult1655 , lerpResult1655 , lerpResult1655 , 1.0));
				float4 MainTexture364 = ( _Color * tex2DNode362 * appendResult1656 );
				float temp_output_673_0 = ( MainTexture364.a * 1.0 );
				

				float Alpha = temp_output_673_0;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined( ASE_CHANGES_WORLD_POS )
					float3 positionOS = mul( GetWorldToObjectMatrix(),  float4( PositionWS, 1.0 ) ).xyz;
					float3 previousPositionWS = mul( GetPrevObjectToWorldMatrix(),  float4( positionOS, 1.0 ) ).xyz;
					input.positionCSNoJitter = mul( _NonJitteredViewProjMatrix, float4( PositionWS, 1.0 ) );
					input.previousPositionCSNoJitter = mul( _PrevViewProjMatrix, float4( previousPositionWS, 1.0 ) );
				#endif

				#if defined( LOD_FADE_CROSSFADE )
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				#if defined(APPLICATION_SPACE_WARP_MOTION)
					return float4( CalcAswNdcMotionVectorFromCsPositions( input.positionCSNoJitter, input.previousPositionCSNoJitter ), 1 );
				#else
					return float4( CalcNdcMotionVectorFromCsPositions( input.positionCSNoJitter, input.previousPositionCSNoJitter ), 0, 0 );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{			
			
			Name "GBuffer"
			Tags { "LightMode"="UniversalGBuffer" }

			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#define UNLIT_DEFAULT_DECAL_BLENDING
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170004


			// Deferred Rendering Path does not support the OpenGL-based graphics API:
			// Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
			#pragma exclude_renderers glcore gles3 

			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_GBUFFER

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if ( UNITY_VERSION >= 60030016 && UNITY_VERSION < 60040000 ) || ( UNITY_VERSION >= 60040010 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutputFormat.hlsl"
			#endif

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
				#define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				half4 tangentWS : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RimShadowColor;
			float4 _HalftoneColor;
			float4 _RimColor;
			float4 _Color;
			float4 _OcclusionMap_ST;
			float4 _OutlineColor;
			float4 _ShadowColor;
			float4 _HatchingColor;
			float4 _EmissionColor;
			float4 _SpecColor;
			float _SmoothnessMultiplier;
			float _MainLightIntesity;
			float _UseSpecularMask;
			float _AdditionalLightsIntesity;
			float _StepHalftoneTexture;
			float _SpecularMaskScale;
			float _SpecularFaloff;
			float _UVAnimationSpeedSpec;
			float _UVScrollSpeedSpec;
			float _UVSrcrollAngleSpec;
			float _SpecularMaskRotation;
			float _HaltonePatternSize;
			float _SpecularMaskStrength;
			float _RampDiffuseTextureLoaded;
			float _Strength;
			float _Lighten;
			float _Darken;
			float _IndirectLightAffectOnHatch;
			float _UseEmission;
			float _SideShineAmount;
			float _SideShineHardness;
			float _SpecularShadowMask;
			float _SideShineIntensity;
			float _BacklightHardness;
			float _BacklightIntensity;
			float _RimPower;
			float _RimThickness;
			float _RimSmoothness;
			float _SpecularPosterizeSteps;
			float _BacklightAmount;
			float _AdditionalLightsSmoothnessMultiplier;
			float _UVAnimationSpeed;
			float _OverlayRotation;
			float _WarpStrength;
			float _WarpTextureRotation;
			float _UVAnimationSpeedWarp;
			float _UVSrcrollAngleWarp;
			float _UVScrollSpeedWarp;
			float _WarpTextureScale;
			float _UseDiffuseWarp;
			float _StepOffset;
			float _OcclusionStrength;
			float _OutlineTextureStrength;
			float _Thicnkess;
			float _OutlineNoiseIntesity;
			float _OutlineNoiseScale;
			float _OutlineDynamicSpeed;
			float _AdaptiveThicnkess;
			float _NormalMapStrength;
			float _Glossiness;
			float _UseShadows;
			float _LightRampOffset;
			float _MaxScaleDependingOnCamera;
			float _UVSrcrollAngle;
			float _UVScrollSpeed;
			float _OverlayUVTilling;
			float _HalftoneSmoothness;
			float _HalftoneEdgeOffset;
			float _HalftoneThreshold;
			float _ShapeSmoothness;
			float _HalftoneToonAffect;
			float _IndirectLightStrength;
			float _AdditionalLightsFaloff;
			float _AdditionalLightsAmount;
			float _DiffusePosterizeSteps;
			float _DiffusePosterizePower;
			float _DiffusePosterizeOffset;
			float _DiffuseWarpBrightnessOffset;
			float _PaperTilling;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _MainTex;
			sampler2D _OcclusionMap;


			#if ( UNITY_VERSION >= 60010000 )
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
			#else
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
			#endif

			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_texcoord3.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord3.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				output.positionCS = ASE_ADJUST_CLIP_POSITION(vertexInput.positionCS );
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalInput.normalWS;
				output.tangentWS = half4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

		#if ( UNITY_VERSION >= 60010000 )
			GBufferFragOutput frag ( PackedVaryings input
		#else
			FragmentOutput frag ( PackedVaryings input
		#endif
								#if defined( ASE_WRITE_DEPTH )
								,out float outputDepth : ASE_SV_DEPTH
								#endif
								 )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 PositionWS = input.positionWS;
				float3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float3 NormalWS = input.normalWS * renormFactor;

				float2 uv_OcclusionMap = input.ase_texcoord3.xy * _OcclusionMap_ST.xy + _OcclusionMap_ST.zw;
				float4 tex2DNode362 = tex2D( _MainTex, uv_OcclusionMap );
				float lerpResult1655 = lerp( 1.0 , tex2D( _OcclusionMap, uv_OcclusionMap ).r , _OcclusionStrength);
				float4 appendResult1656 = (float4(lerpResult1655 , lerpResult1655 , lerpResult1655 , 1.0));
				float4 MainTexture364 = ( _Color * tex2DNode362 * appendResult1656 );
				float temp_output_673_0 = ( MainTexture364.a * 1.0 );
				

				float3 Color = float4( 0,0,0,0 ).rgb;
				float3 Normal = float3(0, 0, 1);
				float Alpha = temp_output_673_0;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
					float AlphaClipThresholdShadow = 0.5;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;

				#ifdef _NORMALMAP
					#if _NORMAL_DROPOFF_TS
						inputData.normalWS = TransformTangentToWorld(Normal, half3x3( TangentWS, BitangentWS, NormalWS ));
					#elif _NORMAL_DROPOFF_OS
						inputData.normalWS = TransformObjectToWorldNormal(Normal);
					#elif _NORMAL_DROPOFF_WS
						inputData.normalWS = Normal;
					#endif
					inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				#else
					inputData.normalWS = NormalWS;
				#endif

				inputData.viewDirectionWS = ViewDirWS;

				#if defined(_DBUFFER) && defined(UNLIT_DEFAULT_DECAL_BLENDING)
					ApplyDecalToBaseColor(input.positionCS, Color);
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = Color;
				surfaceData.alpha = Alpha;

				#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
					float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV( unpacked.positionCS );
					AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion( normalizedScreenSpaceUV );
					surfaceData.albedo.rgb *= aoFactor.directAmbientOcclusion;
				#else
					surfaceData.occlusion = 1;
				#endif

			#if ( UNITY_VERSION >= 60010000 )
				return PackGBuffersSurfaceData( surfaceData, inputData, float3( 0, 0, 0 ) );
			#else
				return SurfaceDataToGbuffer( surfaceData, inputData, float3( 0, 0, 0 ), kLightingInvalid );
			#endif
			}

			ENDHLSL
		}
		
	}
	

	

	CustomEditor "ToonProEditor"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19912
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":277,"pos":[-8712.34,-525.5221],"params":["Inherit","False","6331.035","6906.721","","14","217","1122","386","901","390","213","385","215","1208","216","219","1829","1840","1845","BlinnPhong","0.2631274,0.6002151,0.6886792,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":385,"pos":[-8602.182,211.8337],"params":["Inherit","False","2738.806","952.1386","Comment","34","1531","1529","1528","1527","1526","1525","1517","1521","1520","1519","1518","1524","1523","1522","381","1495","1530","1514","1488","1496","1492","1489","378","375","379","382","384","250","1664","1668","1794","1770","1846","1847","Diffuse Warp","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1529,"pos":[-8596.961,1031.286],"params":["Inherit","False","Property","_UVAnimationSpeedWarp","UV Animation Speed","81","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","5","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ScreenParams, AmplifyShaderEditor","id":1844,"pos":[-8730.684,484.0242],"params":["Inherit","False","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1517,"pos":[-8589.885,771.4034],"params":["Inherit","False","Property","_UVSrcrollAngleWarp","UV Scroll Angle","86","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","360","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":1525,"pos":[-8343.076,953.0225],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":381,"pos":[-8645.469,209.3164],"params":["Inherit","False","Property","_WarpTextureScale","UV Tiling","23","0","Create","False","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":1845,"pos":[-8606.523,362.2712],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RadiansOpNode, AmplifyShaderEditor","id":1518,"pos":[-8305.884,724.4033],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleRemainderNode, AmplifyShaderEditor","id":1526,"pos":[-8160.476,912.6194],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","2","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1846,"pos":[-8471.604,364.1472],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":678,"pos":[1058.121,1503.775],"params":["Inherit","False","1482.025","1686.722","Comment","27","636","659","686","679","685","658","680","638","682","677","364","204","362","1388","1376","1536","1537","1538","1539","1540","1541","1651","1652","1653","1655","1656","1669","Inputs","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CosOpNode, AmplifyShaderEditor","id":1519,"pos":[-8162.881,696.4033],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SinOpNode, AmplifyShaderEditor","id":1521,"pos":[-8175.882,794.4034],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FloorOpNode, AmplifyShaderEditor","id":1528,"pos":[-8007.676,981.0222],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1524,"pos":[-8392.847,605.2203],"params":["Inherit","False","Property","_UVScrollSpeedWarp","UV Scroll Speed","87","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","0.1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1847,"pos":[-8324.31,349.5988],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GrabScreenPosition, AmplifyShaderEditor","id":1794,"pos":[-8257.064,105.5836],"params":["Inherit","False","0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":1669,"pos":[1124.039,1766.173],"params":["Inherit","False","0","1651","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":1523,"pos":[-8069.095,676.4829],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1520,"pos":[-8031.88,723.4033],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1668,"pos":[-8004.736,343.5169],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","FLOAT2","0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1527,"pos":[-7881.676,894.6254],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1770,"pos":[-8123.687,494.8918],"params":["Inherit","False","StaticScreenSpaceUV","-1","","211","1e9a29825d5b8df43882e5bd6744aaf5","0","1","7","FLOAT","1","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1531,"pos":[-7726.941,861.5594],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1539,"pos":[1768.883,3052.132],"params":["Inherit","False","Property","_NormalMapStrength","Normal Map Strength","31","0","Create","False","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":1541,"pos":[1421.466,2768.47],"params":["Inherit","True","Property","_BumpMap","Normal Map","30","1","[Normal]","Create","False","0","0","0","False","0","False","","-1","None","None","True","0","False","bump","Auto","True","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":1540,"pos":[1479.323,2996.637],"params":["Inherit","False","Constant","_Vector0","Vector 0","44","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0,1","0,0,0","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1522,"pos":[-7825.463,655.2419],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT2","1,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1664,"pos":[-7871.438,432.9202],"params":["Inherit","False","Property","_UseAdaptiveScreenUvsWarp","Adaptive Screen Uvs ","111","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT4","0,0,0,0","False","0","FLOAT4","0,0,0,0","False","2","FLOAT4","0,0,0,0","False","3","FLOAT4","0,0,0,0","False","4","FLOAT4","0,0,0,0","False","5","FLOAT4","0,0,0,0","False","6","FLOAT4","0,0,0,0","False","7","FLOAT4","0,0,0,0","False","8","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1122,"pos":[-4231.386,2746.804],"params":["Inherit","False","1770.555","1368.74","","32","1819","1818","1817","1816","1815","1814","1813","1812","1811","1810","1809","1808","1807","1806","1805","1804","1127","699","1126","1125","1124","1129","1769","1132","1123","439","1793","1665","1841","1842","1843","1859","Specular UVs","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1496,"pos":[-7041.899,699.3228],"params":["Inherit","False","Property","_WarpTextureRotation","UV Rotation","47","0","Create","False","0","0","0","True","0","False","Object","-1","","0","83.96","0","180","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1530,"pos":[-7535.959,485.052],"params":["Inherit","False","3","3","0","FLOAT4","0,0,0,0","False","1","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1536,"pos":[1761.389,2825.948],"params":["Inherit","False","3","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":1495,"pos":[-7784.187,255.1986],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1813,"pos":[-4148.26,3972.164],"params":["Inherit","False","Property","_UVAnimationSpeedSpec","UV Animation Speed","82","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","5","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RadiansOpNode, AmplifyShaderEditor","id":1488,"pos":[-6856.261,517.7318],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldNormalVector, AmplifyShaderEditor","id":1537,"pos":[1916.055,2801.07],"params":["Inherit","False","True","1","0","FLOAT3","0,0,1","False","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1514,"pos":[-7422.728,256.8805],"params":["Inherit","False","Property","_UseScreenUvsWarp","Screen Uvs","102","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT4","0,0,0,0","False","0","FLOAT4","0,0,0,0","False","2","FLOAT4","0,0,0,0","False","3","FLOAT4","0,0,0,0","False","4","FLOAT4","0,0,0,0","False","5","FLOAT4","0,0,0,0","False","6","FLOAT4","0,0,0,0","False","7","FLOAT4","0,0,0,0","False","8","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":1492,"pos":[-7149.467,337.9672],"params":["Inherit","False","Constant","_Vector1","Vector 1","87","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5,0.5","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1054,"pos":[-2110.905,5222.568],"params":["Inherit","False","2863.306","1438.541","","31","1294","1282","486","1296","1293","1047","1051","1295","1292","1050","1049","488","1287","1279","1277","1288","1278","1285","1280","1661","1297","1281","1056","1048","478","1792","1803","1848","1849","1850","1851","Overlay UVs","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":216,"pos":[-6310.873,-429.0269],"params":["Inherit","False","698.8475","398.5884","","4","1542","269","233","234","NDotL","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1538,"pos":[2114.056,2805.07],"params":["Float","False","BNCurrentNormal","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor","id":1489,"pos":[-6901.433,291.9619],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","2","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":1814,"pos":[-3864.283,3942.325],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.ScreenParams, AmplifyShaderEditor","id":1840,"pos":[-4310.225,2869.182],"params":["Inherit","False","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":379,"pos":[-6571.373,718.5477],"params":["Inherit","False","Property","_WarpStrength","Warp Strength","21","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","-1","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleRemainderNode, AmplifyShaderEditor","id":1815,"pos":[-3603.119,3956.944],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","2","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1542,"pos":[-6257.038,-382.9371],"params":["Inherit","False","1538","BNCurrentNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":375,"pos":[-6687.352,435.7679],"params":["Inherit","True","Property","_DiffuseWarpNoise","Diffuse Warp Noise","18","2","[NoScaleOffset]","[SingleLineTexture]","Create","False","0","0","0","False","0","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":1843,"pos":[-4143.065,2872.429],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldSpaceLightDirHlpNode, AmplifyShaderEditor","id":269,"pos":[-6073.979,-207.2886],"params":["Inherit","False","False","1","0","FLOAT","0","False","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1285,"pos":[-1793.961,6525.957],"params":["Inherit","False","Property","_UVAnimationSpeed","UV Animation Speed","83","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","5","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":439,"pos":[-4276.767,2774.767],"params":["Inherit","False","Property","_SpecularMaskScale","Specular Mask Tiling","22","0","Create","False","0","0","0","True","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":386,"pos":[-8655.438,1520.64],"params":["Inherit","False","4310.062","1258.77","","78","1451","372","361","1360","1331","1416","1157","1504","391","1554","267","1276","370","1355","1087","1188","1418","248","262","1356","1502","264","965","260","1501","240","251","356","1505","223","1417","358","1329","1508","1211","360","371","1362","359","1359","389","1500","1274","249","444","1357","1358","1330","445","1498","1599","1600","1604","1605","1509","1611","1485","1470","1483","1511","1486","1510","1617","1618","1619","1622","1623","1625","1626","1628","1629","1680","1683","1684","1880","1881","1885","1888","Main Light Diffuse Mode","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":219,"pos":[-8680.826,-457.1807],"params":["Inherit","False","1797.078","446.0921","","9","1415","1412","244","1414","227","247","1852","1854","1853","Light Attenuation","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1841,"pos":[-4014.146,2838.305],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FloorOpNode, AmplifyShaderEditor","id":1816,"pos":[-3360.699,3957.349],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":378,"pos":[-6332.124,557.7954],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":1278,"pos":[-1509.984,6496.118],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":382,"pos":[-6540.259,272.6273],"params":["Inherit","False","Property","_UseDiffuseWarp","UseDiffuse Warp","24","1","[Toggle]","Create","True","0","0","0","True","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":234,"pos":[-6008.713,-366.3873],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ScreenParams, AmplifyShaderEditor","id":1848,"pos":[-1863.346,6005.213],"params":["Inherit","False","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleRemainderNode, AmplifyShaderEditor","id":1279,"pos":[-1248.82,6510.738],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","2","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1292,"pos":[-2055.213,5697.848],"params":["Inherit","False","Property","_UVSrcrollAngle","UV Scroll Angle","85","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","360","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":249,"pos":[-8572.477,2017.969],"params":["Inherit","False","250","BNLightWarpVector","1","0","OBJECT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1817,"pos":[-3228.32,3954.951],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":233,"pos":[-5864.712,-350.3873],"params":["Float","False","BNNDotL","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1805,"pos":[-4126.238,3771.59],"params":["Inherit","False","Property","_UVSrcrollAngleSpec","UV Scroll Angle","84","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","360","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":1851,"pos":[-1628.057,6022.402],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Compare, AmplifyShaderEditor","id":384,"pos":[-6293.789,317.1498],"params":["Inherit","False","0","4","0","FLOAT","0","False","1","FLOAT","1","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":1414,"pos":[-8611.005,-378.3377],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.GrabScreenPosition, AmplifyShaderEditor","id":1793,"pos":[-4190.236,3048.502],"params":["Inherit","False","0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1056,"pos":[-1707.346,5261.636],"params":["Inherit","False","Property","_OverlayUVTilling","UV Tiling","67","0","Create","False","0","0","0","False","0","False","Object","-1","","0","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1842,"pos":[-4019.146,2951.305],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RadiansOpNode, AmplifyShaderEditor","id":1807,"pos":[-3842.239,3724.59],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RadiansOpNode, AmplifyShaderEditor","id":1293,"pos":[-1771.214,5650.848],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1769,"pos":[-4021.579,3262.117],"params":["Inherit","False","StaticScreenSpaceUV","-1","","202","1e9a29825d5b8df43882e5bd6744aaf5","0","1","7","FLOAT","1","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.FloorOpNode, AmplifyShaderEditor","id":1280,"pos":[-1006.4,6511.142],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1129,"pos":[-3887.68,3034.723],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":248,"pos":[-8406.031,1899.421],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":250,"pos":[-6141.967,419.4187],"params":["Float","False","BNLightWarpVector","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LightColorNode, AmplifyShaderEditor","id":247,"pos":[-7663.741,-417.6765],"params":["Inherit","False","0","3","COLOR","0","FLOAT3","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":223,"pos":[-8221.254,2079.614],"params":["Inherit","False","233","BNNDotL","1","0","OBJECT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1850,"pos":[-1499.537,5960.707],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1812,"pos":[-3075.319,3907.655],"params":["Inherit","False","Property","_OverlayMode1","Overlay Mode","64","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","KeywordEnum","3","None","Haftone","Hatching","Create","True","True","All","9","1","FLOAT","0","False","0","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","5","FLOAT","0","False","6","FLOAT","0","False","7","FLOAT","0","False","8","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":1412,"pos":[-8372.836,-363.0377],"params":["Inherit","False","\n    #if SHADOWS_SCREEN\n        half4 clipPos = TransformWorldToHClip(WorldPos);\n        half4 shadowCoord = ComputeScreenPos(clipPos);\n    #else\n        half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);\n    #endif\n\n    Light mainLight = GetMainLight(shadowCoord);\n    DistanceAtten = mainLight.distanceAttenuation;\n    ShadowAtten = mainLight.shadowAttenuation;\n","8","Create","3","True","WorldPos","FLOAT3","0,0,0","In","","Inherit","False","True","DistanceAtten","FLOAT","0","Out","","Inherit","False","True","ShadowAtten","FLOAT","0","Out","","Inherit","False","Light Attenuation","True","False","0","","False","4","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","3","FLOAT","0","False","3","FLOAT","0","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":217,"pos":[-5811.318,98.81934],"params":["Inherit","False","977.2441","332.3028","","6","265","238","237","236","226","222","Half Dir","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1208,"pos":[-8610.043,3076.147],"params":["Inherit","False","1877.47","918.8329","","18","1181","1202","1144","1203","1198","1154","1182","1143","1241","1243","1245","1247","1562","1839","1876","1877","1878","1887","Additional Lights Diffuse","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1502,"pos":[-8367.388,2314.624],"params":["Inherit","False","233","BNNDotL","1","0","OBJECT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":1504,"pos":[-8336.877,2427.897],"params":["Float","False","Constant","_Vector2","Vector 2","0","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0,0","0,0,0","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1297,"pos":[-2082.536,5472.369],"params":["Inherit","False","Property","_UVScrollSpeed","UV Scroll Speed","88","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","0.1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":227,"pos":[-7399.636,-366.5314],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SinOpNode, AmplifyShaderEditor","id":1804,"pos":[-3712.239,3794.59],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1818,"pos":[-3103.358,3729.26],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.CosOpNode, AmplifyShaderEditor","id":1806,"pos":[-3699.239,3696.59],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GrabScreenPosition, AmplifyShaderEditor","id":1792,"pos":[-992.3535,5843.21],"params":["Inherit","False","0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1665,"pos":[-3711.186,3132.833],"params":["Inherit","False","Property","_UseAdaptiveUvsSpecular","Adaptive Screen Uvs","110","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT4","0,0,0,0","False","0","FLOAT4","0,0,0,0","False","2","FLOAT4","0,0,0,0","False","3","FLOAT4","0,0,0,0","False","4","FLOAT4","0,0,0,0","False","5","FLOAT4","0,0,0,0","False","6","FLOAT4","0,0,0,0","False","7","FLOAT4","0,0,0,0","False","8","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1809,"pos":[-4153.561,3546.111],"params":["Inherit","False","Property","_UVScrollSpeedSpec","UV Scroll Speed","89","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","0.1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SinOpNode, AmplifyShaderEditor","id":1296,"pos":[-1641.214,5720.848],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CosOpNode, AmplifyShaderEditor","id":1295,"pos":[-1628.214,5622.848],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1281,"pos":[-874.0208,6508.744],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":251,"pos":[-8237.035,1902.421],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1849,"pos":[-1349.361,5918.092],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":389,"pos":[-8423.692,1747.842],"params":["Inherit","False","250","BNLightWarpVector","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":264,"pos":[-8019.342,1914.061],"params":["Float","False","Constant","_Vector3","Vector 3","0","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0,0","0,0,0","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":486,"pos":[-908.7979,5575.512],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":1287,"pos":[-1821.28,5527.19],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.WorldSpaceLightPos, AmplifyShaderEditor","id":226,"pos":[-5790.688,169.8815],"params":["Inherit","False","0","3","FLOAT4","0","FLOAT3","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1294,"pos":[-1497.214,5649.848],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1417,"pos":[-7754.294,2041.835],"params":["Inherit","False","1415","ShadowAtten","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":267,"pos":[-8110.707,1770.583],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":1123,"pos":[-3839.785,2759.804],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":1811,"pos":[-3892.305,3600.931],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1277,"pos":[-736.0189,6461.448],"params":["Inherit","False","Property","_OverlayMode","Overlay Mode","63","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","KeywordEnum","3","None","Haftone","Hatching","Create","True","True","All","9","1","FLOAT","0","False","0","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","5","FLOAT","0","False","6","FLOAT","0","False","7","FLOAT","0","False","8","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1808,"pos":[-3568.239,3723.59],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1505,"pos":[-8113.792,2445.615],"params":["Inherit","False","244","BNAttenuationColor","1","0","OBJECT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMaxOpNode, AmplifyShaderEditor","id":1501,"pos":[-8159.562,2330.285],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1771,"pos":[-968.6199,5403.044],"params":["Inherit","False","StaticScreenSpaceUV","-1","","203","1e9a29825d5b8df43882e5bd6744aaf5","0","1","7","FLOAT","1","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":244,"pos":[-7179.426,-399.5931],"params":["Float","False","BNAttenuationColor","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1508,"pos":[-7986.188,2534.964],"params":["Inherit","False","1415","ShadowAtten","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1859,"pos":[-3406.039,3288.76],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","FLOAT2","0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1202,"pos":[-8488.096,3544.696],"params":["Inherit","False","Property","_AdditionalLightsAmount","Additional Lights Size","76","0","Create","False","0","0","0","True","0","False","Object","-1","","1","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":215,"pos":[-8596.564,4262.269],"params":["Inherit","False","3808.721","1802.809","","50","1577","805","1574","1576","1573","627","220","1397","903","1403","618","256","619","1093","651","1098","1094","1103","1102","623","1404","621","660","207","243","1099","650","252","1400","1097","1100","254","653","652","406","401","1096","583","649","644","588","661","1364","246","665","804","1104","1578","1579","1886","Main Specular ","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1132,"pos":[-3596.041,2808.021],"params":["Inherit","False","Property","_UseScreenUvsSpecular","Screen Uvs","69","0","Create","False","0","0","0","True","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT4","0,0,0,0","False","0","FLOAT4","0,0,0,0","False","2","FLOAT4","0,0,0,0","False","3","FLOAT4","0,0,0,0","False","4","FLOAT4","0,0,0,0","False","5","FLOAT4","0,0,0,0","False","6","FLOAT4","0,0,0,0","False","7","FLOAT4","0,0,0,0","False","8","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1500,"pos":[-7797.34,2328.851],"params":["Inherit","False","3","3","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor","id":236,"pos":[-5736.223,268.1741],"params":["Float","False","World","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1810,"pos":[-3659.305,3546.053],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT2","1,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1358,"pos":[-6851.262,2240.591],"params":["Inherit","False","Property","_DiffusePosterizeOffset","Posterize Offset","93","0","Create","False","0","0","0","True","0","False","Object","-1","","0","0","-0.5","0.5","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1288,"pos":[-1588.28,5472.312],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT2","1,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":240,"pos":[-7812.167,1908.289],"params":["Inherit","False","244","BNAttenuationColor","1","0","OBJECT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMaxOpNode, AmplifyShaderEditor","id":262,"pos":[-7896.342,1750.062],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1803,"pos":[-509.5449,6366.892],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":699,"pos":[-3293.921,3090.699],"params":["Inherit","False","Property","_SpecularMaskRotation","Specular Mask Rotation","48","0","Create","True","0","0","0","True","0","False","Object","-1","","0","83.96","0","180","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":370,"pos":[-7185.927,2449.221],"params":["Inherit","False","Property","_StepOffset","Step Offset","15","0","Create","False","0","0","0","True","0","False","Object","-1","","0","0","-0.5","0.5","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NormalizeNode, AmplifyShaderEditor","id":222,"pos":[-5534.689,146.8815],"params":["Inherit","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1418,"pos":[-7513.909,1994.875],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1661,"pos":[-616.9718,5410.435],"params":["Inherit","False","Property","_UseAdaptiveScreenUvs","Adaptive Screen Uvs","109","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT4","0,0,0,0","False","0","FLOAT4","0,0,0,0","False","2","FLOAT4","0,0,0,0","False","3","FLOAT4","0,0,0,0","False","4","FLOAT4","0,0,0,0","False","5","FLOAT4","0,0,0,0","False","6","FLOAT4","0,0,0,0","False","7","FLOAT4","0,0,0,0","False","8","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1211,"pos":[-7791.012,2207.441],"params":["Inherit","False","1144","AdditionalLightsDiffuse","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":1562,"pos":[-8454,3674.191],"params":["Inherit","False","5","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","2","False","4","FLOAT","2.9","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":901,"pos":[-4308.089,1916.059],"params":["Inherit","False","1917.12","636.1124","","14","1681","440","1561","1631","1213","1632","1634","902","1557","446","437","441","438","1682","Main Specular Mask","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1096,"pos":[-8391.886,5930.841],"params":["Inherit","False","Property","_SmoothnessMultiplier","Main Specular Size","73","0","Create","False","0","0","0","True","0","False","Object","-1","","1","0","0","2","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":1126,"pos":[-3225.028,2813.923],"params":["Inherit","False","Constant","_Vector5","Vector 5","87","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5,0.5","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.RadiansOpNode, AmplifyShaderEditor","id":1127,"pos":[-3087.906,2959.125],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":260,"pos":[-7709.142,1771.562],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":237,"pos":[-5358.689,185.8815],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1362,"pos":[-6558.23,2224.656],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","-1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1498,"pos":[-7478.483,2388.331],"params":["Inherit","False","Property","_DiffuseWarpAffectHalftone","Diffuse Warp Affect Halftone","100","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT4","0,0,0,0","False","0","FLOAT4","0,0,0,0","False","2","FLOAT4","0,0,0,0","False","3","FLOAT4","0,0,0,0","False","4","FLOAT4","0,0,0,0","False","5","FLOAT4","0,0,0,0","False","6","FLOAT4","0,0,0,0","False","7","FLOAT4","0,0,0,0","False","8","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1050,"pos":[105.2931,5619.522],"params":["Inherit","False","Property","_OverlayRotation","UV Rotation","66","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","180","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":371,"pos":[-7003.382,2291.36],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1819,"pos":[-3087.579,3257.327],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","FLOAT2","0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1684,"pos":[-7515.681,2175.847],"params":["Inherit","False","MaxFromVector3","-1","","210","92f2539b674dd3042b132cfbdf18809e","0","1","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":478,"pos":[-761.747,5249.871],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1247,"pos":[-8230.354,3671.345],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","-2","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1282,"pos":[-191.5992,5439.158],"params":["Inherit","False","3","3","0","FLOAT4","0,0,0,0","False","1","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":359,"pos":[-7462.312,1586.428],"params":["Inherit","False","Property","_LightRampOffset","Light Ramp Offset","10","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","-1","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":651,"pos":[-8329.613,5768.437],"params":["Float","False","Constant","_Float0","Float 0","0","0","Create","True","0","0","0","False","0","False","Object","-1","","10","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":1102,"pos":[-8105.622,5870.156],"params":["Inherit","False","2","0","FLOAT","2","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1631,"pos":[-4203.31,2248.858],"params":["Inherit","False","Property","_HaltonePatternSize","Halftone Pattern Size","104","0","Create","False","0","0","0","False","0","False","Object","-1","","0","1","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1683,"pos":[-7252.914,2182.701],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":488,"pos":[-111.0751,5272.073],"params":["Inherit","False","Property","_UseScreenUvs","Screen Uvs","68","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT4","0,0,0,0","False","0","FLOAT4","0,0,0,0","False","2","FLOAT4","0,0,0,0","False","3","FLOAT4","0,0,0,0","False","4","FLOAT4","0,0,0,0","False","5","FLOAT4","0,0,0,0","False","6","FLOAT4","0,0,0,0","False","7","FLOAT4","0,0,0,0","False","8","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RadiansOpNode, AmplifyShaderEditor","id":1049,"pos":[388.5324,5535.734],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":1048,"pos":[151.0441,5347.93],"params":["Inherit","False","Constant","_Vector0","Vector 0","87","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5,0.5","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":1511,"pos":[-5778.932,2570.127],"params":["Inherit","False","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.NormalizeNode, AmplifyShaderEditor","id":238,"pos":[-5230.689,185.8815],"params":["Inherit","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":360,"pos":[-7155.594,1636.903],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1243,"pos":[-8323.181,3876.271],"params":["Inherit","False","Property","_AdditionalLightsFaloff","Additional Lights Falloff","77","0","Create","False","0","0","0","True","0","False","Object","-1","","1","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":445,"pos":[-6943.111,2173.795],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0.009","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1357,"pos":[-6642.462,1926.19],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1509,"pos":[-6016.182,2617.044],"params":["Inherit","False","Property","_DiffuseWarpBrightnessOffset","Brightness Offset","101","0","Create","False","0","0","0","True","0","False","Object","-1","","1.12","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor","id":1124,"pos":[-3052.025,2760.692],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","2","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":1203,"pos":[-8128.235,3545.015],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":660,"pos":[-8374.304,5637.111],"params":["Inherit","False","638","Smoothness","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor","id":1853,"pos":[-8278.682,-217.9064],"params":["Inherit","False","Constant","_Vector4","Vector 4","51","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1,1,1","0,0,0,0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1852,"pos":[-8042.545,-415.1778],"params":["Inherit","False","Property","_UseShadows","Use Shadows","50","1","[Toggle]","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LightColorNode, AmplifyShaderEditor","id":1618,"pos":[-6464,1776],"params":["Inherit","False","0","3","COLOR","0","FLOAT3","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1188,"pos":[-7375.836,1841.717],"params":["Inherit","False","MaxFromVector3","-1","","205","92f2539b674dd3042b132cfbdf18809e","0","1","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1021,"pos":[-2244.832,3912.274],"params":["Inherit","False","4020.123","1130.029","","39","1022","1216","1226","1225","1222","1220","1217","1042","1186","1045","508","1055","531","1041","1040","1046","1043","529","528","525","527","966","526","516","517","1234","1235","1236","1351","1354","1462","1463","1465","1552","1774","1782","1783","1784","1785","Halftone","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1276,"pos":[-7033.281,1816.061],"params":["Inherit","False","Constant","_Float4","Float 4","101","0","Create","True","0","0","0","False","0","False","Object","-1","","0.98","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1356,"pos":[-6496.462,2134.391],"params":["Inherit","False","Property","_DiffusePosterizePower","Posterize Power","92","0","Create","False","0","0","0","True","0","False","Object","-1","","1","0.5","0.5","3","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor","id":1047,"pos":[378.3767,5290.488],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","2","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1483,"pos":[-5756.334,2482.501],"params":["Inherit","False","250","BNLightWarpVector","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1125,"pos":[-2793.566,2839.703],"params":["Inherit","False","SpecularUVs","-1","True","1","0","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":653,"pos":[-8092.612,5714.436],"params":["Inherit","False","3","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":649,"pos":[-7916.612,5847.436],"params":["Float","False","Constant","_Float1","Float 1","0","0","Create","True","0","0","0","False","0","False","Object","-1","","-2","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":965,"pos":[-7175.173,2060.798],"params":["Inherit","False","CompleteDiffuseLight","-1","True","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1359,"pos":[-6473.462,1940.491],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":265,"pos":[-5070.689,185.8815],"params":["Float","False","BNHalfDirection","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":356,"pos":[-7020.02,1622.714],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1510,"pos":[-5609.679,2638.59],"params":["Inherit","False","3","3","0","FLOAT","0","False","1","FLOAT4","-1,0,0,0","False","2","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1274,"pos":[-7037.281,1738.061],"params":["Inherit","False","Constant","_Float3","Float 3","101","0","Create","True","0","0","0","False","0","False","Object","-1","","0.01","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":444,"pos":[-6758.538,2083.357],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0.5","False","2","FLOAT","0.495","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":1634,"pos":[-3893.671,2248.036],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1329,"pos":[-6810.17,2362.941],"params":["Inherit","False","Property","_DiffusePosterizeSteps","Posterize Steps","90","0","Create","False","0","0","0","True","0","False","Object","-1","","3","1","1","10","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1245,"pos":[-7929.325,3746.032],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Compare, AmplifyShaderEditor","id":1854,"pos":[-7931.402,-260.2408],"params":["Inherit","False","0","4","0","FLOAT","1","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.ScreenPosInputsNode, AmplifyShaderEditor","id":1876,"pos":[-7566.274,3749.709],"params":["Float","False","0","False","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1888,"pos":[-6288,1824],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1887,"pos":[-8512,3408],"params":["Inherit","False","Shadow Mask","-1","","61147","b50f5becdd6b8504a861ba5b9b861159","0","1","3","FLOAT2","0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":1241,"pos":[-7770.771,3504.217],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0.005","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ClampOpNode, AmplifyShaderEditor","id":358,"pos":[-6847.237,1691.888],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0.02,0.02","False","2","FLOAT2","0.98,0.98","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1051,"pos":[559.888,5278.561],"params":["Inherit","False","OverlayUVs","-1","True","1","0","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":966,"pos":[-2080.122,4293.563],"params":["Inherit","False","965","CompleteDiffuseLight","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1154,"pos":[-8169.359,3130.21],"params":["Inherit","False","1538","BNCurrentNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1198,"pos":[-8171.903,3228.192],"params":["Inherit","False","250","BNLightWarpVector","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":1182,"pos":[-8395.315,3160.129],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.PowerNode, AmplifyShaderEditor","id":1355,"pos":[-6330.462,1931.391],"params":["Inherit","False","False","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RoundOpNode, AmplifyShaderEditor","id":1330,"pos":[-6322.955,2222.411],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":256,"pos":[-8279.175,5533.507],"params":["Inherit","False","1538","BNCurrentNormal","1","0","OBJECT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1360,"pos":[-6065.672,1969.796],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":650,"pos":[-7782.619,5736.436],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":527,"pos":[-2016.089,4125.125],"params":["Inherit","False","Property","_HalftoneSmoothness","Halftone Smoothness","56","0","Create","True","0","0","0","False","0","False","Object","-1","","0.3","0","0","2","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1485,"pos":[-5487.313,2442.394],"params":["Inherit","False","3","3","0","FLOAT4","0,0,0,0","False","1","FLOAT","0","False","2","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":636,"pos":[1382.471,1568.447],"params":["Inherit","True","Property","_SpecGlossMap","Specular Map","39","2","[NoScaleOffset]","[SingleLineTexture]","Create","False","0","0","0","True","0","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":1632,"pos":[-3725.131,2329.295],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0.3","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":254,"pos":[-8264.929,5444.507],"params":["Inherit","False","265","BNHalfDirection","1","0","OBJECT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":659,"pos":[1476.81,1783.215],"params":["Inherit","False","Property","_Glossiness","Smoothness","40","0","Create","False","0","0","0","True","0","False","Object","-1","","0.5","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1415,"pos":[-7721.192,-234.9471],"params":["Inherit","False","ShadowAtten","-1","True","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1213,"pos":[-4175.321,2011.506],"params":["Inherit","False","1125","SpecularUVs","1","0","OBJECT","","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SwizzleNode, AmplifyShaderEditor","id":1877,"pos":[-7365.036,3749.838],"params":["Inherit","False","FLOAT2","0","1","2","3","1","0","FLOAT4","0,0,0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMaxOpNode, AmplifyShaderEditor","id":1885,"pos":[-6112,1808],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1839,"pos":[-8112,3376],"params":["Inherit","False","Shadowmask","-1","True","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1486,"pos":[-5319.875,2450.541],"params":["Inherit","False","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1416,"pos":[-6012.252,2377.936],"params":["Inherit","False","1415","ShadowAtten","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":1331,"pos":[-6098.767,2092.703],"params":["Inherit","False","return  floor(In / (1 / Steps)) * (1 / Steps);","1","Create","2","True","In","FLOAT","0","In","","Inherit","False","True","Steps","FLOAT","0","In","","Inherit","False","Posterize","True","False","0","","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":526,"pos":[-2025.089,3992.125],"params":["Inherit","False","Property","_HalftoneEdgeOffset","Halftone Edge Offset","57","0","Create","True","0","0","0","False","0","False","Object","-1","","0.1","0","-1","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1055,"pos":[-2013.269,4645.898],"params":["Inherit","False","1051","OverlayUVs","1","0","OBJECT","","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":441,"pos":[-3840.463,2031.383],"params":["Inherit","True","Property","_SpecularMaskTexture","Specular Mask Texture","19","2","[NoScaleOffset]","[SingleLineTexture]","Create","True","0","0","0","True","0","False","","-1","None","0c021cea0cf2417459ce7fa844118e17","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1625,"pos":[-5706.054,2072.304],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":1557,"pos":[-3571.099,2214.36],"params":["Inherit","True","3","0","FLOAT","0","False","1","FLOAT","0.6","False","2","FLOAT","0.97","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":1623,"pos":[-6054.545,1579.347],"params":["Inherit","False","Constant","_Vector7","Vector 7","127","0","Create","True","0","0","0","False","0","False","Object","-1","","0.02,0","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.Exp2OpNode, AmplifyShaderEditor","id":652,"pos":[-7633.619,5741.436],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":252,"pos":[-8017.589,5469.842],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":528,"pos":[-1696.994,4115.875],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1186,"pos":[-1771.505,4300.659],"params":["Inherit","False","MaxFromVector3","-1","","209","92f2539b674dd3042b132cfbdf18809e","0","1","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":658,"pos":[1702.515,1656.614],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1881,"pos":[-5914.11,1864.365],"params":["Inherit","False","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1882,"pos":[-5914.11,1923.365],"params":["Inherit","False","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1880,"pos":[-5984,1824],"params":["Inherit","False","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":361,"pos":[-6688,1584],"params":["Inherit","True","Property","_LightRampTexture","Light Ramp Texture","12","2","[NoScaleOffset]","[SingleLineTexture]","Create","False","0","0","0","True","1","","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":1181,"pos":[-7560.631,3127.085],"params":["Inherit","False","float3 Color = 0;\n\t\n\t\n\n#define SUM_LIGHT(Light)\\\n\t\tfloat3 DotVector = dot(light.direction,WorldNormal);\t\\\n\thalf3 AttLightColor = (light.shadowAttenuation * light.distanceAttenuation);\\\n\t float3 colout = max(float3(0.f,0.f,0.f),LightWrapVector + (1-LightWrapVector) * DotVector )*AttLightColor*light.color; \\\n\tfloat maxColor = max(colout.r,max(colout.g,colout.b));\\\n\tfloat3 outColor = smoothstep(SMin,SMax,maxColor)*light.color;\\\n\t Color += outColor;\n\n\tInputData inputData = (InputData)0;\n\tinputData.normalizedScreenSpaceUV = ScreenUV;\n\tinputData.positionWS = WorldPosition;\n\n\tuint meshRenderingLayers = GetMeshRenderingLayer();\n\tuint pixelLightCount = GetAdditionalLightsCount();\n\n\t#if USE_FORWARD_PLUS\n\tfor (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)\n\t{\n\t\tFORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK\n\n\t\tLight light = GetAdditionalLight(lightIndex, WorldPosition,shadowmask);\n\t\t#ifdef _LIGHT_LAYERS\n\t\tif (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))\n\t\t#endif\n\t\t{\n\t\t\tSUM_LIGHT( light );\n\t\t}\n\t}\n\t#endif\n\n\tLIGHT_LOOP_BEGIN( pixelLightCount )\n\t\tLight light = GetAdditionalLight(lightIndex, WorldPosition,shadowmask);\n\t\t#ifdef _LIGHT_LAYERS\n\t\tif (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))\n\t\t#endif\n\t\t{\n\t\t\tSUM_LIGHT( light );\n\t\t}\n\tLIGHT_LOOP_END\n\n\nreturn Color;","3","Create","8","False","WorldPosition","FLOAT3","0,0,0","In","","Float","False","False","WorldNormal","FLOAT3","0,0,0","In","","Float","False","False","LightWrapVector","FLOAT3","0,0,0","In","","Inherit","False","False","SMin","FLOAT","0","In","","Inherit","False","False","SMax","FLOAT","0","In","","Inherit","False","False","Faloff","FLOAT","0","In","","Inherit","False","False","shadowmask","FLOAT4","1,1,1,1","In","","Inherit","False","True","ScreenUV","FLOAT2","0,0","In","","Inherit","False","AdditionalLight","False","False","0","","False","8","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT","0","False","4","FLOAT","0","False","5","FLOAT","0","False","6","FLOAT4","1,1,1,1","False","7","FLOAT2","0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":465,"pos":[423.3723,-1099.152],"params":["Inherit","False","3204.799","1529.321","","58","760","766","740","764","741","749","762","753","745","763","742","767","748","737","759","743","744","751","747","765","1644","1647","456","1645","1646","1642","739","732","731","726","712","715","457","537","463","718","704","454","467","738","708","702","464","462","719","460","716","755","721","757","758","705","458","756","466","727","1648","1650","Rim","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor","id":1561,"pos":[-3368.183,2096.343],"params":["Inherit","False","Property","_StepHalftoneTexture","Step Halftone Texture","103","0","Create","False","0","0","0","False","0","False","","0","True","Create","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1554,"pos":[-4617.162,1576.96],"params":["Inherit","False","BNDiffuseNoAdditionalLights","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1470,"pos":[-5471.167,2275.45],"params":["Inherit","False","Property","_UseDiffuseWarpAsOverlay","Impact Shadows","99","0","Create","False","0","0","0","True","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT4","0,0,0,0","False","0","FLOAT4","0,0,0,0","False","2","FLOAT4","0,0,0,0","False","3","FLOAT4","0,0,0,0","False","4","FLOAT4","0,0,0,0","False","5","FLOAT4","0,0,0,0","False","6","FLOAT4","0,0,0,0","False","7","FLOAT4","0,0,0,0","False","8","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":1622,"pos":[-5804.497,1548.024],"params":["Inherit","True","Property","_LightRampTexture","Light Ramp Texture","12","2","[NoScaleOffset]","[SingleLineTexture]","Fetch","True","0","0","0","False","1","","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1680,"pos":[-5853.912,1735.709],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1619,"pos":[-5453.472,1878.97],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1629,"pos":[-5486.158,2078.652],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":529,"pos":[-1604.635,3962.274],"params":["Inherit","False","Property","_HalftoneThreshold","Halftone Threshold","58","0","Create","True","0","0","0","False","0","False","Object","-1","","0.1","0","0","0.15","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":638,"pos":[1855.223,1676.036],"params":["Inherit","False","Smoothness","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":440,"pos":[-3290.488,2295.681],"params":["Inherit","False","Property","_SpecularMaskStrength","Specular Mask Strength","20","0","Create","True","0","0","0","True","0","False","Object","-1","","0.1856417","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":665,"pos":[-7740.41,5595.694],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","2","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":525,"pos":[-1451.088,4118.125],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1093,"pos":[-8523.727,4959.168],"params":["Inherit","False","Property","_AdditionalLightsSmoothnessMultiplier","Additional Lights Specular Size","72","0","Create","False","0","0","0","True","0","False","Object","-1","","1","0","0","2","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMaxOpNode, AmplifyShaderEditor","id":246,"pos":[-7868.181,5506.507],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":1451,"pos":[-5641.219,1756.577],"params":["Inherit","False","Property","_ShadowColor","Shadow Color","96","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0,0,0","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":508,"pos":[-1782.492,4485.812],"params":["Inherit","True","Property","_HalftoneTexture","Halftone Texture","55","2","[NoScaleOffset]","[SingleLineTexture]","Create","True","0","0","0","False","0","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1143,"pos":[-7292.4,3144.843],"params":["Inherit","False","Property","_UseAdditionalLightsDiffuse","UseAdditional Lights","75","0","Create","False","0","0","0","False","0","False","","0","1","1","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT3","0,0,0","False","0","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT3","0,0,0","False","5","FLOAT3","0,0,0","False","6","FLOAT3","0,0,0","False","7","FLOAT3","0,0,0","False","8","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1652,"pos":[1503.492,2306.291],"params":["Inherit","False","Property","_OcclusionStrength","Occlusion Strength ","108","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":1651,"pos":[1432.063,2063.587],"params":["Inherit","True","Property","_OcclusionMap","Occlusion Map","107","0","Create","False","0","0","0","False","0","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":1682,"pos":[-2942.101,2277.643],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":583,"pos":[-7704.521,5317.834],"params":["Inherit","False","Property","_SpecularPosterizeSteps","Specular Posterize Steps","32","0","Create","True","0","0","0","True","0","False","Object","-1","","0","0","0","15","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":1351,"pos":[-1334.522,4431.36],"params":["Inherit","False","5","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","0","False","4","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1626,"pos":[-5192.144,1747.387],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":765,"pos":[1128.333,-43.20255],"params":["Inherit","False","Property","_SideShineAmount","Side Shine Amount","54","0","Create","True","0","0","0","False","0","False","Object","-1","","0.2717647","0","0","0.7","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1463,"pos":[-254.7649,4329.303],"params":["Inherit","False","Property","_HalftoneToonAffect","Toon Affect","97","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1617,"pos":[-5316.11,1619.201],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":804,"pos":[-8427.473,4775.182],"params":["Inherit","False","638","Smoothness","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1628,"pos":[-5238.33,1951.119],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.LightColorNode, AmplifyShaderEditor","id":1552,"pos":[-250.2906,4059.738],"params":["Inherit","False","0","3","COLOR","0","FLOAT3","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.Compare, AmplifyShaderEditor","id":1099,"pos":[-7659.398,5872.401],"params":["Inherit","False","0","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":1103,"pos":[-8195.563,4873.534],"params":["Inherit","False","2","0","FLOAT","2","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1046,"pos":[-1312.338,4731.281],"params":["Inherit","False","Property","_ShapeSmoothness","Transition Smoothness","65","0","Create","False","0","0","0","False","0","False","Object","-1","","0.2","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":517,"pos":[-1231.262,4129.005],"params":["Inherit","False","2","0","FLOAT","0.1","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":446,"pos":[-3118.994,2126.112],"params":["Inherit","False","3","0","FLOAT","1","False","1","FLOAT","1","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PowerNode, AmplifyShaderEditor","id":401,"pos":[-7587.071,5545.759],"params":["Inherit","False","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1220,"pos":[-286.4756,4226.19],"params":["Inherit","False","1554","BNDiffuseNoAdditionalLights","1","0","OBJECT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1144,"pos":[-6977.063,3146.878],"params":["Inherit","False","AdditionalLightsDiffuse","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":504,"pos":[-2238.51,2542.837],"params":["Inherit","False","2567.819","1176.527","","24","1053","1069","1111","490","1066","482","503","476","491","1025","275","498","1121","1070","1120","1119","499","492","1067","1064","481","1187","1265","1460","Hatching","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":390,"pos":[-4258.111,704.8892],"params":["Inherit","False","1678.375","818.0153","","14","229","263","1269","692","642","392","1221","691","276","581","239","1266","1363","1466","Final Diffuse","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1655,"pos":[1780.563,2131.611],"params":["Inherit","False","3","0","FLOAT","1","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":1576,"pos":[-8069.019,4436.257],"params":["Inherit","False","Constant","_Vector6","Vector 6","125","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1,1","0,0,0","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1462,"pos":[70.23511,4215.303],"params":["Inherit","False","3","0","COLOR","1,1,1,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1094,"pos":[-8015.695,4808.17],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1157,"pos":[-4645.257,2091.291],"params":["Inherit","False","1144","AdditionalLightsDiffuse","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":805,"pos":[-8281.293,4298.183],"params":["Inherit","False","1538","BNCurrentNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1681,"pos":[-2897.608,2118.314],"params":["Inherit","False","Property","_InverseMask","Inverse Mask","112","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT","0","False","0","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","5","FLOAT","0","False","6","FLOAT","0","False","7","FLOAT","0","False","8","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1235,"pos":[682.1721,4361.406],"params":["Inherit","False","1144","AdditionalLightsDiffuse","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RoundOpNode, AmplifyShaderEditor","id":588,"pos":[-7369.591,5277.984],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":438,"pos":[-2944.745,1963.174],"params":["Inherit","False","Property","_UseSpecularMask","UseSpecular Mask","25","1","[Toggle]","Create","True","0","0","0","True","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":516,"pos":[-1063.541,4249.853],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":372,"pos":[-4920.076,1746.524],"params":["Inherit","False","Property","_UseLightRamp","Shading Mode","16","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","KeywordEnum","3","Step","DiffuseRamp","Posterize","Create","True","True","All","9","1","COLOR","0,0,0,0","False","0","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","4","COLOR","0,0,0,0","False","5","COLOR","0,0,0,0","False","6","COLOR","0,0,0,0","False","7","COLOR","0,0,0,0","False","8","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":748,"pos":[1057.807,-244.0028],"params":["Inherit","False","1538","BNCurrentNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":708,"pos":[472.0822,-281.9579],"params":["Inherit","False","Property","_BacklightAmount","Backlight Amount","49","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldSpaceLightPos, AmplifyShaderEditor","id":741,"pos":[1043.601,246.2542],"params":["Inherit","False","0","3","FLOAT4","0","FLOAT3","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1041,"pos":[-1020.704,4572.356],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","-0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":207,"pos":[-7656.765,5108.074],"params":["Inherit","False","Property","_SpecularFaloff","Specular Falloff","9","0","Create","False","0","0","0","True","0","False","Object","-1","","0","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor","id":1574,"pos":[-8246.844,4372.275],"params":["Inherit","False","World","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":743,"pos":[939.8915,137.4635],"params":["Inherit","False","Constant","_Float2","Float 2","62","0","Create","True","0","0","0","False","0","False","Object","-1","","-1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor","id":744,"pos":[882.2424,-36.06917],"params":["Inherit","False","World","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1100,"pos":[-7367.509,5554.123],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":1577,"pos":[-8039.773,4297.185],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":767,"pos":[1429.617,-17.20018],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","-0.3","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1042,"pos":[-977.7531,4746.639],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1656,"pos":[1932.025,2227.229],"params":["Inherit","False","FLOAT4","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","1","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":204,"pos":[1971.248,1808.104],"params":["Inherit","False","Property","_Color","Color","8","0","Create","True","0","0","0","False","0","False","Object","-1","","0.6792453,0.6792453,0.6792453,1","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1087,"pos":[-4574.137,1910.086],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT3","0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1098,"pos":[-7062.433,4646.951],"params":["Inherit","False","Property","_AdditionalLightsIntesity","Additional Lights Intensity","74","0","Create","False","0","0","0","True","0","False","Object","-1","","1","0","0","6","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":742,"pos":[1133.649,92.01123],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":691,"pos":[-3773.227,1400.115],"params":["Inherit","False","Property","_IndirectLightStrength","Indirect Light Strength","45","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":766,"pos":[1554.617,11.79982],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LightColorNode, AmplifyShaderEditor","id":406,"pos":[-6895.12,4967.884],"params":["Inherit","False","0","3","COLOR","0","FLOAT3","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1119,"pos":[-2110.699,3001.061],"params":["Inherit","False","460","RimLight","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1460,"pos":[-2123.598,3276.599],"params":["Inherit","False","243","BNspecularFinalColor","1","0","OBJECT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1234,"pos":[945.2336,4200.764],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT3","0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1040,"pos":[-870.4089,4520.315],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1222,"pos":[882.5724,4445.615],"params":["Inherit","False","364","MainTexture","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":275,"pos":[-2118.361,3093.842],"params":["Inherit","False","391","BNDiffuse","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":749,"pos":[1336.177,-244.8872],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":1573,"pos":[-7367.304,4528.916],"params":["Inherit","False","float3 Color = 0;\n\nSmoothness = exp2(10 * Smoothness + 1);\nint numLights = GetAdditionalLightsCount();\nfor(int i = 0; i<numLights;i++)\n{\n\t\n\t\t\t#if VERSION_GREATER_EQUAL(10, 1)\n\t\t\tLight light = GetAdditionalLight(i, WorldPosition, half4(1,1,1,1));\n\t\t\t// see AdditionalLights_float for explanation of this\n\t\t#else\n\t\t\tLight light = GetAdditionalLight(i, WorldPosition);\n\t\t#endif\n\t\n\thalf3 AttLightColor = light.color *(light.distanceAttenuation * light.shadowAttenuation);\n\tColor += LightingSpecular(AttLightColor, light.direction, WorldNormal, WorldView, half4(SpecColor, 0), Smoothness);\t\n}\n\nfloat IN = max(Color.b,max(Color.r,Color.g));\n\nfloat minOut = 0.5 * SpecFaloff - 0.005;\nfloat faloff = lerp(IN, smoothstep(minOut, 0.5, IN), SpecFaloff);\nif(Steps < 1)\n{\n    return Color *faloff;\n}\nelse\n{\n    return  Color *floor(faloff / (1 / Steps)) * (1 / Steps);\n}\n","3","Create","7","True","WorldPosition","FLOAT3","0,0,0","In","","Float","False","True","WorldNormal","FLOAT3","0,0,0","In","","Float","False","True","WorldView","FLOAT3","0,0,0","In","","Float","False","True","SpecColor","FLOAT3","0,0,0","In","","Float","False","True","Smoothness","FLOAT","0.5","In","","Float","False","True","Steps","FLOAT","0","In","","Inherit","False","True","SpecFaloff","FLOAT","0","In","","Inherit","False","AdditionalLightsSpecularMy","False","False","0","","False","7","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT","0.5","False","5","FLOAT","0","False","6","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":721,"pos":[783.0558,-265.3714],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","2","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor","id":712,"pos":[604.3212,-477.6704],"params":["Inherit","False","World","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1066,"pos":[-1840.345,3338.422],"params":["Inherit","False","Property","_Lighten","Lighten","71","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","-1","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Compare, AmplifyShaderEditor","id":1104,"pos":[-8050.104,5007.949],"params":["Inherit","False","0","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":1376,"pos":[2060.616,1574.841],"params":["Inherit","False","Property","_SpecColor","Specular Value","3","0","Create","False","0","0","0","True","0","False","Object","-1","","1,1,1,0","1,1,1,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":581,"pos":[-4000.692,1309.903],"params":["Inherit","False","1538","BNCurrentNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.WorldSpaceLightPos, AmplifyShaderEditor","id":702,"pos":[548.6017,-596.4689],"params":["Inherit","False","0","3","FLOAT4","0","FLOAT3","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.Compare, AmplifyShaderEditor","id":437,"pos":[-2681.937,2015.409],"params":["Inherit","False","0","4","0","FLOAT","0","False","1","FLOAT","1","False","2","FLOAT","0","False","3","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1043,"pos":[-805.9669,4710.431],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":1578,"pos":[-6986.635,5267.024],"params":["Inherit","False","float minOut = 0.5 * SpecFaloff - 0.005;\nfloat faloff = lerp(IN, smoothstep(minOut, 0.5, IN), SpecFaloff);\nif(Steps < 1)\n{\n    return faloff;\n}\nelse\n{\n    return  floor(faloff / (1 / Steps)) * (1 / Steps);\n}","1","Create","3","True","IN","FLOAT","0","In","","Inherit","False","True","SpecFaloff","FLOAT","0","In","","Inherit","False","True","Steps","FLOAT","0","In","","Inherit","False","FaloffPosterize","False","False","0","","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1404,"pos":[-7107.193,5108.159],"params":["Inherit","False","Property","_MainLightIntesity","Main Light Intensity","94","0","Create","False","0","0","0","False","0","False","Object","-1","","1","0","0","6","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":531,"pos":[-811.8291,4229.577],"params":["Inherit","False","2","0","FLOAT","0.1","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1120,"pos":[-2101.053,3178.53],"params":["Inherit","False","680","Emission","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1653,"pos":[2040.811,2106.22],"params":["Inherit","False","3","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT4","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":704,"pos":[855.1718,-548.1996],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1400,"pos":[-6736.806,5184.546],"params":["Inherit","False","3","3","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1121,"pos":[-1894.876,3059.795],"params":["Inherit","False","4","4","0","FLOAT","0","False","1","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":391,"pos":[-4532.718,1697.535],"params":["Inherit","False","BNDiffuse","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1388,"pos":[2304.728,1627.696],"params":["Inherit","False","SpecularColor","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1221,"pos":[-3451.219,1360.915],"params":["Inherit","False","IndirectLightStrength","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":763,"pos":[1590.333,-108.2025],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1225,"pos":[1121.372,4201.733],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1097,"pos":[-6732.377,4612.236],"params":["Inherit","False","3","3","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":745,"pos":[1328.984,137.2247],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":1045,"pos":[-648.0934,4367.319],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":902,"pos":[-2568.28,2123.094],"params":["Inherit","False","SpecularMask","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1067,"pos":[-1745.472,3174.743],"params":["Inherit","False","Property","_Darken","Darken","70","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","-1","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1111,"pos":[-1538.438,3340.503],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","2","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":1217,"pos":[75.86629,3963.918],"params":["Inherit","False","Property","_HalftoneColor","Halftone Color","59","0","Create","False","0","0","0","False","0","False","Object","-1","","0,0,0,1","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.IndirectDiffuseLighting, AmplifyShaderEditor","id":276,"pos":[-3764.335,1268.758],"params":["Inherit","False","World","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":719,"pos":[921.3605,-285.2785],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","-2","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":364,"pos":[2184.66,1995.724],"params":["Inherit","False","MainTexture","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1397,"pos":[-5930.385,5184.457],"params":["Inherit","False","638","Smoothness","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":764,"pos":[1678.333,108.7975],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":692,"pos":[-3272.457,1181.337],"params":["Inherit","False","3","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1579,"pos":[-6527.679,4680.167],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":751,"pos":[1721.406,-172.3272],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","4","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1216,"pos":[329.6536,4330.798],"params":["Inherit","True","3","0","COLOR","0,0,0,0","False","1","COLOR","1,1,1,0","False","2","FLOAT","1","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":903,"pos":[-5932.224,5081.812],"params":["Inherit","False","902","SpecularMask","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1187,"pos":[-1529.354,2768.867],"params":["Inherit","False","MaxFromVector3","-1","","204","92f2539b674dd3042b132cfbdf18809e","0","1","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":705,"pos":[1044.782,-545.7766],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","-0.6","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldSpaceCameraPos, AmplifyShaderEditor","id":492,"pos":[-1058.644,3358.441],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":392,"pos":[-4132.12,745.6323],"params":["Inherit","False","391","BNDiffuse","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1403,"pos":[-5952.205,5308.702],"params":["Inherit","False","1388","SpecularColor","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1069,"pos":[-1437.051,3185.468],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","-2","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1782,"pos":[537.9628,4803.693],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1070,"pos":[-1403.74,3305.438],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1784,"pos":[591.0415,4015.491],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":499,"pos":[-979.4488,3531.412],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1461,"pos":[-2166.924,1205.188],"params":["Inherit","False","1696.135","1222.97","","24","1790","1556","1249","1637","1555","1270","1469","1264","1026","1260","1468","1467","1256","1253","1254","1258","1257","1789","1791","1795","1856","1857","1858","1855","Hatching Implementation","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":213,"pos":[-4675.104,-420.4454],"params":["Inherit","False","1762.018","982.026","","14","1231","274","696","694","270","695","259","693","271","1232","1237","1238","1366","1797","Diffuse + Specular","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":503,"pos":[-739.7486,3528.276],"params":["Inherit","False","Property","_MaxScaleDependingOnCamera","Max Scale Depends On Camera","62","0","Create","False","0","0","0","False","0","False","Object","-1","","1","10","1","10","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":1064,"pos":[-1271.18,3066.587],"params":["Inherit","False","5","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","0","False","4","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1785,"pos":[1204.024,4122.485],"params":["Inherit","False","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":619,"pos":[-6552.265,5461.582],"params":["Inherit","False","1538","BNCurrentNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":747,"pos":[1842.102,-59.75346],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1266,"pos":[-3901.295,784.0469],"params":["Inherit","False","Property","_OverlayMode","Overlay Mode","79","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","KeywordEnum","3","None","Haftone","Hatching","Create","True","True","All","9","1","COLOR","0,0,0,0","False","0","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","4","COLOR","0,0,0,0","False","5","COLOR","0,0,0,0","False","6","COLOR","0,0,0,0","False","7","COLOR","0,0,0,0","False","8","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":220,"pos":[-5679.071,5035.036],"params":["Inherit","False","4","4","0","FLOAT3","1,1,1","False","1","FLOAT","0","False","2","FLOAT","0","False","3","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":715,"pos":[1165.498,-681.5406],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor","id":481,"pos":[-2169.376,2599.59],"params":["Inherit","True","Property","_Hatch1","Hatch Texture Darker","61","1","[NoScaleOffset]","Create","False","0","0","0","False","0","False","","None","None","False","white","Auto","Texture2D","False","-1","0","2","SAMPLER2D","0","SAMPLERSTATE","1"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":644,"pos":[-6819.08,5576.433],"params":["Inherit","False","638","Smoothness","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":463,"pos":[474.3687,-786.8387],"params":["Inherit","False","Property","_RimSmoothness","Rim Smoothness","29","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0.5","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1363,"pos":[-3200.625,1032.195],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT3","0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.DistanceOpNode, AmplifyShaderEditor","id":498,"pos":[-774.9536,3369.153],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1236,"pos":[591.3068,4597.426],"params":["Inherit","False","HalftoneDiffuseShadowMask","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":642,"pos":[-4138.578,1020.884],"params":["Inherit","False","364","MainTexture","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor","id":482,"pos":[-2170.969,2780.633],"params":["Inherit","True","Property","_Hatch2","Hatch Texture Brighter","60","1","[NoScaleOffset]","Create","False","0","0","0","False","0","False","","None","None","False","white","Auto","Texture2D","False","-1","0","2","SAMPLER2D","0","SAMPLERSTATE","1"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1783,"pos":[1170.956,4503.854],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1053,"pos":[-1393.442,2619.551],"params":["Inherit","False","1051","OverlayUVs","1","0","OBJECT","","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1269,"pos":[-2978.034,1065.561],"params":["Inherit","False","IndirectDiffuseLight","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.ClampOpNode, AmplifyShaderEditor","id":753,"pos":[2041.405,-41.32713],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1.1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":537,"pos":[510.0921,-1058.331],"params":["Inherit","False","1538","BNCurrentNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1774,"pos":[1302.066,4170.942],"params":["Inherit","True","3","0","COLOR","0,0,0,0","False","1","COLOR","1,1,1,0","False","2","FLOAT","1","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":457,"pos":[458.4457,-880.3952],"params":["Inherit","False","Property","_RimPower","Rim Power","28","0","Create","True","0","0","0","False","0","False","Object","-1","","2","1","1","12","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1238,"pos":[-4659.746,-28.08583],"params":["Inherit","False","1236","HalftoneDiffuseShadowMask","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":623,"pos":[-6459.219,5675.479],"params":["Inherit","False","Property","_Strength","Strength","38","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":740,"pos":[2165.052,202.1731],"params":["Inherit","False","738","FresnelValue","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1465,"pos":[1276.373,4608.949],"params":["Inherit","False","1269","IndirectDiffuseLight","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.ScreenParams, AmplifyShaderEditor","id":1855,"pos":[-1917.826,2214.864],"params":["Inherit","False","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.IndirectSpecularLight, AmplifyShaderEditor","id":618,"pos":[-6310.735,5485.579],"params":["Inherit","False","World","3","0","FLOAT3","0,0,1","False","1","FLOAT","1","False","2","FLOAT","0.75","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":491,"pos":[-985.7573,2994.764],"params":["Inherit","False","\tfloat log2_dist = log2(_dist)-0.2;\n\t\n\tfloat2 floored_log_dist = floor( (log2_dist + float2(0.0, 1.0) ) * 0.5) *2.0 - float2(0.0, 1.0);\t\t\t\t\n\tfloat2 uv_scale = min(_MaxScaleDependingOnCamera, pow(2.0, floored_log_dist));\n\t\n\tfloat uv_blend = abs(frac(log2_dist * 0.5) * 2.0 - 1.0);\n\t\n\tfloat2 scaledUVA = _uv / uv_scale.x; // 16\n\tfloat2 scaledUVB = _uv / uv_scale.y; // 8 \n\n\tfloat3 hatch0A = tex2D(_Hatch0, scaledUVA).rgb;\n\tfloat3 hatch1A = tex2D(_Hatch1, scaledUVA).rgb;\n\n\tfloat3 hatch0B = tex2D(_Hatch0, scaledUVB).rgb;\n\tfloat3 hatch1B = tex2D(_Hatch1, scaledUVB).rgb;\n\n\tfloat3 hatch0 = lerp(hatch0A, hatch0B, uv_blend);\n\tfloat3 hatch1 = lerp(hatch1A, hatch1B, uv_blend);\n\n\tfloat3 overbright = max(0, _intensity - 1.0);\n\n\tfloat3 weightsA = saturate((_intensity * 6.0) + float3(-0, -1, -2));\n\tfloat3 weightsB = saturate((_intensity * 6.0) + float3(-3, -4, -5));\n\n\tweightsA.xy -= weightsA.yz;\n\tweightsA.z -= weightsB.x;\n\tweightsB.xy -= weightsB.yz;\n\n\thatch0 = hatch0 * weightsA;\n\thatch1 = hatch1 * weightsB;\n\n\tfloat3 hatching = overbright + hatch0.r +\n\t\thatch0.g + hatch0.b +\n\t\thatch1.r + hatch1.g +\n\t\thatch1.b;\n\n\treturn hatching;","3","Create","6","False","_uv","FLOAT2","0,0","In","","Inherit","False","False","_intensity","FLOAT","0","In","","Inherit","False","False","_Hatch0","SAMPLER2D","","In","","Inherit","False","False","_Hatch1","SAMPLER2D","","In","","Inherit","False","False","_dist","FLOAT","0","In","","Inherit","False","True","_MaxScaleDependingOnCamera","FLOAT","0","In","","Inherit","False","HatchingConstantScale","True","False","0","","False","6","0","FLOAT2","0,0","False","1","FLOAT","0","False","2","SAMPLER2D","","False","3","SAMPLER2D","","False","4","FLOAT","0","False","5","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":727,"pos":[1247.561,-468.5357],"params":["Inherit","False","Property","_BacklightHardness","Backlight Hardness","14","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","0.5","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":716,"pos":[1409.204,-599.4019],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":466,"pos":[820.7245,-839.5659],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":458,"pos":[465.8676,-974.0062],"params":["Inherit","False","Property","_RimThickness","Rim Thickness","27","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","3","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":476,"pos":[-1077.271,2633.342],"params":["Inherit","False","float intensity = color;\n\n    float3 hatch0 = tex2D(_Hatch0, _uv).rgb;\n    float3 hatch1 = tex2D(_Hatch1, _uv).rgb;\n\n    float3 overbright = max(0, intensity - 1.0);\n\n    float3 weightsA = saturate((intensity * 6.0) + float3(-0, -1, -2));\n    float3 weightsB = saturate((intensity * 6.0) + float3(-3, -4, -5));\n\n    weightsA.xy -= weightsA.yz;\n    weightsA.z -= weightsB.x;\n    weightsB.xy -= weightsB.yz;\n\n    hatch0 = hatch0 * weightsA;\n    hatch1 = hatch1 * weightsB;\n\n    float3 hatching = overbright + hatch0.r +\n        hatch0.g + hatch0.b +\n        hatch1.r + hatch1.g +\n        hatch1.b;\n\n    return hatching;\n    return hatching;","3","Create","4","False","_uv","FLOAT2","0,0","In","","Inherit","False","True","color","FLOAT","0","In","","Inherit","False","False","_Hatch0","SAMPLER2D","","In","","Inherit","False","False","_Hatch1","SAMPLER2D","","In","","Inherit","False","Hatching","True","False","0","","False","4","0","FLOAT2","0,0","False","1","FLOAT","0","False","2","SAMPLER2D","","False","3","SAMPLER2D","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":693,"pos":[-4625.304,-155.1281],"params":["Inherit","False","391","BNDiffuse","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":229,"pos":[-3574.793,815.2498],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMaxOpNode, AmplifyShaderEditor","id":1886,"pos":[-5504,5008],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1790,"pos":[-1930.129,2107.977],"params":["Inherit","False","Property","_PaperTilling","Paper Tiling","113","0","Create","False","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":726,"pos":[1589.999,-580.5162],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0.5","False","2","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":263,"pos":[-3059.157,798.1735],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":762,"pos":[2476.47,-39.55762],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":760,"pos":[2266.413,297.66],"params":["Inherit","False","Property","_SideShineHardness","Side Shine Hardness","13","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","0.5","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1237,"pos":[-4398.406,-124.581],"params":["Inherit","False","Property","_OverlayMode","Overlay Mode","79","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","KeywordEnum","3","None","Haftone","Hatching","Create","True","True","All","9","1","COLOR","0,0,0,0","False","0","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","4","COLOR","0,0,0,0","False","5","COLOR","0,0,0,0","False","6","COLOR","0,0,0,0","False","7","COLOR","0,0,0,0","False","8","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":467,"pos":[1004.604,-900.3163],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FresnelNode, AmplifyShaderEditor","id":454,"pos":[757.5737,-1049.152],"params":["Inherit","False","Standard","WorldNormal","ViewDir","True","True","5","0","FLOAT3","0,0,1","False","4","FLOAT3","0,0,0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1226,"pos":[1529.925,4309.969],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":661,"pos":[-5980.348,5521.406],"params":["Inherit","False","3","3","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":490,"pos":[-719.8448,2737.505],"params":["Inherit","False","Property","_UseHatchingConstantScale","Hatching Constant Scale","63","0","Create","False","0","0","0","False","0","False","","0","1","1","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT3","0,0,0","False","0","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT3","0,0,0","False","5","FLOAT3","0,0,0","False","6","FLOAT3","0,0,0","False","7","FLOAT3","0,0,0","False","8","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":1858,"pos":[-1669.666,2175.111],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":627,"pos":[-5408,4672],"params":["Inherit","False","Property","_UseSpecular","UseSpecular Highlights","2","0","Create","False","0","0","0","True","0","False","","0","1","1","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","COLOR","0,0,0,0","False","0","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","4","COLOR","0,0,0,0","False","5","COLOR","0,0,0,0","False","6","COLOR","0,0,0,0","False","7","COLOR","0,0,0,0","False","8","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":696,"pos":[-4517.71,204.9207],"params":["Inherit","False","Property","_SpecularShadowMask","Specular Shadow Mask","46","0","Create","True","0","0","0","False","0","False","Object","-1","","0","1","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":756,"pos":[1672.788,-980.4533],"params":["Inherit","False","Property","_BacklightIntensity","Backlight Intensity","51","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","4","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1466,"pos":[-3047.77,1230.248],"params":["Inherit","False","IndirectHatching","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":621,"pos":[-5829.478,5520.319],"params":["Inherit","False","Property","_UseEnvironmentRefletion","UseEnvironment Reflections","37","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT3","0,0,0","False","0","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT3","0,0,0","False","5","FLOAT3","0,0,0","False","6","FLOAT3","0,0,0","False","7","FLOAT3","0,0,0","False","8","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1022,"pos":[1575.399,4670.469],"params":["Inherit","False","Halftone","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":239,"pos":[-2802.317,769.5178],"params":["Float","True","BNFinalDiffuse","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1265,"pos":[-315.796,2787.393],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":755,"pos":[1768.169,-723.2394],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":462,"pos":[1232.969,-1000.956],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0.5","False","2","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":758,"pos":[1817.283,-552.0821],"params":["Inherit","False","Property","_SideShineIntensity","Side Shine Intensity","53","0","Create","False","0","0","0","False","0","False","Object","-1","","1","0","0","4","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":759,"pos":[2630.551,-58.32035],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0.5","False","2","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1856,"pos":[-1534.746,2176.987],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TFHCGrayscale, AmplifyShaderEditor","id":1797,"pos":[-4153.83,-58.39511],"params":["Inherit","False","1","1","0","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":243,"pos":[-5386.463,4436.045],"params":["Float","False","BNspecularFinalColor","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1469,"pos":[-2124.462,1991.161],"params":["Inherit","False","Property","_IndirectLightAffectOnHatch","Indirect Light Affect On Hatch","98","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":718,"pos":[1902.301,-880.9074],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":271,"pos":[-4628.169,-376.4447],"params":["Inherit","False","239","BNFinalDiffuse","1","0","OBJECT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1232,"pos":[-4575.157,-275.6162],"params":["Inherit","False","1022","Halftone","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1364,"pos":[-5461.899,5581.855],"params":["Inherit","False","IndirectSpecular","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":738,"pos":[1419.672,-986.2852],"params":["Inherit","False","FresnelValue","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":695,"pos":[-3958.377,7.337563],"params":["Inherit","False","3","0","FLOAT","1","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1467,"pos":[-2134.365,1863.206],"params":["Inherit","False","1466","IndirectHatching","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":757,"pos":[2122.584,-634.8906],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":270,"pos":[-4165.508,-150.3121],"params":["Inherit","False","243","BNspecularFinalColor","1","0","OBJECT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1025,"pos":[-267.1698,2626.978],"params":["Inherit","False","Hatching","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GrabScreenPosition, AmplifyShaderEditor","id":1791,"pos":[-1492.132,1926.313],"params":["Inherit","False","0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1857,"pos":[-1387.452,2162.439],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":694,"pos":[-3814.204,-108.875],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1366,"pos":[-3538.575,-113.5741],"params":["Inherit","False","1364","IndirectSpecular","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1644,"pos":[2679.005,-656.2682],"params":["Inherit","False","1554","BNDiffuseNoAdditionalLights","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1231,"pos":[-4198.647,-370.1738],"params":["Inherit","False","Property","_OverlayMode","Overlay Mode","77","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","KeywordEnum","3","None","Haftone","Hatching","Create","True","True","All","9","1","COLOR","0,0,0,0","False","0","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","4","COLOR","0,0,0,0","False","5","COLOR","0,0,0,0","False","6","COLOR","0,0,0,0","False","7","COLOR","0,0,0,0","False","8","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.TFHCGrayscale, AmplifyShaderEditor","id":1648,"pos":[2883.622,-509.4114],"params":["Inherit","False","0","1","0","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1026,"pos":[-1958.094,1282.58],"params":["Inherit","False","1025","Hatching","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":739,"pos":[1998.822,-1050.442],"params":["Inherit","False","738","FresnelValue","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":1645,"pos":[2840.679,-393.1641],"params":["Inherit","False","Property","_RimShadowColor","Rim Shadow Color","105","0","Create","False","0","0","0","False","0","False","Object","-1","","0,0.05551431,0.9622642,0","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":731,"pos":[2120.477,-821.2676],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":1253,"pos":[-2070.124,1585.938],"params":["Inherit","False","Property","_HatchingColor","Hatching Color","78","0","Create","False","0","0","0","False","0","False","Object","-1","","0,0,0,1","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1468,"pos":[-1880.071,1840.766],"params":["Inherit","False","3","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1789,"pos":[-1220.61,2037.258],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","FLOAT2","0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":1555,"pos":[-1762.134,1299.336],"params":["Inherit","False","FLOAT3","1","0","FLOAT3","0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":456,"pos":[2734.758,-872.3867],"params":["Inherit","False","Property","_RimColor","Rim Color","26","1","[HDR]","Create","True","0","0","0","False","0","False","Object","-1","","1,1,1,0","1,1,1,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1647,"pos":[2982.141,-773.8771],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":732,"pos":[2221.038,-1035.235],"params":["Inherit","False","Property","_UseBacklight","Rim As Backlight & Side Shine","52","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT","0","False","0","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","5","FLOAT","0","False","6","FLOAT","0","False","7","FLOAT","0","False","8","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1650,"pos":[3146.622,-579.4114],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1270,"pos":[-1801.1,1520.373],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT3","0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":259,"pos":[-3353.517,-351.2742],"params":["Inherit","False","3","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT3","0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":679,"pos":[1461,2580.218],"params":["Inherit","False","Property","_EmissionColor","Emission Color","42","1","[HDR]","Create","True","0","0","0","False","0","False","Object","-1","","0,0,0,0","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":682,"pos":[1351.523,2385.05],"params":["Inherit","True","Property","_EmissionMap","Emission Map","43","3","[HDR]","[NoScaleOffset]","[SingleLineTexture]","Create","True","0","0","0","False","0","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":1795,"pos":[-974.3113,1902.741],"params":["Inherit","True","Property","_PaperTexture","Paper Texture","114","1","[NoScaleOffset]","Create","True","0","0","0","False","0","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1264,"pos":[-1482.546,1247.387],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","1,1,1,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":686,"pos":[1827.266,2502.482],"params":["Inherit","False","Property","_UseEmission","UseEmission","44","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":677,"pos":[1771.378,2591.815],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1556,"pos":[-1463.134,1409.336],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":274,"pos":[-3140.385,-357.7659],"params":["Inherit","False","BNBlinnPhongLightning","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1646,"pos":[3250.362,-717.5472],"params":["Inherit","False","Property","_RimSplitColor","Rim Split Color","106","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","KeywordEnum","3","NoSplit","MultiplyWithDiffuse","UseSecondColor","Create","True","True","All","9","1","COLOR","0,0,0,0","False","0","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","4","COLOR","0,0,0,0","False","5","COLOR","0,0,0,0","False","6","COLOR","0,0,0,0","False","7","COLOR","0,0,0,0","False","8","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":464,"pos":[2662.978,-1035.445],"params":["Inherit","False","Property","_UseRimLight","UseRim Light","0","0","Create","True","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT","0","False","0","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","5","FLOAT","0","False","6","FLOAT","0","False","7","FLOAT","0","False","8","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1254,"pos":[-1553.623,1562.387],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","1,1,1,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":1256,"pos":[-1518.766,1779.329],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Compare, AmplifyShaderEditor","id":685,"pos":[2006.266,2608.482],"params":["Inherit","False","0","4","0","FLOAT","0","False","1","FLOAT","1","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1642,"pos":[3442.544,-824.1395],"params":["Inherit","False","RimColor","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":460,"pos":[2924.15,-1018.172],"params":["Inherit","False","RimLight","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1257,"pos":[-1332.921,1677.669],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","1,1,1,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1260,"pos":[-1209.246,1245.788],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","1,1,1,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":530,"pos":[-1778.019,976.3893],"params":["Inherit","False","274","BNBlinnPhongLightning","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":680,"pos":[2194.921,2612.172],"params":["Inherit","False","Emission","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1643,"pos":[-1137.734,830.2725],"params":["Inherit","False","1642","RimColor","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1636,"pos":[-1215.137,709.5232],"params":["Inherit","False","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":461,"pos":[-1063.669,1038.62],"params":["Inherit","False","460","RimLight","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1258,"pos":[-1192.877,1376.174],"params":["Inherit","False","Property","_UsePureSketch","Pure Sketch","80","0","Create","False","0","0","0","False","0","False","","0","1","1","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","COLOR","0,0,0,0","False","0","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","4","COLOR","0,0,0,0","False","5","COLOR","0,0,0,0","False","6","COLOR","0,0,0,0","False","7","COLOR","0,0,0,0","False","8","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":362,"pos":[1404.155,1877.507],"params":["Inherit","True","Property","_MainTex","Albedo Texture","11","0","Create","False","0","0","0","False","0","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1637,"pos":[-878.1372,1225.523],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":681,"pos":[-773.323,1053.889],"params":["Inherit","False","680","Emission","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1635,"pos":[-822.1129,860.0081],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1449,"pos":[-597.9544,675.8444],"params":["Inherit","False","Property","_OutlineTextureStrength","Texture Strength ","95","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":670,"pos":[46.10808,1250.098],"params":["Inherit","False","364","MainTexture","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":44,"pos":[-272.0464,388.2114],"params":["Inherit","False","Property","_OutlineColor","Outline Color","7","1","[HDR]","Create","True","0","0","0","True","0","False","Object","-1","","0,0,0,0","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1249,"pos":[-688.2852,1269.027],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":282,"pos":[-438.3608,945.2823],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1448,"pos":[-252.7794,586.307],"params":["Inherit","False","3","0","COLOR","1,1,1,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":64,"pos":[-1880.329,-546.7816],"params":["Inherit","False","1478.969","931.5795","","24","593","596","50","592","57","52","54","53","55","51","56","590","60","62","58","591","59","598","600","599","601","602","604","606","Outline","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":387,"pos":[1643.036,755.7413],"params":["Inherit","False","338","166","","1","373","Editor Properties","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1829,"pos":[-7940.005,2850.515],"params":["Inherit","False","285","161","do not delete","1","1897","","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":671,"pos":[280.2685,1150.97],"params":["Inherit","False","COLOR","1","0","COLOR","0,0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1421,"pos":[-45.53701,757.0069],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1024,"pos":[-208,960],"params":["Inherit","False","Property","_OverlayMode","Overlay Mode","78","0","Create","True","0","0","0","False","0","False","","0","0","0","True","","KeywordEnum","3","None","Haftone","Hatching","Create","True","True","All","9","1","COLOR","0,0,0,0","False","0","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","4","COLOR","0,0,0,0","False","5","COLOR","0,0,0,0","False","6","COLOR","0,0,0,0","False","7","COLOR","0,0,0,0","False","8","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.StickyNoteNode, AmplifyShaderEditor","id":1830,"pos":[-212.1996,1143.355],"params":["Inherit","False","233","107","Passes note","","1,1,1,1","Outline goes to pass 0 and main color - pass1","0","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":604,"pos":[-1024.31,-59.87411],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":591,"pos":[-1659.262,-259.775],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.TimeNode, AmplifyShaderEditor","id":598,"pos":[-1837.066,-507.4237],"params":["Inherit","False","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":599,"pos":[-1846.061,-365.7374],"params":["Inherit","False","Property","_OutlineDynamicSpeed","Outline Dynamic Speed","35","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","0.05","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NormalVertexDataNode, AmplifyShaderEditor","id":52,"pos":[-1850.069,191.2531],"params":["Inherit","False","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":602,"pos":[-1381.9,-345.7536],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DistanceOpNode, AmplifyShaderEditor","id":55,"pos":[-929.3464,-380.4816],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":1604,"pos":[-4908.307,2566.073],"params":["Inherit","False","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":606,"pos":[-870.0676,46.70271],"params":["Inherit","False","Property","_UseDynamicOutline","Use Sketch Outline","36","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT3","0,0,0","False","0","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT3","0,0,0","False","5","FLOAT3","0,0,0","False","6","FLOAT3","0,0,0","False","7","FLOAT3","0,0,0","False","8","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":56,"pos":[-1013.993,-179.219],"params":["Inherit","False","Property","_AdaptiveThicnkess","Adaptive Thickness","5","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0.32","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":62,"pos":[-552.3168,-51.57129],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":600,"pos":[-1471.603,-471.4403],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":54,"pos":[-1444.329,249.3617],"params":["Inherit","False","3","2","0","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":592,"pos":[-1183.824,-103.8956],"params":["Inherit","False","2","2","0","FLOAT","1","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":51,"pos":[-1108.915,-350.8623],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":673,"pos":[420.2685,1256.97],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":601,"pos":[-1733.868,-277.1592],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldSpaceCameraPos, AmplifyShaderEditor","id":50,"pos":[-1180.349,-497.7589],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":593,"pos":[-1634.274,-144.02],"params":["Inherit","False","Property","_OutlineNoiseScale","Outline Noise Scale","33","0","Create","False","0","0","0","False","0","False","Object","-1","","10","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":57,"pos":[-1367.279,63.58631],"params":["Inherit","False","Property","_OutlineType","Outline Type","6","0","Create","True","0","0","0","False","0","False","","0","0","0","True","","KeywordEnum","3","Normal","Position","UVBaked","Create","True","True","All","9","1","FLOAT3","0,0,0","False","0","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT3","0,0,0","False","5","FLOAT3","0,0,0","False","6","FLOAT3","0,0,0","False","7","FLOAT3","0,0,0","False","8","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":60,"pos":[-593.3286,72.36163],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.NoiseGeneratorNode, AmplifyShaderEditor","id":590,"pos":[-1410.021,-165.775],"params":["Inherit","False","Simplex2D","True","False","2","0","FLOAT2","0,0","False","1","FLOAT","15.43","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":58,"pos":[-841.3283,219.3617],"params":["Inherit","False","Property","_Thicnkess","Thickness","4","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","0.1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":53,"pos":[-1660.237,206.434],"params":["Inherit","False","0","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1350,"pos":[268.6716,661.877],"params":["Inherit","False","Constant","_Float6","Float 6","111","0","Create","True","0","0","0","False","0","False","Object","-1","","-2","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":59,"pos":[-713.4068,-214.2483],"params":["Inherit","False","3","0","FLOAT","1","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":373,"pos":[1693.036,805.7413],"params":["Inherit","False","Property","_RampDiffuseTextureLoaded","RampDiffuseTextureLoaded","17","1","[HideInInspector]","Create","True","0","0","0","True","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1611,"pos":[-4620.763,2489.308],"params":["Inherit","False","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1599,"pos":[-5111.077,2598.644],"params":["Inherit","False","1415","ShadowAtten","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1349,"pos":[424.3337,676.1953],"params":["Inherit","False","Property","_UseOutline","UseOutline","1","0","Create","False","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT","0","False","0","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","5","FLOAT","0","False","6","FLOAT","0","False","7","FLOAT","0","False","8","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":365,"pos":[341.8629,510.9032],"params":["Inherit","False","Property","_UseOutline","UseOutline","1","0","Create","True","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT3","0,0,0","False","0","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT3","0,0,0","False","5","FLOAT3","0,0,0","False","6","FLOAT3","0,0,0","False","7","FLOAT3","0,0,0","False","8","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor","id":737,"pos":[2469.855,113.0997],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1354,"pos":[-1429.461,4606.334],"params":["Inherit","False","Property","_Inverse","Inverse","91","0","Create","True","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT","0","False","0","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","5","FLOAT","0","False","6","FLOAT","0","False","7","FLOAT","0","False","8","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1600,"pos":[-4935.629,2686.68],"params":["Inherit","False","250","BNLightWarpVector","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":674,"pos":[441.585,1038.581],"params":["Float","False","Property","_Cutoff","Alpha Clip Threshold","41","0","Create","False","0","0","0","False","0","False","Object","-1","","0","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1605,"pos":[-4745.212,2537.469],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":596,"pos":[-1602.23,-57.26891],"params":["Inherit","False","Property","_OutlineNoiseIntesity","Outline Noise Intensity","34","0","Create","False","0","0","0","False","0","False","Object","-1","","1","0","0","2","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":1878,"pos":[-7526.157,3392.087],"params":["Inherit","False","float3 Color = 0;\n\nint numLights = GetAdditionalLightsCount();\nfor(int i = 0; i<numLights;i++)\n{\n\t\n\t\t\t#if VERSION_GREATER_EQUAL(10, 1)\n\t\t\tLight light = GetAdditionalLight(i, WorldPosition, shadowmask);\n\t\t\t// see AdditionalLights_float for explanation of this\n\t\t#else\n\t\t\tLight light = GetAdditionalLight(i, WorldPosition);\n\t\t#endif\n\n\t\n\tfloat3 DotVector = dot(light.direction,WorldNormal);\n\t\n\n\thalf3 AttLightColor = (light.shadowAttenuation * light.distanceAttenuation);\n\t float3 colout = max(float3(0.f,0.f,0.f),LightWrapVector + (1-LightWrapVector) * DotVector )*AttLightColor*light.color; \n\tfloat maxColor = max(colout.r,max(colout.g,colout.b));\n\tfloat3 outColor = smoothstep(SMin,SMax,maxColor)*light.color;\n\t Color += outColor;\n\t//Color += smoothstep(float3(Faloff,Faloff,Faloff),float3(0.5f,0.5f,0.5f),colout);\n}\n\nreturn Color;","3","Create","7","True","WorldPosition","FLOAT3","0,0,0","In","","Float","False","True","WorldNormal","FLOAT3","0,0,0","In","","Float","False","True","LightWrapVector","FLOAT3","0,0,0","In","","Inherit","False","True","SMin","FLOAT","0","In","","Inherit","False","True","SMax","FLOAT","0","In","","Inherit","False","True","Faloff","FLOAT","0","In","","Inherit","False","True","shadowmask","FLOAT4","1,1,1,1","In","","Inherit","False","AdditionalLight","False","False","0","","False","7","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT","0","False","4","FLOAT","0","False","5","FLOAT","0","False","6","FLOAT4","1,1,1,1","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1897,"pos":[-7903.005,2911.515],"params":["Inherit","False","SRP Additional Light","-1","","61154","6c86746ad131a0a408ca599df5f40861","3,6,1,351,0,23,1","6","2","FLOAT3","0,0,0","False","11","FLOAT3","0,0,0","False","345","FLOAT3","0,0,0","False","346","FLOAT3","0,0,0","False","347","FLOAT","0.5","False","32","FLOAT4","0,0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassSwitchNode, AmplifyShaderEditor","id":42,"pos":[117.9438,783.0602],"params":["Inherit","False","0","0","13","13","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","4","COLOR","0,0,0,0","False","5","COLOR","0,0,0,0","False","6","COLOR","0,0,0,0","False","7","COLOR","0,0,0,0","False","8","COLOR","0,0,0,0","False","9","COLOR","0,0,0,0","False","10","COLOR","0,0,0,0","False","11","COLOR","0,0,0,0","False","12","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1868,"pos":[810.7261,969.3201],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","13","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","ShadowCaster","0","2","ShadowCaster","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","True","False","False","False","False","0","False","","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=ShadowCaster","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1869,"pos":[810.7261,969.3201],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","13","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","DepthOnly","0","3","DepthOnly","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","True","False","False","False","False","0","False","","False","False","False","False","False","False","False","False","False","True","1","False","","False","False","False","True","1","LightMode=DepthOnly","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1870,"pos":[810.7261,969.3201],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","13","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","Meta","0","4","Meta","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=Meta","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1871,"pos":[810.7261,969.3201],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","13","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","Universal2D","0","5","Universal2D","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=Universal2D","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1872,"pos":[810.7261,969.3201],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","13","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","SceneSelectionPass","0","6","SceneSelectionPass","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=SceneSelectionPass","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1873,"pos":[810.7261,969.3201],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","13","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","ScenePickingPass","0","7","ScenePickingPass","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=Picking","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1874,"pos":[810.7261,969.3201],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","13","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","DepthNormals","0","8","DepthNormals","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=DepthNormalsOnly","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1875,"pos":[810.7261,969.3201],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","13","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","DepthNormalsOnly","0","9","DepthNormalsOnly","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=DepthNormalsOnly","False","True","11","d3d11","metal","vulkan","xboxone","xboxseries","playstation","ps4","ps5","switch","switch2","webgpu","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1889,"pos":[1072,1108],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","MotionVectors","0","10","MotionVectors","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","False","False","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=MotionVectors","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1890,"pos":[1072,1108],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","XRMotionVectors","0","11","XRMotionVectors","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","True","1","False","","255","False","","1","False","","7","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","False","False","False","False","True","1","LightMode=XRMotionVectors","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1891,"pos":[1072,1108],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","GBuffer","0","12","GBuffer","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=UniversalGBuffer","False","True","12","d3d11","gles","metal","vulkan","xboxone","xboxseries","playstation","ps4","ps5","switch","switch2","webgpu","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1866,"pos":[794.142,646.0062],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","19","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","ExtraPrePass","0","0","ExtraPrePass","6","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","1","False","","True","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","True","True","1","False","","True","3","False","","True","False","0","False","","0","False","","False","True","0","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1867,"pos":[1072,1008],"params":["Float","False","True","-1","2","ToonProEditor","0","19","Stylized Toon","2992e84f91cbeb14eab234972e07ea9d","True","Forward","0","1","Forward","12","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=UniversalForwardOnly","False","False","14","Include","","False","","Native","False","0","0","","Pragma","multi_compile _ _LIGHT_COOKIES ","False","","Custom","True","0","0","Forward","Pragma","multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN","False","","Custom","True","0","0","Forward","Pragma","multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS","False","","Custom","True","0","0","Forward","Pragma","multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS","False","","Custom","True","0","0","Forward","Pragma","multi_compile_fragment _ _REFLECTION_PROBE_BLENDING","False","","Custom","True","0","0","Forward","Pragma","multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION","False","","Custom","True","0","0","Forward","Pragma","multi_compile_fragment _ _REFLECTION_PROBE_ATLAS","False","","Custom","True","0","0","Forward","Pragma","multi_compile _ REFLECTION_PROBE_ROTATION","False","","Custom","True","0","0","Forward","Pragma","multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH","False","","Custom","True","0","0","Forward","Pragma","multi_compile _ LIGHTMAP_SHADOW_MIXING","False","","Custom","True","0","0","Forward","Pragma","multi_compile _ SHADOWS_SHADOWMASK","False","","Custom","True","0","0","Forward","Pragma","multi_compile_fragment _ _LIGHT_LAYERS","False","","Custom","True","0","0","Forward","Pragma","multi_compile _ _CLUSTER_LIGHT_LOOP","False","","Custom","True","0","0","Forward","","0","0","Standard","33","Surface","0","0","  Keep Alpha","0","0","  Blend","0","0","Two Sided","1","0","Alpha Clipping","0","0","  Use Shadow Threshold","0","0","Fragment Normal Space","0","0","Forward Only","0","0","Cast Shadows","1","0","Receive Shadows","1","0","Receive SSAO","1","0","Default Decal Blending","1","0","Motion Vectors","1","0","  Additional Motion Vectors","1","0","  Alembic Motion Vectors","0","0","  XR Motion Vectors","0","0","GPU Instancing","1","0","LOD CrossFade","1","638251859426275030","Built-in Fog","0","639065134847256314","Meta Pass","0","638251859496447722","Extra Pre Pass","1","638251859501580475","Tessellation","0","0","  Phong","0","0","  Strength","0.5,False,","0","  Type","0","0","  Tess","16,False,","0","  Min","10,False,","0","  Max","25,False,","0","  Edge Length","16,False,","0","  Max Displacement","25,False,","0","Write Depth","0","0","  Conservative","0","0","Vertex Position","1","0","0","13","True","True","True","True","False","False","True","True","True","False","True","False","True","False","","False","0"]}
{"wire":[1525,0,1529,0]}
{"wire":[1845,0,1844,1]}
{"wire":[1845,1,1844,2]}
{"wire":[1518,0,1517,0]}
{"wire":[1526,0,1525,0]}
{"wire":[1846,0,381,0]}
{"wire":[1846,1,1845,0]}
{"wire":[1519,0,1518,0]}
{"wire":[1521,0,1518,0]}
{"wire":[1528,0,1526,0]}
{"wire":[1847,0,1846,0]}
{"wire":[1847,1,381,0]}
{"wire":[1523,0,1524,0]}
{"wire":[1520,0,1519,0]}
{"wire":[1520,1,1521,0]}
{"wire":[1668,0,1794,0]}
{"wire":[1668,1,1847,0]}
{"wire":[1527,0,1528,0]}
{"wire":[1770,7,381,0]}
{"wire":[1531,0,1527,0]}
{"wire":[1531,1,1527,0]}
{"wire":[1541,1,1669,0]}
{"wire":[1522,0,1523,0]}
{"wire":[1522,1,1520,0]}
{"wire":[1664,1,1668,0]}
{"wire":[1664,0,1770,0]}
{"wire":[1530,0,1664,0]}
{"wire":[1530,1,1531,0]}
{"wire":[1530,2,1522,0]}
{"wire":[1536,0,1540,0]}
{"wire":[1536,1,1541,0]}
{"wire":[1536,2,1539,0]}
{"wire":[1495,0,381,0]}
{"wire":[1495,1,1522,0]}
{"wire":[1488,0,1496,0]}
{"wire":[1537,0,1536,0]}
{"wire":[1514,1,1495,0]}
{"wire":[1514,0,1530,0]}
{"wire":[1538,0,1537,0]}
{"wire":[1489,0,1514,0]}
{"wire":[1489,1,1492,0]}
{"wire":[1489,2,1488,0]}
{"wire":[1814,0,1813,0]}
{"wire":[1815,0,1814,0]}
{"wire":[375,1,1489,0]}
{"wire":[1843,0,1840,1]}
{"wire":[1843,1,1840,2]}
{"wire":[1841,0,439,0]}
{"wire":[1841,1,1843,0]}
{"wire":[1816,0,1815,0]}
{"wire":[378,0,375,1]}
{"wire":[378,1,379,0]}
{"wire":[1278,0,1285,0]}
{"wire":[234,0,1542,0]}
{"wire":[234,1,269,0]}
{"wire":[1279,0,1278,0]}
{"wire":[1817,0,1816,0]}
{"wire":[233,0,234,0]}
{"wire":[1851,0,1848,1]}
{"wire":[1851,1,1848,2]}
{"wire":[384,0,382,0]}
{"wire":[384,2,378,0]}
{"wire":[1842,0,1841,0]}
{"wire":[1842,1,439,0]}
{"wire":[1807,0,1805,0]}
{"wire":[1293,0,1292,0]}
{"wire":[1769,7,439,0]}
{"wire":[1280,0,1279,0]}
{"wire":[1129,0,1842,0]}
{"wire":[1129,1,1793,0]}
{"wire":[248,0,249,0]}
{"wire":[250,0,384,0]}
{"wire":[1850,0,1056,0]}
{"wire":[1850,1,1851,0]}
{"wire":[1812,1,1817,0]}
{"wire":[1812,0,1817,0]}
{"wire":[1812,2,1817,0]}
{"wire":[1412,1,1414,0]}
{"wire":[227,0,247,1]}
{"wire":[227,1,1412,3]}
{"wire":[1804,0,1807,0]}
{"wire":[1818,0,1812,0]}
{"wire":[1818,1,1812,0]}
{"wire":[1806,0,1807,0]}
{"wire":[1665,1,1129,0]}
{"wire":[1665,0,1769,0]}
{"wire":[1296,0,1293,0]}
{"wire":[1295,0,1293,0]}
{"wire":[1281,0,1280,0]}
{"wire":[251,0,248,0]}
{"wire":[251,1,223,0]}
{"wire":[1849,0,1850,0]}
{"wire":[1849,1,1056,0]}
{"wire":[486,0,1849,0]}
{"wire":[486,1,1792,0]}
{"wire":[1287,0,1297,0]}
{"wire":[1294,0,1295,0]}
{"wire":[1294,1,1296,0]}
{"wire":[267,0,389,0]}
{"wire":[267,1,251,0]}
{"wire":[1123,0,439,0]}
{"wire":[1811,0,1809,0]}
{"wire":[1277,0,1281,0]}
{"wire":[1277,2,1281,0]}
{"wire":[1808,0,1806,0]}
{"wire":[1808,1,1804,0]}
{"wire":[1501,0,1502,0]}
{"wire":[1501,1,1504,0]}
{"wire":[1771,7,1056,0]}
{"wire":[244,0,227,0]}
{"wire":[1859,0,1665,0]}
{"wire":[1859,1,1818,0]}
{"wire":[1132,1,1123,0]}
{"wire":[1132,0,1859,0]}
{"wire":[1500,0,1501,0]}
{"wire":[1500,1,1505,0]}
{"wire":[1500,2,1508,0]}
{"wire":[1810,0,1811,0]}
{"wire":[1810,1,1808,0]}
{"wire":[1288,0,1287,0]}
{"wire":[1288,1,1294,0]}
{"wire":[262,0,267,0]}
{"wire":[262,1,264,0]}
{"wire":[1803,0,1277,0]}
{"wire":[1803,1,1277,0]}
{"wire":[222,0,226,1]}
{"wire":[1418,0,260,0]}
{"wire":[1418,1,1417,0]}
{"wire":[1661,1,486,0]}
{"wire":[1661,0,1771,0]}
{"wire":[1562,0,1202,0]}
{"wire":[1127,0,699,0]}
{"wire":[260,0,262,0]}
{"wire":[260,1,240,0]}
{"wire":[237,0,222,0]}
{"wire":[237,1,236,0]}
{"wire":[1362,0,1358,0]}
{"wire":[1498,1,1500,0]}
{"wire":[1498,0,1418,0]}
{"wire":[371,0,370,0]}
{"wire":[1819,0,1132,0]}
{"wire":[1819,1,1810,0]}
{"wire":[1684,1,1211,0]}
{"wire":[478,0,1056,0]}
{"wire":[478,1,1288,0]}
{"wire":[1247,0,1562,0]}
{"wire":[1282,0,1661,0]}
{"wire":[1282,1,1803,0]}
{"wire":[1282,2,1288,0]}
{"wire":[1102,1,1096,0]}
{"wire":[1683,0,1684,0]}
{"wire":[1683,1,1498,0]}
{"wire":[488,1,478,0]}
{"wire":[488,0,1282,0]}
{"wire":[1049,0,1050,0]}
{"wire":[1511,0,1416,0]}
{"wire":[238,0,237,0]}
{"wire":[360,0,359,0]}
{"wire":[360,1,1188,0]}
{"wire":[445,0,371,0]}
{"wire":[1357,0,1188,0]}
{"wire":[1357,1,1362,0]}
{"wire":[1124,0,1819,0]}
{"wire":[1124,1,1126,0]}
{"wire":[1124,2,1127,0]}
{"wire":[1203,0,1247,0]}
{"wire":[1188,1,260,0]}
{"wire":[1047,0,488,0]}
{"wire":[1047,1,1048,0]}
{"wire":[1047,2,1049,0]}
{"wire":[1125,0,1124,0]}
{"wire":[653,0,660,0]}
{"wire":[653,1,651,0]}
{"wire":[653,2,1102,0]}
{"wire":[965,0,1683,0]}
{"wire":[1359,0,1357,0]}
{"wire":[265,0,238,0]}
{"wire":[356,0,360,0]}
{"wire":[1510,0,1509,0]}
{"wire":[1510,2,1511,0]}
{"wire":[444,0,1188,0]}
{"wire":[444,1,445,0]}
{"wire":[444,2,371,0]}
{"wire":[1634,0,1631,0]}
{"wire":[1245,0,1203,0]}
{"wire":[1245,1,1243,0]}
{"wire":[1854,1,1852,0]}
{"wire":[1854,2,1412,4]}
{"wire":[1854,3,1853,0]}
{"wire":[1888,0,1618,0]}
{"wire":[1888,1,1618,2]}
{"wire":[1241,0,1245,0]}
{"wire":[358,0,356,0]}
{"wire":[358,1,1274,0]}
{"wire":[358,2,1276,0]}
{"wire":[1051,0,1047,0]}
{"wire":[1355,0,1359,0]}
{"wire":[1355,1,1356,0]}
{"wire":[1330,0,1329,0]}
{"wire":[1360,0,444,0]}
{"wire":[650,0,653,0]}
{"wire":[650,1,649,0]}
{"wire":[1485,0,1416,0]}
{"wire":[1485,1,1483,0]}
{"wire":[1485,2,1510,0]}
{"wire":[636,1,1669,0]}
{"wire":[1632,0,1634,0]}
{"wire":[1415,0,1854,0]}
{"wire":[1877,0,1876,0]}
{"wire":[1885,0,1888,0]}
{"wire":[1839,0,1887,0]}
{"wire":[1486,0,1485,0]}
{"wire":[1331,0,1355,0]}
{"wire":[1331,1,1330,0]}
{"wire":[441,1,1213,0]}
{"wire":[1625,0,1360,0]}
{"wire":[1557,0,441,1]}
{"wire":[1557,1,1632,0]}
{"wire":[1557,2,1634,0]}
{"wire":[652,0,650,0]}
{"wire":[252,0,254,0]}
{"wire":[252,1,256,0]}
{"wire":[528,0,526,0]}
{"wire":[528,1,527,0]}
{"wire":[1186,1,966,0]}
{"wire":[658,0,636,1]}
{"wire":[658,1,659,0]}
{"wire":[1881,0,1885,0]}
{"wire":[1882,0,1885,0]}
{"wire":[1880,0,1885,0]}
{"wire":[361,1,358,0]}
{"wire":[1181,0,1182,0]}
{"wire":[1181,1,1154,0]}
{"wire":[1181,2,1198,0]}
{"wire":[1181,3,1241,0]}
{"wire":[1181,4,1203,0]}
{"wire":[1181,6,1839,0]}
{"wire":[1181,7,1877,0]}
{"wire":[1561,0,441,1]}
{"wire":[1561,1,1557,0]}
{"wire":[1554,0,372,0]}
{"wire":[1470,1,1416,0]}
{"wire":[1470,0,1486,0]}
{"wire":[1622,1,1623,0]}
{"wire":[1680,0,361,0]}
{"wire":[1680,1,1880,0]}
{"wire":[1619,0,1451,0]}
{"wire":[1619,1,1881,0]}
{"wire":[1619,2,1625,0]}
{"wire":[1629,0,1451,0]}
{"wire":[1629,1,1882,0]}
{"wire":[1629,2,1331,0]}
{"wire":[638,0,658,0]}
{"wire":[665,0,652,0]}
{"wire":[525,0,1186,0]}
{"wire":[525,1,526,0]}
{"wire":[525,2,528,0]}
{"wire":[246,0,252,0]}
{"wire":[508,1,1055,0]}
{"wire":[1143,0,1181,0]}
{"wire":[1651,1,1669,0]}
{"wire":[1682,0,446,0]}
{"wire":[1351,0,508,1]}
{"wire":[1626,0,1451,0]}
{"wire":[1626,1,1619,0]}
{"wire":[1626,2,1470,0]}
{"wire":[1617,0,1622,0]}
{"wire":[1617,1,1680,0]}
{"wire":[1617,2,1470,0]}
{"wire":[1628,0,1451,0]}
{"wire":[1628,1,1629,0]}
{"wire":[1628,2,1470,0]}
{"wire":[1099,0,1096,0]}
{"wire":[1103,1,1093,0]}
{"wire":[517,0,529,0]}
{"wire":[517,1,525,0]}
{"wire":[446,1,1561,0]}
{"wire":[446,2,440,0]}
{"wire":[401,0,246,0]}
{"wire":[401,1,665,0]}
{"wire":[1144,0,1143,0]}
{"wire":[1655,1,1651,1]}
{"wire":[1655,2,1652,0]}
{"wire":[1462,0,1552,0]}
{"wire":[1462,1,1220,0]}
{"wire":[1462,2,1463,0]}
{"wire":[1094,0,804,0]}
{"wire":[1094,1,1103,0]}
{"wire":[1681,1,446,0]}
{"wire":[1681,0,1682,0]}
{"wire":[588,0,583,0]}
{"wire":[516,0,517,0]}
{"wire":[516,1,1351,0]}
{"wire":[372,1,1626,0]}
{"wire":[372,0,1617,0]}
{"wire":[372,2,1628,0]}
{"wire":[1041,0,1046,0]}
{"wire":[1100,0,401,0]}
{"wire":[1100,1,1099,0]}
{"wire":[767,0,765,0]}
{"wire":[1042,0,1046,0]}
{"wire":[1656,0,1655,0]}
{"wire":[1656,1,1655,0]}
{"wire":[1656,2,1655,0]}
{"wire":[1087,0,372,0]}
{"wire":[1087,1,1157,0]}
{"wire":[742,0,744,0]}
{"wire":[742,1,743,0]}
{"wire":[766,0,767,0]}
{"wire":[1234,0,1462,0]}
{"wire":[1234,1,1235,0]}
{"wire":[1040,0,1041,0]}
{"wire":[749,0,748,0]}
{"wire":[749,1,741,1]}
{"wire":[1573,0,1577,0]}
{"wire":[1573,1,805,0]}
{"wire":[1573,2,1574,0]}
{"wire":[1573,3,1576,0]}
{"wire":[1573,4,1094,0]}
{"wire":[1573,5,588,0]}
{"wire":[1573,6,207,0]}
{"wire":[721,0,708,0]}
{"wire":[1104,0,1093,0]}
{"wire":[437,0,438,0]}
{"wire":[437,2,1681,0]}
{"wire":[1043,0,1042,0]}
{"wire":[1578,0,1100,0]}
{"wire":[1578,1,207,0]}
{"wire":[1578,2,588,0]}
{"wire":[531,1,516,0]}
{"wire":[1653,0,204,0]}
{"wire":[1653,1,362,0]}
{"wire":[1653,2,1656,0]}
{"wire":[704,0,712,0]}
{"wire":[704,1,702,1]}
{"wire":[1400,0,406,1]}
{"wire":[1400,1,1404,0]}
{"wire":[1400,2,1578,0]}
{"wire":[1121,0,1119,0]}
{"wire":[1121,1,275,0]}
{"wire":[1121,2,1120,0]}
{"wire":[1121,3,1460,0]}
{"wire":[391,0,1087,0]}
{"wire":[1388,0,1376,0]}
{"wire":[1221,0,691,0]}
{"wire":[763,0,749,0]}
{"wire":[763,1,766,0]}
{"wire":[1225,0,1234,0]}
{"wire":[1225,1,1222,0]}
{"wire":[1097,0,1573,0]}
{"wire":[1097,1,1098,0]}
{"wire":[1097,2,1104,0]}
{"wire":[745,0,742,0]}
{"wire":[745,1,741,1]}
{"wire":[1045,0,531,0]}
{"wire":[1045,1,1040,0]}
{"wire":[1045,2,1043,0]}
{"wire":[902,0,437,0]}
{"wire":[1111,0,1066,0]}
{"wire":[276,0,581,0]}
{"wire":[719,0,721,0]}
{"wire":[364,0,1653,0]}
{"wire":[764,0,745,0]}
{"wire":[764,1,766,0]}
{"wire":[692,1,276,0]}
{"wire":[692,2,1221,0]}
{"wire":[1579,0,1097,0]}
{"wire":[1579,1,1400,0]}
{"wire":[751,0,763,0]}
{"wire":[1216,0,1217,0]}
{"wire":[1216,1,1462,0]}
{"wire":[1216,2,1045,0]}
{"wire":[1187,1,1121,0]}
{"wire":[705,0,704,0]}
{"wire":[705,1,719,0]}
{"wire":[1069,0,1067,0]}
{"wire":[1782,0,1045,0]}
{"wire":[1070,0,1111,0]}
{"wire":[1784,0,1225,0]}
{"wire":[1784,1,1217,0]}
{"wire":[1784,2,1217,4]}
{"wire":[1064,0,1187,0]}
{"wire":[1064,3,1069,0]}
{"wire":[1064,4,1070,0]}
{"wire":[1785,0,1784,0]}
{"wire":[747,0,751,0]}
{"wire":[747,1,764,0]}
{"wire":[1266,1,392,0]}
{"wire":[1266,0,392,0]}
{"wire":[1266,2,392,0]}
{"wire":[220,0,1579,0]}
{"wire":[220,1,903,0]}
{"wire":[220,2,1397,0]}
{"wire":[220,3,1403,0]}
{"wire":[715,0,705,0]}
{"wire":[1363,0,642,0]}
{"wire":[1363,1,692,0]}
{"wire":[498,0,492,0]}
{"wire":[498,1,499,0]}
{"wire":[1236,0,1216,0]}
{"wire":[1783,0,1782,0]}
{"wire":[1269,0,1363,0]}
{"wire":[753,0,747,0]}
{"wire":[1774,0,1785,0]}
{"wire":[1774,1,1225,0]}
{"wire":[1774,2,1783,0]}
{"wire":[618,0,619,0]}
{"wire":[618,1,644,0]}
{"wire":[491,0,1053,0]}
{"wire":[491,1,1064,0]}
{"wire":[491,2,481,0]}
{"wire":[491,3,482,0]}
{"wire":[491,4,498,0]}
{"wire":[491,5,503,0]}
{"wire":[716,0,715,0]}
{"wire":[466,0,463,0]}
{"wire":[476,0,1053,0]}
{"wire":[476,1,1064,0]}
{"wire":[476,2,481,0]}
{"wire":[476,3,482,0]}
{"wire":[229,0,1266,0]}
{"wire":[229,1,642,0]}
{"wire":[1886,0,220,0]}
{"wire":[726,0,716,0]}
{"wire":[726,1,727,0]}
{"wire":[263,0,229,0]}
{"wire":[263,1,1269,0]}
{"wire":[762,0,753,0]}
{"wire":[762,1,740,0]}
{"wire":[1237,1,693,0]}
{"wire":[1237,0,1238,0]}
{"wire":[1237,2,693,0]}
{"wire":[467,0,466,0]}
{"wire":[454,0,537,0]}
{"wire":[454,2,458,0]}
{"wire":[454,3,457,0]}
{"wire":[1226,0,1774,0]}
{"wire":[1226,1,1465,0]}
{"wire":[661,0,618,0]}
{"wire":[661,1,623,0]}
{"wire":[661,2,644,0]}
{"wire":[490,1,476,0]}
{"wire":[490,0,491,0]}
{"wire":[1858,0,1855,1]}
{"wire":[1858,1,1855,2]}
{"wire":[627,0,1886,0]}
{"wire":[1466,0,692,0]}
{"wire":[621,0,661,0]}
{"wire":[1022,0,1226,0]}
{"wire":[239,0,263,0]}
{"wire":[1265,0,490,0]}
{"wire":[755,0,738,0]}
{"wire":[755,1,726,0]}
{"wire":[462,0,454,0]}
{"wire":[462,1,467,0]}
{"wire":[759,0,762,0]}
{"wire":[759,1,760,0]}
{"wire":[1856,0,1790,0]}
{"wire":[1856,1,1858,0]}
{"wire":[1797,0,1237,0]}
{"wire":[243,0,627,0]}
{"wire":[718,0,756,0]}
{"wire":[718,1,755,0]}
{"wire":[1364,0,621,0]}
{"wire":[738,0,462,0]}
{"wire":[695,1,1797,0]}
{"wire":[695,2,696,0]}
{"wire":[757,0,758,0]}
{"wire":[757,1,759,0]}
{"wire":[1025,0,1265,0]}
{"wire":[1857,0,1856,0]}
{"wire":[1857,1,1790,0]}
{"wire":[694,0,270,0]}
{"wire":[694,1,695,0]}
{"wire":[1231,1,271,0]}
{"wire":[1231,0,1232,0]}
{"wire":[1231,2,271,0]}
{"wire":[1648,0,1644,0]}
{"wire":[731,0,718,0]}
{"wire":[731,1,757,0]}
{"wire":[1468,1,1467,0]}
{"wire":[1468,2,1469,0]}
{"wire":[1789,0,1791,0]}
{"wire":[1789,1,1857,0]}
{"wire":[1555,0,1026,0]}
{"wire":[1647,0,456,0]}
{"wire":[1647,1,1644,0]}
{"wire":[732,1,739,0]}
{"wire":[732,0,731,0]}
{"wire":[1650,0,1645,0]}
{"wire":[1650,1,456,0]}
{"wire":[1650,2,1648,0]}
{"wire":[1270,0,1253,0]}
{"wire":[1270,1,1468,0]}
{"wire":[259,0,1231,0]}
{"wire":[259,1,694,0]}
{"wire":[259,2,1366,0]}
{"wire":[682,1,1669,0]}
{"wire":[1795,1,1789,0]}
{"wire":[1264,0,530,0]}
{"wire":[1264,1,1270,0]}
{"wire":[1264,2,1253,4]}
{"wire":[677,0,682,0]}
{"wire":[677,1,679,0]}
{"wire":[1556,0,1555,0]}
{"wire":[274,0,259,0]}
{"wire":[1646,1,456,0]}
{"wire":[1646,0,1647,0]}
{"wire":[1646,2,1650,0]}
{"wire":[464,0,732,0]}
{"wire":[1254,0,1270,0]}
{"wire":[1254,1,1795,0]}
{"wire":[1254,2,1555,0]}
{"wire":[1256,0,1253,4]}
{"wire":[685,0,686,0]}
{"wire":[685,2,677,0]}
{"wire":[1642,0,1646,0]}
{"wire":[460,0,464,0]}
{"wire":[1257,0,1254,0]}
{"wire":[1257,1,1795,0]}
{"wire":[1257,2,1256,0]}
{"wire":[1260,0,1264,0]}
{"wire":[1260,1,530,0]}
{"wire":[1260,2,1556,0]}
{"wire":[680,0,685,0]}
{"wire":[1636,0,530,0]}
{"wire":[1258,1,1260,0]}
{"wire":[1258,0,1257,0]}
{"wire":[362,1,1669,0]}
{"wire":[1637,0,1258,0]}
{"wire":[1637,1,1643,0]}
{"wire":[1637,2,461,0]}
{"wire":[1635,0,1636,0]}
{"wire":[1635,1,1643,0]}
{"wire":[1635,2,461,0]}
{"wire":[1249,0,1637,0]}
{"wire":[1249,1,681,0]}
{"wire":[282,0,1635,0]}
{"wire":[282,1,681,0]}
{"wire":[1448,1,362,0]}
{"wire":[1448,2,1449,0]}
{"wire":[671,0,670,0]}
{"wire":[1421,0,44,0]}
{"wire":[1421,1,1448,0]}
{"wire":[1024,1,282,0]}
{"wire":[1024,0,282,0]}
{"wire":[1024,2,1249,0]}
{"wire":[604,0,592,0]}
{"wire":[604,1,57,0]}
{"wire":[591,1,601,0]}
{"wire":[602,0,600,0]}
{"wire":[55,0,50,0]}
{"wire":[55,1,51,0]}
{"wire":[1604,0,1599,0]}
{"wire":[606,1,57,0]}
{"wire":[606,0,604,0]}
{"wire":[62,0,59,0]}
{"wire":[62,1,60,0]}
{"wire":[600,0,598,2]}
{"wire":[600,1,599,0]}
{"wire":[592,0,590,0]}
{"wire":[592,1,596,0]}
{"wire":[673,0,671,3]}
{"wire":[601,0,602,0]}
{"wire":[57,1,52,0]}
{"wire":[57,0,53,0]}
{"wire":[57,2,54,0]}
{"wire":[60,0,606,0]}
{"wire":[60,1,58,0]}
{"wire":[590,0,591,0]}
{"wire":[590,1,593,0]}
{"wire":[59,1,55,0]}
{"wire":[59,2,56,0]}
{"wire":[1611,0,1605,0]}
{"wire":[1349,1,1350,0]}
{"wire":[1349,0,673,0]}
{"wire":[365,0,62,0]}
{"wire":[737,0,753,0]}
{"wire":[737,1,740,0]}
{"wire":[1605,0,1604,0]}
{"wire":[1605,1,1600,0]}
{"wire":[1878,0,1182,0]}
{"wire":[1878,1,1154,0]}
{"wire":[1878,2,1198,0]}
{"wire":[1878,3,1241,0]}
{"wire":[1878,4,1203,0]}
{"wire":[1878,6,1839,0]}
{"wire":[1897,32,1839,0]}
{"wire":[42,0,1421,0]}
{"wire":[42,1,1024,0]}
{"wire":[1866,0,42,0]}
{"wire":[1866,1,1349,0]}
{"wire":[1866,3,365,0]}
{"wire":[1867,2,42,0]}
{"wire":[1867,3,673,0]}
{"wire":[1867,4,674,0]}
ASEEND*/
//CHKSM=D29CD75AFC317BE800D271E9E8A818230A9FBF87