cbuffer QuadNodeConstants : register(b1) {
	float4x4 World;
	float4x4 NodeToPlanet;
	float3 NodeLightDirection;
	float3 NodeColor;
	float nodeScale;
}