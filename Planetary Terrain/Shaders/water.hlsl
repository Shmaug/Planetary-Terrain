#include "_constants.hlsli"
#include "_planet.hlsli"
#include "_quadnode.hlsli"
#include "_atmosphere.hlsli"

cbuffer WaterBuffer : register(b4) {
	float3 SurfaceOffset;
	float Fade;
	float Time;
};
struct v2f {
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD0;
	float3 worldPos :  TEXCOORD1;
	float height : TEXCOORD2;

	float3 c0 : COLOR0;
	float3 c1 : COLOR1;
};

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float height : TEXCOORD0) {
	v2f v;

	float4 wp = mul(vertex, World);

	v.position = mul(wp, mul(View, Projection));
	v.position.z = LogDepth(v.position.w);
	v.normal = mul(float4(normal, 1), WorldInverseTranspose).xyz;

	v.height = height;

	ScatterOutput so = GroundScatter(mul(vertex, NodeToPlanet).xyz - planetPos);
	v.c0 = so.c0;
	v.c1 = so.c1;

	v.worldPos = wp.xyz;

	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	float3 col = waterColor;

	if (length(LightDirection) > 0) {
		// diffuse lighting
		col *= clamp(dot(LightDirection, -i.normal), 0, 1);

		// specular lighting
		float3 r = reflect(-LightDirection, i.normal);
		float3 v = normalize(i.worldPos);
		float dp = dot(r, v);
		if (dp > 0)
			col += pow(dp, 200);
	}

	col = i.c1 + col * i.c0;

	return float4(col, 1);
}