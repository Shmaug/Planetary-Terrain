cbuffer QuadNodeConstants : register(b1) {
    row_major float4x4 World;
    row_major float4x4 WorldInverseTranspose;
	row_major float4x4 NodeToPlanet;
	row_major float4x4 NodeOrientation;
	float3 NodeColor;
	float NodeScale;
	bool drawWaterFar;
}