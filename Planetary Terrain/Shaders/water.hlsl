#include "_constants.hlsli"
#include "_planet.hlsli"
#include "_quadnode.hlsli"
#include "_atmosphere.hlsli"

cbuffer WaterBuffer : register(b4) {
	float3 SurfaceOffset;
	float DetailDistance;
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

	if (CameraHeight > 0) { // should be 0 if atmosphere is null
		ScatterOutput o = GroundScatter(mul(vertex, NodeToPlanet).xyz - planetPos, NodeLightDirection);
		v.c0 = o.c0;
		v.c1 = o.c1;
	}
	else {
		v.c0 = 1;
		v.c1 = 0;
	}

	v.worldPos = wp.xyz;

	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	i.normal = normalize(i.normal);

	float4 col = float4(waterColor, 1);

	float l = length(i.worldPos);
	col.a = 1 - dot(i.normal, -i.worldPos / l) + (length(i.worldPos)*nodeScale / DetailDistance - .5);

	// diffuse
	float light = clamp(dot(NodeLightDirection, -i.normal), 0, 1);
	col.rgb *= light;

	// specular
	float3 r = reflect(-NodeLightDirection, i.normal);
	float3 v = normalize(i.worldPos);
	float dp = dot(r, v);
	if (dp > 0)
		col.rgb += pow(dp, 200) * light;

	col.rgb = i.c1 + col.rgb * i.c0;

	return col;
}