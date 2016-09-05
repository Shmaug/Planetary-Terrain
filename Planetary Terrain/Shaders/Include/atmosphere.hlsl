#include "constants.hlsl"

cbuffer AtmoConstants : register (b1) {
	float4x4 World;

	float InnerRadius;
	float OuterRadius;

	float3 InvWavelength;
	float CameraHeight;

	float KrESun;
	float KmESun;
	float Kr4PI;
	float Km4PI;

	float g;

	float Scale;
	float ScaleDepth;
	float ScaleOverScaleDepth;
	float InvScaleDepth;

	float3 planetPos;

	int nSamples;
}
cbuffer PlanetConstants : register(b2) {
	float3 LightDirection;
}

struct v2f {
	float4 position : SV_POSITION;
	float4 C0 : COLOR0;
	float4 C1 : COLOR1;
	float3 rd : TEXCOORD0;
};

// The scale equation calculated by Vernier's Graphical Analysis
float scale(float fCos)
{
	float x = 1.0 - fCos;
	return ScaleDepth * exp(-0.00287 + x*(0.459 + x*(3.83 + x*(-6.80 + x*5.25))));
}

// Calculates the Mie phase function
float getMiePhase(float fCos, float fCos2, float g, float g2)
{
	return 1.5 * ((1.0 - g2) / (2.0 + g2)) * (1.0 + fCos2) / pow(1.0 + g2 - 2.0*g*fCos, 1.5);
}

// Calculates the Rayleigh phase function
float getRayleighPhase(float fCos2)
{
	//return 1.0;
	return 0.75 + 0.75*fCos2;
}

// Returns the near and far intersection points of a line and a sphere
float2 getIntersections(float3 v3Pos, float3 v3Ray, float fDistance2, float fRadius2)
{
	float B = 2.0 * dot(v3Pos, v3Ray);
	float C = fDistance2 - fRadius2;
	float fDet = sqrt(max(0.0, B*B - 4.0 * C));
	return float2(0.5 * (-B - fDet), 0.5 * (-B + fDet));
}