#include "_constants.hlsli"
#include "_quadnode.hlsli"

cbuffer StarConstants : register(b2) {
	float num;
}
Texture2D ColorMapTexture : register(t1);

struct v2f {
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD0;
	float2 tempHumid : TEXCOORD1;
};

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 tempHumid : TEXCOORD0) {
	v2f v;
	v.position = mul(vertex, mul(World, mul(View, Projection)));
	LogDepth(v.position);
	v.normal = mul(normal, (float3x3)World);
	v.tempHumid = tempHumid;
	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	float3 col = ColorMapTexture.Sample(AnisotropicSampler, i.tempHumid).rgb;
	return float4(col, 1);
}
