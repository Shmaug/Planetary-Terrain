#include "_constants.hlsli"

TextureCube SkyboxTexture : register(t0);
SamplerState SkyboxSampler : register(s0);

struct v2f {
	float4 position : SV_POSITION;
	float3 uvw : TEXCOORD0;
};

v2f vsmain(float4 vertex : POSITION0) {
	v2f v;
	v.position = mul(vertex, mul(View, Projection));
	v.uvw = vertex.xyz;
	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	return SkyboxTexture.Sample(SkyboxSampler, i.uvw);
}