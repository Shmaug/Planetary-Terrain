#include "atmosphere.h"

v2f main(float4 vertex : POSITION0, float3 normal : NORMAL0) {
	v2f v;
	float4 worldVertex = mul(vertex, World);
	v.position = mul(worldVertex, mul(View, Projection));
	
	v.worldpos = worldVertex.xyz;

	return v;
}