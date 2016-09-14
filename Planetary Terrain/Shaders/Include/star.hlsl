#include "constants.hlsl"

cbuffer ChunkConstants : register (b1) {
	row_major float4x4 World;
	row_major float4x4 WorldInverseTranspose;
}
cbuffer StarConstants : register(b2) {
	float num;
}
Texture2D ColorMapTexture : register(t0);
SamplerState ColorMapSampler : register(s0);

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
};
