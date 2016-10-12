cbuffer QuadNodeConstants : register(b1) {
    row_major float4x4 World;
    row_major float4x4 WorldInverseTranspose;
	float3 nodePos;
	bool drawWaterFar;
}