cbuffer WorldConstants : register(b0) {
	float4x4 view;
	float4x4 projection;
	float3 cameraDirection;
	float farPlane;
};