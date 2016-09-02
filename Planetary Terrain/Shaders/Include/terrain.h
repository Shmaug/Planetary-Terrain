#include "constants.h"

cbuffer ChunkConstants : register (b1) {
	float4x4 World;
	float4x4 WorldInverseTranspose;
}
cbuffer PlanetConstants : register(b2) {
	float3 LightDirection;
}
Texture2D ColorMapTexture : register(t0);
SamplerState ColorMapSampler : register(s0);

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
};