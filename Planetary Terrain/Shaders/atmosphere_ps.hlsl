#include "atmosphere.h"

float4 main(v2f i) : SV_TARGET
{
	float3 ray = normalize(i.worldpos);

	float2 e = ray_vs_sphere(-planetPos, ray, R);
	if (length(planetPos) < R || e.x > e.y)
		discard;

	float2 f = ray_vs_sphere(-planetPos, ray, R_INNER);
	e.y = min(e.y, f.x);

	float4 col = float4(0, 0, 0, 0);
	col.rgb = in_scatter(-planetPos, ray, e, -LightDirection);
	col.a = length(col.rgb);
	return col;
}

