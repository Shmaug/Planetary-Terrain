#include "_constants.hlsli"

cbuffer ModelConstants : register (b1) {
	float4x4 World;
	float4x4 WorldInverseTranspose;
	float3 LightDirection;
	float3 SpecularColor;
	float Shininess;
	float SpecularIntensity;
}

Texture2D DiffuseTexture  : register(t0);
Texture2D EmissiveTexture : register(t1);
Texture2D SpecularTexture : register(t2);
Texture2D NormalTexture : register(t3);

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float3 worldPos : TEXCOORD2;
};

v2f modelvs(float4 vertex, float3 normal, float2 uv, float4x4 world, float3x3 normWorld) {
	v2f v;
	float4 wp = mul(vertex, world);
	v.position = mul(wp, mul(View, Projection));
	v.position.z = LogDepth(v.position.w);
	v.normal = mul(normal, normWorld);
	v.uv = uv;
	v.worldPos = wp.xyz;
	return v;
}

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0) {
	v2f v;
	return modelvs(vertex, normal, uv, World, (float3x3)WorldInverseTranspose);
}

v2f instancedvs(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0, float4x4 instanceWorld : WORLD, uint instanceID : SV_InstanceID) {
	v2f v;
	float4x4 w = mul(instanceWorld, World);
	return modelvs(vertex, normal, uv, w, (float3x3)w);
}

float4 psmain(v2f i) : SV_TARGET
{
	float4 col = DiffuseTexture.Sample(AnisotropicSampler, i.uv);
	clip(col.a - .1);

	if (length(LightDirection) > 0) {
		float3 n = (NormalTexture.Sample(AnisotropicSampler, i.uv).xyz-.5) * 2;
		if (length(n) > 0) {
			n = normalize(n);
			n = mul(n, (float3x3)WorldInverseTranspose);
		}

		float light = dot(LightDirection, -i.normal);
		col.rgb *= clamp(light, 0, 1);

		if (SpecularIntensity > 0) {
			float3 r = reflect(-LightDirection, i.normal);
			float3 v = normalize(i.worldPos);
			float dp = dot(r, v);
			if (dp > 0) {
				float s = SpecularTexture.Sample(AnisotropicSampler, i.uv).r;
				col.rgb += SpecularColor * SpecularIntensity * pow(dp, Shininess) * s;
			}
		}
	}
	col.rgb += EmissiveTexture.Sample(AnisotropicSampler, i.uv).rgb;

	return col;
}