#include "_constants.hlsli"

Texture2D tex : register(t0);

cbuffer ObjectConstants : register(b1) {
	row_major float4x4 World;
	float3 LightDirection;
}

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};

v2f vsmain(float4 vertex : POSITION0, float2 uv : TEXCOORD0, row_major float4x4 world : WORLD, uint instanceID : SV_InstanceID) {
	v2f v;
	v.position = mul(vertex, mul(mul(world, World), mul(View, Projection)));
	v.uv = uv;
	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	float4 c = tex.Sample(AnisotropicSampler, i.uv);
	clip(c.a - .1);
	return c;
}