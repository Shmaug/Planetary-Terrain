#include "planet.hlsli"

v2f main(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0, float height : TEXCOORD1) {
	v2f v;
	v.position = mul(vertex, mul(World, mul(View, Projection)));
	v.normal = mul(float4(normal, 1), WorldInverseTranspose).xyz;
	v.uv = uv;
	v.height = height;

	float3 cameraPos = -cameraPosition;

	return v;
}