#include "constants.hlsl"

cbuffer ModelConstants : register (b1) {
	row_major float4x4 World;
	row_major float4x4 WorldInverseTranspose;
	float3 LightDirection;
	float3 SpecularColor;
	float Shininess;
	float SpecularIntensity;
}
Texture2D DiffuseTexture : register(t0);
SamplerState DiffuseSampler : register(s0);

Texture2D EmissiveTexture : register(t1);
SamplerState EmissiveSampler : register(s1);

Texture2D SpecularTexture : register(t2);
SamplerState SpecularSampler : register(s2);

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float3 worldPos : TEXCOORD2;
};