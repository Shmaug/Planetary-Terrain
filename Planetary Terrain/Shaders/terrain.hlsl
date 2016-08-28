#include "constants.hlsl"

cbuffer ChunkConstants : register (b1) {
	float4x4 world;
	float4x4 worldInverseTranspose;
}
cbuffer PlanetConstants : register(b2) {
	float4 placeholder;
}
Texture2D colorMapTexture : register(t0);
SamplerState colorMapSampler : register(s0);

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
};

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0) {
	v2f v;
	v.position = mul(vertex, mul(world, mul(view, projection)));
	v.uv = uv;
	v.normal = mul(normal, (float3x3)worldInverseTranspose);
	return v;
}
float4 psmain(v2f i) : SV_TARGET
{
	float3 col = colorMapTexture.Sample(colorMapSampler, i.uv);
	col *= clamp(dot(lightDirection, -i.normal), 0, 1);
	return float4(col, 1);
}