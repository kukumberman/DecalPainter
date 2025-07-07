// Meshに対してデカールを貼るシェーダー
// Mesh空間の位置からUV座標を計算して、デカールテクスチャを描き込む。
Shader "DecalMapping"
{
	Properties
	{
		// 累積用テクスチャ
		_AccumulateTexture ("AccumulateTexture", 2D) = "black" {}
		// デカールテクスチャ
		_DecalTexture("Decal Texture", 2D) = "black" {}
		// デカールペイント情報たち
		_DecalRadius("Decal Radius", Float) = 0.5
		_DecalPositionOS("Decal Position (Object Space)", Vector) = (0, 0, 0, 0)
		_DecalNormal("Decal Normal", Vector) = (0, 1, 0, 0)
		_DecalTangent("Decal Tangent", Vector) = (1, 0, 0, 0)
		_Color("Color", Color) = (0, 0, 0, 0)
		// オブジェクト情報
		_ObjectScale("Object Scale", Vector) = (1, 1, 1, 1)
	}

	SubShader
	{
		Tags{"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}
		LOD 300
		
		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		
		TEXTURE2D(_AccumulateTexture);
		SAMPLER(sampler_AccumulateTexture);
		TEXTURE2D(_DecalTexture);
		SAMPLER(sampler_DecalTexture);

		TEXTURE2D(_MyDepthTexture);
		SAMPLER(sampler_MyDepthTexture);
		
		CBUFFER_START(UnityPerMaterial)
		float4 _AccumulateTexture_ST;
		float4 _DecalTexture_ST;
		float2 _DecalSize;
		float3 _DecalPositionOS;
		float3 _DecalNormal;
		float3 _DecalTangent;
		float4 _Color;
		float3 _ObjectScale;
		float _ProjectionDepth;
		
		float4x4 X_UNITY_MATRIX_V;
		float4x4 X_glstate_matrix_projection;
		#define X_UNITY_MATRIX_P OptimizeProjectionMatrix(X_glstate_matrix_projection)
		float4x4 X_UNITY_MATRIX_VP;
		float4x4 X_UNITY_MATRIX_I_V;
		float4x4 X_UNITY_MATRIX_I_P;
		float4x4 X_UNITY_MATRIX_I_VP;

		float4x4 X_unity_CameraProjection;
		float4x4 X_unity_CameraInvProjection;
		float4x4 X_unity_WorldToCamera;
		float4x4 X_unity_unity_CameraToWorld;
		CBUFFER_END
		ENDHLSL

		Pass
		{
			Blend One Zero
			Cull Off

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex ProcessVertex
			#pragma fragment ProcessFragment
			#pragma shader_feature_local _ UV_CHANNEL_0 UV_CHANNEL_1 UV_CHANNEL_2 UV_CHANNEL_3 UV_CHANNEL_4 UV_CHANNEL_5 UV_CHANNEL_6 UV_CHANNEL_7

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normal : NORMAL;
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				float2 uv3 : TEXCOORD3;
				float2 uv4 : TEXCOORD4;
				float2 uv5 : TEXCOORD5;
				float2 uv6 : TEXCOORD6;
				float2 uv7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float3 positionOS : TEXCOORD1;
				half3 normalOS : TEXCOORD2;
				float3 positionWS : TEXCOORD3;
				float3 positionVS : TEXCOORD4;
			};

			float2 GetUV(Attributes IN)
			{
			#if defined(UV_CHANNEL_0)
				return IN.uv0;
			#elif defined(UV_CHANNEL_1)
				return IN.uv1;
			#elif defined(UV_CHANNEL_2)
				return IN.uv2;
			#elif defined(UV_CHANNEL_3)
				return IN.uv3;
			#elif defined(UV_CHANNEL_4)
				return IN.uv4;
			#elif defined(UV_CHANNEL_5)
				return IN.uv5;
			#elif defined(UV_CHANNEL_6)
				return IN.uv6;
			#elif defined(UV_CHANNEL_7)
				return IN.uv7;
			#else
				return IN.uv0;
			#endif
			}

			float3 Decal_TransformWorldToView(float3 positionWS)
			{
				return mul(X_UNITY_MATRIX_V, float4(positionWS, 1.0)).xyz;
			}

			float3 Decal_ComputeWorldSpacePosition(float2 uv, float depth)
			{
				return ComputeWorldSpacePosition(uv, depth, X_UNITY_MATRIX_I_VP);
			}

			float3 Decal_ComputeViewSpacePosition(float2 uv, float depth)
			{
				return ComputeViewSpacePosition(uv, depth, X_UNITY_MATRIX_I_P);
			}

			float SampleDepthTexture(float2 uv)
			{
				return SAMPLE_TEXTURE2D(_MyDepthTexture, sampler_MyDepthTexture, uv).r;
			}

			float SampleDepth(float2 uv)
			{
			#if UNITY_REVERSED_Z
				float depth = SampleDepthTexture(uv);
			#else
				// Adjust z to match NDC for OpenGL
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleDepthTexture(uv));
			#endif
				
				return depth;
			}

			half4 DepthColorBlend(Varyings input, half4 acc, half4 decalColor, float2 uv)
			{
				float depth = SampleDepth(uv);
				// float3 worldPos = Decal_ComputeWorldSpacePosition(uv, depth);

				// ok
				// return half4(depth, depth, depth, 1);
				// return input.positionWS.x > 0 ? half4(0, 1, 0, 1) : half4(1, 0, 0, 1);
				// return worldPos.z > 0 ? half4(0, 1, 0, 1) : half4(1, 0, 0, 1);
				// return half4(input.positionWS.xyz, 1);
				
				// return half4(input.positionVS.xyz, 1);
				
				float3 VS = Decal_ComputeViewSpacePosition(uv, depth);

				// return half4(VS, 1);
				// return half4(input.positionVS, 1);

				float viewZ = -input.positionVS.z;
				float depthZ = VS.z;
				
				float z = 0;
				z = depthZ;

				// return half4(z, z, z, 1);

				if (viewZ > depthZ + 0.01)
				{
					// return half4(1, 0, 0, 1);
					return acc;
				}

				return decalColor;
				// return half4(1, 1, 1, 1);
				// return acc;
			}

			half4 SourceOver(half4 src, half4 dst)
			{
				half4 result = src * src.a + dst * (1 - src.a);
				return result;
			}

			Varyings ProcessVertex(Attributes input)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				Varyings output = (Varyings)0;

				float2 texcoord = GetUV(input);

				// UV座標をそのままClip空間として表示
				// プラットフォームごとによる上下の違いについて：https://docs.unity3d.com/2019.1/Documentation/Manual/SL-PlatformDifferences.html
				output.positionCS = float4(texcoord.xy * 2 - 1, 0, 1);
				output.positionCS.y *= _ProjectionParams.x;

				// 計算をObject空間で行うため、Object空間の法線と座標を渡す。
				// World空間でもいいが、halfで精度が足りなくなりやすいので理由がなければObject空間で計算する。
				output.texcoord = TRANSFORM_TEX(texcoord, _AccumulateTexture);
				output.normalOS = input.normal;
				output.positionOS = input.positionOS.xyz;
				
				output.positionWS = TransformObjectToWorld(output.positionOS);
				output.positionVS = Decal_TransformWorldToView(output.positionWS);
				
				return output;
			}

			half4 ProcessFragment(Varyings input) : SV_Target
			{
				const half3 normal = normalize(input.normalOS);
				half3 decalNormal = normalize(_DecalNormal);
				half3 decalTangent = normalize(_DecalTangent);
				half3 decalBitangent = normalize(cross(decalTangent, decalNormal));
				decalTangent = normalize(cross(decalNormal, decalBitangent));
				
				// 1. 平面上に描画座標をマップする。
				// 平面から描画座標のベクトルを求め、平面への正射影ベクトルを求める(=平面に投影した座標)
				const half3 decalCenterToPositionVector = (input.positionOS - _DecalPositionOS) * _ObjectScale;
				const float2 positionOnPlane = float2(dot(decalTangent, decalCenterToPositionVector), dot(decalBitangent, decalCenterToPositionVector));
				//return half4(x,y,0,1);
				
				
				// 2. 平面に投影した座標を、平面のUV座標に変換する
				const half2 unclampedUv = (positionOnPlane / _DecalSize) + 0.5; // 0~1空間に正規化するが、範囲外の場合もあるのでClampしない。0~1なら平面内。
				const half sameDirectionMask = step(0.2, dot(normal, decalNormal)); // Decalと同じ向きなら1, 逆なら0
				const half decalAreaMask = int(0 <= unclampedUv.x && unclampedUv.x <= 1 && 0 <= unclampedUv.y && unclampedUv.y <= 1); // 平面内なら1, 平面外なら0 
				const half2 uv = unclampedUv * sameDirectionMask * decalAreaMask;
				//return half4(uv.xy, 0, 1);
				
				
				// 4. UV座標のDecalTextureの色をフェッチするだけ
				half4 decalColor = SAMPLE_TEXTURE2D(_DecalTexture, sampler_DecalTexture, TRANSFORM_TEX(uv, _DecalTexture));
				decalColor.xyz *= _Color.xyz;
				//return stamp;
				
				
				// 5. 累積テクスチャに重ねて描画する
				const half4 acc = SAMPLE_TEXTURE2D(_AccumulateTexture, sampler_AccumulateTexture, input.texcoord);

				if (sameDirectionMask == 0 || decalAreaMask == 0)
				{
					return acc;
				}

				float3 direction = (input.positionOS - _DecalPositionOS);

				if (-input.positionVS.z > _ProjectionDepth)
				{
					return acc;
				}

				if (dot(normalize(direction), decalNormal) >= 0)
				{
					return acc;
				}

				half4 finalColor = SourceOver(decalColor, acc);
				return DepthColorBlend(input, acc, finalColor, uv);
			}
			ENDHLSL
		}
	}
}
