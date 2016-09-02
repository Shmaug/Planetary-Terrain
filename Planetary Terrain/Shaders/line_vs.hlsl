#include "line.h"

v2f main(float4 vertex : POSITION0, float4 color : COLOR0) {
	v2f v;
	v.position = mul(vertex, mul(World, mul(View, Projection)));
	v.color = color;
	return v;
}