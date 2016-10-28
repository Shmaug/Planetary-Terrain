#include "_model.hlsli"

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0, row_major float4x4 instanceWorld : WORLD, uint instanceID : SV_InstanceID) {
	v2f v;
	float4x4 w = mul(instanceWorld, World);
	return modelvs(vertex, normal, uv, w, (float3x3)w);
}