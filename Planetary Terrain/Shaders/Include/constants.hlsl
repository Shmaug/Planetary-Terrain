#define PI   3.14159265359
#define PI4 12.56637061436

cbuffer WorldConstants : register(b0) {
	row_major float4x4 View;
	row_major float4x4 Projection;
	float3 CameraPosition;
	float3 CameraDirection;
};