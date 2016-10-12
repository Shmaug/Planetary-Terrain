#include "_constants.hlsli"
#include "_quadnode.hlsli"

cbuffer StarConstants : register(b2) {
	float num;
}
Texture2D ColorMapTexture : register(t0);
SamplerState ColorMapSampler : register(s0);

struct v2f {
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD0;
	float2 uv : TEXCOORD1;
	float height : TEXCOORD2;
};

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0, float height : TEXCOORD1) {
	v2f v;
	v.position = mul(vertex, mul(World, mul(View, Projection)));
	v.normal = mul(normal, (float3x3)World);
	v.uv = uv;
	v.height = height;
	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	float3 col = ColorMapTexture.Sample(ColorMapSampler, i.uv).rgb;
	return float4(col, 1);
}
