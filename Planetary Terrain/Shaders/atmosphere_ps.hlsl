#include "atmosphere.hlsl"

float4 main(v2f i) : SV_TARGET
{
	float g2 = g*g;

	float fCos = dot(-LightDirection, i.rd) / length(i.rd);
	float fRayleighPhase = 0.75 * (1.0 + fCos*fCos);
	float fMiePhase = 1.5 * ((1.0 - g2) / (2.0 + g2)) * (1.0 + fCos*fCos) / pow(1.0 + g2 - 2.0*g*fCos, 1.5);
	float3 f = i.c0 + fMiePhase * i.c1;

	return float4(f, length(f));
}

