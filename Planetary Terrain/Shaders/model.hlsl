#include "_constants.hlsli"

cbuffer ModelConstants : register (b1) {
	float4x4 World;
	float4x4 WorldInverseTranspose;
	float3 LightDirection;
	float3 SpecularColor;
	float Shininess;
	float SpecularIntensity;
	float EmissiveIntensity;
	bool UseNormalTexture;
}

Texture2D DiffuseTexture  : register(t1);
Texture2D EmissiveTexture : register(t2);
Texture2D SpecularTexture : register(t3);
Texture2D NormalTexture   : register(t4);

struct v2f {
	float4 position : SV_POSITION;
	float3 tangent : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float2 uv : TEXCOORD2;
	float3 worldPos : TEXCOORD3;
	float4 color : COLOR0;
};

v2f modelvs(float4 vertex, float3 normal, float3 tangent, float2 uv, float4 color, float4x4 world, float3x3 normWorld) {
	v2f v;
	float4 wp = mul(vertex, world);
	v.position = mul(wp, mul(View, Projection));
	v.position.z = LogDepth(v.position.w);
	v.normal = normalize(mul(normal, normWorld));
	v.tangent = normalize(mul(tangent, normWorld));
	v.uv = uv;
	v.worldPos = wp.xyz;
	v.color = color;
	return v;
}

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float3 tangent : TANGENT0, float2 uv : TEXCOORD0, float4 color : COLOR0) {
	v2f v;
	return modelvs(vertex, normal, tangent, uv, color, World, (float3x3)WorldInverseTranspose);
}

v2f instancedvs(float4 vertex : POSITION0, float3 normal : NORMAL0, float3 tangent : TANGENT0, float2 uv : TEXCOORD0, float4 color : COLOR0, float4x4 instanceWorld : WORLD, uint instanceID : SV_InstanceID) {
	v2f v;
	float4x4 w = mul(instanceWorld, World);
	return modelvs(vertex, normal, tangent, uv, color, w, (float3x3)w);
}

float4 psmain(v2f i) : SV_TARGET
{
	i.normal = normalize(i.normal);

	float4 col = DiffuseTexture.Sample(AnisotropicSampler, i.uv) * i.color;
	clip(col.a - .1);

	if (UseNormalTexture)
		i.normal = UnpackNormal(i.normal, normalize(i.tangent), NormalTexture.Sample(AnisotropicSampler, i.uv).rgb);

	float light = clamp(dot(LightDirection, -i.normal), 0, 1);
	col.rgb *= light;

	if (light > 0 && SpecularIntensity > 0) {
		float3 r = reflect(-LightDirection, i.normal);
		float3 v = normalize(i.worldPos);
		float dp = dot(r, v);
		if (dp > 0) {
			float s = SpecularTexture.Sample(AnisotropicSampler, i.uv).r;
			col.rgb += SpecularColor * SpecularIntensity * pow(dp, Shininess) * s * light;
		}
	}

	if (EmissiveIntensity > 0)
		col.rgb = lerp(col.rgb, EmissiveTexture.Sample(AnisotropicSampler, i.uv).rgb * EmissiveIntensity, (1 - light) * EmissiveIntensity);

	return col;
}