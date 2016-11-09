#include "_constants.hlsli"
#include "_quadnode.hlsli"
#include "_planet.hlsli"
#include "_atmosphere.hlsli"

Texture2D ColorMapTexture : register(t0);
Texture2D Texture : register(t1);

struct v2f {
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD0;
	float3 worldPos : TEXCOORD1;
	float3 uvw : TEXCOORD2;
	float2 tempHumid : TEXCOORD3;

	float3 c0 : TEXCOORD5;
	float3 c1 : TEXCOORD6;

	float4 color : COLOR0;
};

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float4 color : COLOR0, float3 uvw : TEXCOORD0, float2 tempHumid : TEXCOORD1) {
	v2f v;
	float4 worldPosition = mul(vertex, World);
	v.position = mul(worldPosition, mul(View, Projection));
	v.position.z = LogDepth(v.position.w);
	v.worldPos = worldPosition.xyz;
	v.normal = mul(float4(normal, 1), WorldInverseTranspose).xyz;
	v.uvw = uvw;
	v.tempHumid = tempHumid;
	v.color = color;

	ScatterOutput o = GroundScatter(mul(vertex, NodeToPlanet).xyz - planetPos);
	v.c0 = o.c0;
	v.c1 = o.c1;

	return v;
}

float4 triplanar(float3 uvw, float3 normal) {
	float3 blend = abs(normal);
	blend /= dot(blend, 1);

	float4 cx = Texture.Sample(AnisotropicSampler, uvw.yz);
	float4 cy = Texture.Sample(AnisotropicSampler, uvw.xz);
	float4 cz = Texture.Sample(AnisotropicSampler, uvw.xy);

	return cx * blend.x + cy * blend.y + cz * blend.z;
}

float4 psmain(v2f i) : SV_TARGET
{
	i.normal = normalize(i.normal);

	float3 col = ColorMapTexture.Sample(AnisotropicSampler, i.tempHumid).rgb * NodeColor;

	//col *= triplanar(i.uvw, i.normal).rgb;
	col *= Texture.Sample(AnisotropicSampler, i.uvw.xy);

	col *= clamp(dot(LightDirection, -i.normal), 0, 1);

	col = i.c1 + col * i.c0;

	return float4(col, 1) * i.color;
}
