cbuffer WorldConstants : register(b0) {
	float4x4 view;
	float4x4 projection;
	float3 cameraDirection;
	float3 lightDirection;
};
cbuffer AtmoConstants : register (b1) {
	float4x4 world;
	float3 center;
	float radius;
}
cbuffer PlanetConstants : register(b2) {
	float4 placeholder;
}
Texture2D colorMapTexture : register(t0);
SamplerState colorMapSampler : register(s0);

struct v2f {
	float4 position : SV_POSITION;
};

v2f vsmain(float4 vertex : POSITION0) {
	v2f v;
	vertex = mul(vertex, world);
	v.position = mul(vertex, mul(view, projection));
	return v;
}
float4 psmain(v2f i) : SV_TARGET
{
	return float4(1, 0, 0, .5);
}