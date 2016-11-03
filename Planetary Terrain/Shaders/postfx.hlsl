#include "_constants.hlsli"

Texture2D ScreenTexture  : register(t0);

cbuffer BlurConstants : register (b1) {
	float BlurRadius;
}

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};

v2f vsmain(float4 vertex : POSITION0, float2 uv : TEXCOORD0) {
	v2f v;
	v.position = vertex;
	v.uv = float2(uv.x, 1 - uv.y);
	return v;
}

float4 blurps(v2f i) : SV_TARGET
{
	float4 src = ScreenTexture.Sample(AnisotropicSampler, i.uv);
	
	return src;
}