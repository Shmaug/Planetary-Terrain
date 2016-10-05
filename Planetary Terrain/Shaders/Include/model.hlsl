#include "constants.hlsl"

cbuffer ModelConstants : register (b1) {
	row_major float4x4 World;
	row_major float4x4 WorldInverseTranspose;
	float3 LightDirection;
}
Texture2D DiffuseTexture : register(t0);
SamplerState DiffuseSampler : register(s0);

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
};