#include "atmosphere.hlsl"

float4 main(v2f i) : SV_TARGET
{
	float3 ro = -planetPos;
	float3 rd = normalize(i.worldPos);

	float2 e = ray_vs_sphere(ro, rd, R);

	float2 f = ray_vs_sphere(ro, rd, R_INNER);
	e.y = min(e.y, f.x);

	float4 col = float4(in_scatter(ro, rd, e, -LightDirection), 1);
	col.a = length(col.rgb);
	col.rgb = pow(col.rgb, .454545);

	return col;
}

