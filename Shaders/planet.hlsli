#include "constants.hlsli"

cbuffer ChunkConstants : register (b1) {
	row_major float4x4 World;
	row_major float4x4 WorldInverseTranspose;
	float3 offset;
	bool drawWaterFar;
}
cbuffer PlanetConstants : register(b2) {
	float3 LightDirection;
	float waterHeight;
	float3 waterColor;
}
cbuffer AtmosphereConstants : register(b3) {
	float4x4 World;

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

Texture2D ColorMapTexture : register(t0);
SamplerState ColorMapSampler : register(s0);

struct v2f {
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD0;
	float2 uv : TEXCOORD1;
	float height : TEXCOORD2;
};