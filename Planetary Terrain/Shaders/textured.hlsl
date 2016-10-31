#include "_constants.hlsli"

Texture2D Texture : register(t0);

cbuffer consts : register(b1) {
	row_major float4x4 World;
};

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};

v2f vsmain(float4 vertex : POSITION0, float2 uv : TEXCOORD) {
	v2f v;
	v.position = mul(vertex, mul(World, mul(View, Projection)));
	v.uv = uv;
	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	return Texture.Sample(AnisotropicSampler, i.uv);
}