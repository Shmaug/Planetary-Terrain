#include "_constants.hlsli"

Texture2D ScreenTexture  : register(t0);

cbuffer cbuf : register (b1) {
	float4 ScreenRect;
}

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};

v2f vsmain(float4 vertex : POSITION0, float2 uv : TEXCOORD0) {
	v2f v;
	vertex.xy = vertex.xy * .5 + .5; // 0 - 1
	vertex.xy *= ScreenRect.zw;
	vertex.xy += ScreenRect.xy;
	vertex.xy = vertex.xy * 2 - 1; // -1 - 1

	v.position = vertex;
	v.uv = float2(uv.x, 1 - uv.y);
	return v;
}

float4 blurps(v2f i) : SV_TARGET {
	float3 r = ScreenTexture.Sample(AnisotropicSampler, i.uv);
	return float4(r, 1);
}