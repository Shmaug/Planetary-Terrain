cbuffer WorldConstants : register(b0) {
	float4x4 view;
	float4x4 projection;
	float3 cameraPosition;
	float3 cameraDirection;
	float3 lightDirection;
};
cbuffer ChunkConstants : register (b1) {
	float4x4 world;
}
cbuffer PlanetConstants : register(b2) {
	float4x4 planetWorld;
}
Texture2D colorMapTexture : register(t0);
SamplerState colorMapSampler : register(s0);

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
};

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0) {
	float4x4 w = mul(planetWorld, world);
	v2f v;
	v.position = mul(vertex, mul(w, mul(view, projection)));
	v.uv = uv;
	v.normal = mul(normal, (float3x3)w);
	return v;
}
float4 psmain(v2f i) : SV_TARGET
{
	float4 col = colorMapTexture.Sample(colorMapSampler, i.uv);
	return col * clamp(dot(-i.normal, lightDirection), 0, 1);
}