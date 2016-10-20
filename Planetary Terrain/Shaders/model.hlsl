#include "_constants.hlsli"

cbuffer ModelConstants : register (b1) {
	row_major float4x4 World;
	row_major float4x4 WorldInverseTranspose;
	float3 LightDirection;
	float3 SpecularColor;
	float Shininess;
	float SpecularIntensity;
}
SamplerState TextureSampler : register(s0);

Texture2D DiffuseTexture  : register(t0);
Texture2D EmissiveTexture : register(t1);
Texture2D SpecularTexture : register(t2);

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float3 worldPos : TEXCOORD2;
};

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0) {
	v2f v;
	float4 wp = mul(vertex, World);
	v.position = mul(wp, mul(View, Projection));
	v.normal = mul(float4(normal, 1), WorldInverseTranspose).xyz;
	v.uv = uv;
	v.worldPos = wp.xyz;
	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	float3 col = DiffuseTexture.Sample(TextureSampler, i.uv).rgb;
	if (length(LightDirection) > 0) {
		float light = dot(LightDirection, -i.normal);
		col *= clamp(light, 0, 1);

		if (SpecularIntensity > 0) {
			float3 r = reflect(-LightDirection, i.normal);
			float3 v = normalize(i.worldPos);
			float dp = dot(r, v);
			if (dp > 0) {
				float s = SpecularTexture.Sample(TextureSampler, i.uv).r;
				col += SpecularColor * SpecularIntensity * pow(dp, Shininess) * s;
			}
		}
	}
	col += EmissiveTexture.Sample(TextureSampler, i.uv).rgb;

	return float4(col, 1);
}