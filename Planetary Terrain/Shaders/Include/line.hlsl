#include "constants.hlsl"

cbuffer ObjectConstants : register(b1) {
	float4x4 World;
}

struct v2f {
	float4 position : SV_POSITION;
	float4 color : COLOR0;
};