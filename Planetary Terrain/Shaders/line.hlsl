#include "constants.hlsl"

cbuffer ObjectConstants : register(b1) {
	float4x4 world;
}

struct v2f {
	float4 position : SV_POSITION;
	float4 color : COLOR0;
};

v2f vsmain(float4 vertex : POSITION0, float4 color : COLOR0) {
	v2f v;
	v.position = mul(vertex, mul(world, mul(view, projection)));
	v.color = color;
	return v;
}
float4 psmain(v2f i) : SV_TARGET
{
	return i.color;
}