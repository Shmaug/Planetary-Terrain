#define PI 3.14159265359

cbuffer WorldConstants : register(b0) {
	float4x4 View;
	float4x4 Projection;
	float3 CameraPosition;
	float3 CameraDirection;
	float FarPlane;
};