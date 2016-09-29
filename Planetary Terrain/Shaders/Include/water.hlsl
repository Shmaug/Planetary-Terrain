#include "constants.hlsl"

cbuffer ChunkConstants : register (b1) {
	row_major float4x4 World;
	row_major float4x4 WorldInverseTranspose;
	bool drawWaterFar;
}
cbuffer PlanetConstants : register(b2) {
	float3 LightDirection;
	float waterHeight;
	float3 waterColor;
}

struct v2f {
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD0;
};