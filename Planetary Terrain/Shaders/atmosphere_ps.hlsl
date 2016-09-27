#include "atmosphere.hlsl"

float4 main(v2f i) : SV_TARGET
{
	float2 q = getIntersections(-planetPos, i.rd, CameraHeight * CameraHeight, OuterRadius * OuterRadius);

	float fCos = dot(LightDirection, i.rd) / length(i.rd);
	float fCos2 = fCos*fCos;
	float4 color = getRayleighPhase(fCos2) * i.C0 + getMiePhase(fCos, fCos2, g, g*g) * i.C1;
	return i.C1;// float4(color.rgb, length(color.rgb));
}

