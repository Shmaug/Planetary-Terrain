#include "water.hlsli"

v2f main(float4 vertex : POSITION0,float3 normal : NORMAL0) {
	v2f v;
	v.position = mul(vertex, mul(World, mul(View, Projection)));
	v.normal = mul(float4(normal, 1), WorldInverseTranspose).xyz;
	return v;
}