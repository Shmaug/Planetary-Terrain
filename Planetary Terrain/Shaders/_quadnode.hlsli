cbuffer QuadNodeConstants : register(b1) {
    float4x4 World;
    float4x4 WorldInverseTranspose;
	float4x4 NodeToPlanet;
	float4x4 NodeOrientation;
	float3 NodeColor;
	float NodeScale;
	bool drawWaterFar;
}