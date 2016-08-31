#include "constants.hlsl"

cbuffer AtmoConstants : register (b1) {
	float4x4 World;

	int Samples;

	float InnerRadius;
	float OuterRadius;

	float ScaleH;
	float ScaleL;

	float kr;
	float km;
	float e;
	float3 cr;
	float gm;

	float3 planetPos;
}
cbuffer PlanetConstants : register(b2) {
	float3 LightDirection;
}

struct v2f {
	float4 position : SV_POSITION;
	float3 ray : TEXCOORD0;
};

float mie(float g, float c, float cc) {
	float gg = g*g;
	float a = (1 - gg) * (1 + cc);
	float b = 1 + gg - 2 * g * c;
	b *= sqrt(b);
	b *= 2 + gg;
	return 1.5 * a / b;
}
float reyleigh(float cc) {
	return .75 + .75 * cc;
}
float2 ray_sphere(float3 p, float3 dir, float r) {
	float b = dot(p, dir);
	float c = dot(p, p) - r * r;

	float d = b*b - c;
	if (d < 0)
		return float2(0, 0);

	d = sqrt(d);

	return float2(-b - d, -b + d);
}

float density(float3 p) {
	return exp(-(length(p) - InnerRadius) * ScaleH);
}

float optic(float3 p, float3 q) {
	float3 step = (q - p) / Samples;
	float3 v = p + step * .5;

	float sum = 0;
	for (int i = 0; i < Samples; i++) {
		sum += density(v);
		v += step;
	}
	sum *= length(step) * ScaleL;
	return sum;
}
float3 in_scatter(float3 o, float3 dir, float2 i, float3 l) {
	float len = (i.y - i.x) / Samples;
	float3 step = dir * len;
	float3 p = o + dir * i.x;
	float3 v = p + dir * (len * .5);

	float3 sum = float3(0, 0, 0);
	for (int i = 0; i < Samples; i++) {
		float2 f = ray_sphere(v, l, OuterRadius);
		float3 u = v + l * f.y;
		
		float n = (optic(p, v) + optic(v, u)) * PI*4;

		sum += density(v) * exp(-n * kr * cr + km);

		v += step;
	}
	sum *= len * ScaleL;

	float c = dot(dir, -l);
	float cc = c * c;

	return sum * (kr * cr * reyleigh(cc) + km * mie(gm, c, cc)) * e;
}

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0) {
	v2f v;
	float4 worldVertex = mul(vertex, World);
	v.position = mul(worldVertex, mul(View, Projection));
	
	v.ray = normalize(worldVertex.xyz);

	return v;
}
float4 psmain(v2f i) : SV_TARGET
{
	float4 col = float4(0, 0, 0, 0);
	float2 s = ray_sphere(-planetPos, i.ray, OuterRadius);
	if (length(s) > 0) {
		col.rgb = in_scatter(-planetPos, i.ray, s, -LightDirection);
		col.a = 1;
		return col;
	}
	return col;
}