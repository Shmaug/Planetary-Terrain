#include "atmosphere.hlsl"

float4 main(v2f i) : SV_TARGET
{
	float fCos = dot(LightDirection, i.rd) / length(i.rd);
	float fCos2 = fCos*fCos;
	float4 color = getRayleighPhase(fCos2) * i.C0 + getMiePhase(fCos, fCos2, g, g*g) * i.C1;
	color.a = color.b;
	return float4(CameraHeight, 0, 0, 1);
}

