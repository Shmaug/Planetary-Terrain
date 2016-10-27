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

	float3 c0 : COLOR0;
	float3 c1 : COLOR1;
};

float3 Wave(float3 pos, float2 WaveDir, float KAmpOverLen = 3, float WaveLength = 2, float steep = 1, float Phase = 1) {
	float Amplitude = WaveLength * KAmpOverLen;
	float Omega = 2 * PI / WaveLength;
	// float Phase = waves[i].Speed * Omega;
	float Steepness = steep / (Omega * Amplitude * 1);
	float CosTerm = cos(Omega * dot(WaveDir, pos.xz) + Phase * Time);
	float SinTerm = sin(Omega * dot(WaveDir, pos.xz) + Phase * Time);

	// Compute Position
	float3 smallPos;
	smallPos.x = Steepness * Amplitude * WaveDir.x * CosTerm;
	smallPos.z = Steepness * Amplitude * WaveDir.y * CosTerm;
	smallPos.y = Amplitude * sin(Omega * dot(WaveDir, pos.xz) + Phase * Time);

	return smallPos;
}

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0) {
	v2f v;

	float3 wo = 0;

	float2 d = normalize(float2(.25, .25));
	wo += Wave(vertex.xyz * NodeScale + SurfaceOffset, d);

	wo  = mul(wo, (float3x3)NodeOrientation); // relative to planet
	
	float4 wp = mul(vertex, World);
	//wp.xyz += wo * clamp(1 - (length(wp) / Fade), 0, 1);

	v.position = mul(wp, mul(View, Projection));
	v.normal = mul(float4(normal, 1), WorldInverseTranspose).xyz;

	ScatterOutput so = GroundScatter(mul(vertex, NodeToPlanet).xyz + wo - planetPos);
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

	return float4(1 - exp(-Exposure * col), 1);
}