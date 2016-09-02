#include "constants.hlsl"

cbuffer AtmoConstants : register (b1) {
	float4x4 World;

	int NUM_SCATTER;

	float R_INNER;
	float R;

	float SCALE_H;
	float SCALE_L;

	float K_R;
	float K_M;
	float E;
	float3 C_R;
	float G_M;

	float3 planetPos;
}
cbuffer PlanetConstants : register(b2) {
	float3 LightDirection;
}
static const float MAX = 10000.0;

float2 ray_vs_sphere(float3 p, float3 dir, float r) {
	float b = dot(p, dir);
	float c = dot(p, p) - r * r;

	float d = b * b - c;
	if (d < 0.0) {
		return float2(MAX, -MAX);
	}
	d = sqrt(d);

	return float2(-b - d, -b + d);
}

// Mie
// g : ( -0.75, -0.999 )
//      3 * ( 1 - g^2 )               1 + c^2
// F = ----------------- * -------------------------------
//      2 * ( 2 + g^2 )     ( 1 + g^2 - 2 * g * c )^(3/2)
float phase_mie(float g, float c, float cc) {
	float gg = g * g;

	float a = (1.0 - gg) * (1.0 + cc);

	float b = 1.0 + gg - 2.0 * g * c;
	b *= sqrt(b);
	b *= 2.0 + gg;

	return 1.5 * a / b;
}

// Reyleigh
// g : 0
// F = 3/4 * ( 1 + c^2 )
float phase_reyleigh(float cc) {
	return 0.75 * (1.0 + cc);
}

float density(float3 p) {
	return exp(-(length(p) - R_INNER) * SCALE_H);
}

float optic(float3 p, float3 q) {
	float3 step = (q - p) / (float)NUM_SCATTER;
	float3 v = p + step * 0.5;

	float sum = 0.0;
	for (int i = 0; i < NUM_SCATTER; i++) {
		sum += density(v);
		v += step;
	}
	sum *= length(step) * SCALE_L;

	return sum;
}

float3 in_scatter(float3 o, float3 dir, float2 e, float3 l) {
	float len = (e.y - e.x) / (float)NUM_SCATTER;
	float3 step = dir * len;
	float3 p = o + dir * e.x;
	float3 v = p + dir * (len * 0.5);

	float3 sum = float3(0, 0, 0);
	for (int i = 0; i < NUM_SCATTER; i++) {
		float2 f = ray_vs_sphere(v, l, R);
		float3 u = v + l * f.y;

		float n = (optic(p, v) + optic(v, u)) * (PI * 4.0);

		sum += density(v) * exp(-n * (K_R * C_R + K_M));

		v += step;
	}
	sum *= len * SCALE_L;

	float c = dot(dir, -l);
	float cc = c * c;

	return sum * (K_R * C_R * phase_reyleigh(cc) + K_M * phase_mie(G_M, c, cc)) * E;
}

struct v2f {
	float4 position : SV_POSITION;
	float3 worldPos : TEXCOORD0;
};
