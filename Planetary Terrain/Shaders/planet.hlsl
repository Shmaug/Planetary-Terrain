#include "_constants.hlsli"
#include "_quadnode.hlsli"
#include "_atmosphere.hlsli"

Texture2D ColorMapTexture : register(t1);

struct v2f {
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD0;
	float3 worldPos : TEXCOORD1;
	float2 tempHumid : TEXCOORD2;

	float3 c0 : TEXCOORD3;
	float3 c1 : TEXCOORD4;

	float4 color : COLOR0;
};

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float4 color : COLOR0, float2 tempHumid : TEXCOORD0) {
	v2f v;
	float4 worldPosition = mul(vertex, World);
	v.position = mul(worldPosition, mul(View, Projection));
	LogDepth(v.position);
	v.worldPos = worldPosition.xyz;
	v.normal = mul(float4(normal, 1), (float3x3)World).xyz;
	v.tempHumid = tempHumid;
	v.color = color;
	
	if (CameraHeight > 0) { // should be 0 if atmosphere is null
		ScatterOutput o = GroundScatter(mul(vertex, NodeToPlanet).xyz - planetPos, NodeLightDirection);
		v.c0 = o.c0;
		v.c1 = o.c1;
	}
	else {
		v.c0 = 1;
		v.c1 = 0;
	}

	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	i.normal = normalize(i.normal);

	float3 col = ColorMapTexture.Sample(AnisotropicSampler, i.tempHumid).rgb;
	col *= NodeColor;

	col *= clamp(dot(NodeLightDirection, -i.normal), 0, 1);

	col = i.c1 + col * i.c0;

	return float4(col, 1);
}
