#include "constants.hlsl"

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

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0) {
	v2f v;
	v.position = mul(vertex, mul(World, mul(View, Projection)));
	v.uv = uv;
	v.normal = mul(normal, (float3x3)WorldInverseTranspose);
	return v;
}
float4 psmain(v2f i) : SV_TARGET
{
	float3 col = ColorMapTexture.Sample(ColorMapSampler, i.uv);
	if (length(LightDirection) > 0)
		col *= clamp(dot(LightDirection, -i.normal), 0, 1);
	return float4(col, 1);
}