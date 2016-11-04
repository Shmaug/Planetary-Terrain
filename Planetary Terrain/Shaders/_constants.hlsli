#define PI 3.14159265359
#define PI4 12.56637061436
#define G 9.8

// BUFFERS //
// b0: Camera (View, Projection)
// b1: Model (Quadnode constants, model constants)
// b2: Planet constants

// SAMPLERS //
// s0: Anisotropic sampler

cbuffer WorldConstants : register(b0) {
	float4x4 View;
	float4x4 Projection;
};

SamplerState AnisotropicSampler : register(s0);