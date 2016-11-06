#include "_constants.hlsli"

cbuffer perFrame : register(b1) {
	float3 offset;
}

Texture2D tex : register(t0);
Texture2D normtex : register(t1);

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};

float4x4 billboard(float3 pos, float3 up) {
	float3 look = -normalize(pos);
	float3 right = normalize(cross(up, look));

	return float4x4(
		right.x, right.y, right.z, 0,
		up   .x, up   .y, up   .z, 0,
		look .x, look .y, look .z, 0,
		pos  .x, pos  .y, pos  .z, 1);
}

v2f vsmain(float4 vertex : POSITION0, float2 uv : TEXCOORD0, float3 pos : TEXCOORD1, float3 up : TEXCOORD2, uint instanceID : SV_InstanceID) {
	v2f v;

	vertex.y += .5;
	vertex.xyz *= .5;

	vertex.xyz *= 30;
	
	v.position = mul(vertex, mul(billboard(pos+offset, up), mul(View, Projection)));
	v.position.z = LogDepth(v.position.w);
	v.uv = float2(uv.x, 1-uv.y);
	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	float4 c = tex.Sample(AnisotropicSampler, i.uv);
	float4 n = normtex.Sample(AnisotropicSampler, i.uv);
	clip(c.a - .1);
	return c;
}