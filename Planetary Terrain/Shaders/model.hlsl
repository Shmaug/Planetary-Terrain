#include "_model.hlsli"

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0) {
	v2f v;
	return modelvs(vertex, normal, uv, World);
}