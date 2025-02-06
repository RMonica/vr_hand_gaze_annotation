Shader "Unlit/PointCloud"
{
	Properties
	{
		_MainTex ("Texture (RGB)", 2D) = "white" {}
		_Size("Size", Float) = 0.1
	}
		SubShader
		{
			//Tags { "Queue" = "AlphaTest" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
			Tags { "RenderType"="Opaque" "Queue" = "Geometry" }
			Blend One OneMinusSrcAlpha
			AlphaToMask On
			Cull Off
			//Lighting Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float _Size;
			struct GS_INPUT
			{
				float4 vertex : POSITION;
				float3 normal	: NORMAL;
				float4 color	: COLOR;
				float4 ringColor : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct FS_INPUT {
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float4 centerpos : TEXCOORD1;
				float4 vertWorld : TEXCOORD2;
				float4 ringColor : TEXCOORD3;

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			GS_INPUT vert (appdata_full v)
			{
				GS_INPUT o = (GS_INPUT)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(GS_INPUT, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.vertex = v.vertex;
				o.normal = v.normal;
				o.color = v.color;
				o.color.a = 1.0;
				o.ringColor = v.texcoord1;
				o.ringColor.a = 1.0;

				//float3 lightshader = ShadeVertexLights(o.vertex, o.normal);
				return o;
			}


			[maxvertexcount(3)]
			void geom (point GS_INPUT tri[1], inout TriangleStream<FS_INPUT> triStream)
			{
				
				FS_INPUT pIn = (FS_INPUT)0;

				UNITY_INITIALIZE_OUTPUT(FS_INPUT, pIn);
				UNITY_SETUP_INSTANCE_ID(tri[0]);
				UNITY_TRANSFER_INSTANCE_ID(tri[0], pIn);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(pIn);

				pIn.normal = mul(unity_ObjectToWorld, tri[0].normal);
				//float light_intensity = max(0.1, dot(pIn.normal, float3(0.0, 0.0, -1.0)));
				float light_intensity = 1.0; // shadows already applied by the natual color of the pointcloud points, given by the natural light present in the scene
				pIn.color = tri[0].color;
				//pIn.color.rgb *= light_intensity;
				pIn.centerpos= mul(unity_ObjectToWorld, tri[0].vertex);
				pIn.ringColor = tri[0].ringColor;
				pIn.ringColor.rgb *= light_intensity;
				if (pIn.color.r == 1.0f && pIn.color.g == 1.0f && pIn.color.b == 1.0f) // if white, not labeled yet
					pIn.color = pIn.ringColor;
				
				float4 vertex = mul(unity_ObjectToWorld, tri[0].vertex);
				float3 tangent = normalize(cross(float3(1,0,0), pIn.normal));
				if (length(cross(float3(1, 0, 0), pIn.normal)) <= 0.0001)
				{
					tangent = normalize(cross(float3(0, 1, 0), pIn.normal));
					if (length(cross(float3(0, 1, 0), pIn.normal)) <= 0.0001)
					{
						tangent = normalize(cross(float3(0, 0, 1), pIn.normal));
					}
				}
				float3 up = normalize(cross(tangent, pIn.normal));

				up = float3(0.0f, 1.0f, 0.0f);
				tangent = float3(1.0f, 0.0f, 0.0f);

				float h = _Size;
				float r = _Size * sqrt(3) / 2.0f;
				float4 dp1 = float4(up * h, 0);
				float4 dp2 = float4(tangent * r - (up * _Size / 2.0f), 0);
				float4 dp3 = float4(-tangent * r - (up * _Size / 2.0f), 0);

				//pIn.vertex = mul(UNITY_MATRIX_VP, vertex + dp2);
				pIn.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_V, vertex) + dp2);
				pIn.texcoord = float2(-0.5,0);
				pIn.vertWorld = vertex + dp2;
				triStream.Append(pIn);

				//pIn.vertex = mul(UNITY_MATRIX_VP, vertex + dp1);
				pIn.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_V, vertex) + dp1);
				pIn.texcoord = float2(0.5,1.5);
				pIn.vertWorld = vertex + dp1;
				triStream.Append(pIn);

				//pIn.vertex = mul(UNITY_MATRIX_VP, vertex + dp3);
				pIn.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_V, vertex) + dp3);
				pIn.texcoord = float2(1.5,0);
				pIn.vertWorld = vertex + dp3;
				triStream.Append(pIn);
			}

			float4 frag (FS_INPUT i) : COLOR
			{
				float4 color = i.color;
				//float d = length(i.vertWorld.xyz/i.vertWorld.w - i.centerpos.xyz/i.centerpos.w);
				float d = distance(i.vertWorld/i.vertWorld.w, i.centerpos/i.centerpos.w);
				float r = _Size / 2.0f;
				if (d > r * 0.5f)
				//if (d > r * 0.8f)
					color = i.ringColor;
				if (d > r)
					discard;
				//color.a = step(0.5, tex2D(_MainTex, i.texcoord).a);
				
				return color;
			}
			ENDCG
		}
	}
}
