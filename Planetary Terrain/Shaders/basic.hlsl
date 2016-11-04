#include "_constants.hlsli"

cbuffer ObjectConstants : register(b1) {
	float4x4 World;
}

// COLORED //
struct coloredv2f {
	float4 position : SV_POSITION;
	float4 color : COLOR0;
};

coloredv2f coloredvs(float4 vertex : POSITION0, float4 color : COLOR0) {
	coloredv2f v;
	v.position = mul(vertex, mul(World, mul(View, Projection)));
	v.color = color;
	return v;
}

float4 coloredps(coloredv2f i) : SV_TARGET
{
	return i.color;
}

// TEXTURED //
Texture2D Texture : register(t0);

struct texturedv2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};

texturedv2f texturedvs(float4 vertex : POSITION0, float2 uv : TEXCOORD) {
	texturedv2f v;
	v.position = mul(vertex, mul(World, mul(View, Projection)));
	v.uv = uv;
	return v;
}

float4 texturedps(texturedv2f i) : SV_TARGET
{
	return Texture.Sample(AnisotropicSampler, i.uv);
}