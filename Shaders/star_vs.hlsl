#include "star.hlsli"

v2f main(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0, float height : TEXCOORD1) {
	v2f v;
	v.position = mul(vertex, mul(World, mul(View, Projection)));
	v.normal = mul(normal, (float3x3)World);
	v.uv = uv;
	v.height = height;
	return v;
}