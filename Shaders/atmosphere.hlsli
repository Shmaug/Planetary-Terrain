#include "constants.hlsli"

cbuffer AtmoConstants : register (b1) {
	row_major float4x4 World;

	float InnerRadius;
	float OuterRadius;

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

	float fSamples;
	int nSamples;

	float3 planetPos;
	float3 InvWavelength;
}
cbuffer PlanetConstants : register(b2) {
	float3 LightDirection;
	float waterHeight;
	float3 waterColor;
}

struct v2f {
	float4 position : SV_POSITION;
	float3 c0 : TEXCOORD0;
	float3 c1 : TEXCOORD1;
	float3 rd : TEXCOORD2;
};

// The scale equation calculated by Vernier's Graphical Analysis
float scale(float fCos)
{
	float x = 1.0 - fCos;
	return ScaleDepth * exp(-0.00287 + x*(0.459 + x*(3.83 + x*(-6.80 + x*5.25))));
}