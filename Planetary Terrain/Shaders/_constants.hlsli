#define PI 3.14159265359
#define PI4 12.56637061436
#define G 9.8

cbuffer WorldConstants : register(b0) {
	row_major float4x4 View;
	row_major float4x4 Projection;
};

Texture2D ShadowDepthTexture : register(t0);
SamplerState AnisotropicSampler : register(s0);
