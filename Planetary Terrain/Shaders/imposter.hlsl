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

float4x4 billboard(float3 pos, float3 up, float3 fwd) {
	float3 vec1 = -pos;
	float l = length(vec1);
	if (l < .0001)
		vec1 = -fwd;
	else
		vec1 /= l;

	float3 vec2 = normalize(cross(up, vec1));
	float3 vec3 = cross(vec1, vec2);

	return float4x4(
		vec2.x, vec2.y, vec2.z, 0,
		vec3.x, vec3.y, vec3.z, 0,
		vec1.x, vec1.y, vec1.z, 0,
		pos .x, pos .y, pos .z, 1);
}

v2f vsmain(float4 vertex : POSITION0, float2 uv : TEXCOORD0, float3 pos : TEXCOORD1, float3 up : TEXCOORD2, uint instanceID : SV_InstanceID) {
	v2f v;

	vertex.xyz *= .5;
	vertex.y += .5;

	vertex.xyz *= 30;
	vertex.y *= 2;
	
	pos += offset;

	v.position = mul(vertex, mul(billboard(pos, up, float3(-View[2][0], -View[2][0], -View[2][0])), mul(View, Projection)));
	v.position.z = LogDepth(v.position.w);
	v.uv = float2(uv.x, 1-uv.y);
	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	float4 c = tex.Sample(AnisotropicSampler, i.uv);
	clip(c.a - .1);
	return c;
}