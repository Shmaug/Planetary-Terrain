#include "_constants.hlsli"

cbuffer AeroConstants : register (b1) {
	row_major float4x4 World;
	row_major float4x4 WorldInverseTranspose;
	float3 VelocityDirection;
	float Size;
	float Step;
}

struct v2f {
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD0;
	float3 worldPos : TEXCOORD1;
};

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0) {
	v2f v;
	v.normal = mul(float4(normal, 1), WorldInverseTranspose).xyz;
	
	float4 wp = mul(vertex, World);
	wp.xyz += -VelocityDirection * Step * Size;
	wp.xyz *= (1 + Step) * Size * .25;

	v.position = mul(wp, mul(View, Projection));
	v.worldPos = wp.xyz;
	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	float v = dot(-VelocityDirection, normalize(i.normal));
	//if (v > 0) {
		float n = noise(i.worldPos);
		return float4(1, 0, 0, 1 / (10 * Step * Step + 1));
	//}

	return 0;
}