cbuffer AtmoConstants : register (b3) {
	float InnerRadius;
	float OuterRadius;

	float CameraHeight;

	float KrESun;
	float KmESun;
	float Kr4PI;
	float Km4PI;

	float g;

	float Scale;
	float ScaleDepth;
	float ScaleOverScaleDepth;
	float InvScaleDepth;

	float Exposure;

	float fSamples;
	int nSamples;

	float3 planetPos;
	float3 InvWavelength;
}

// The scale equation calculated by Vernier's Graphical Analysis
float scale(float fCos)
{
	float x = 1.0 - fCos;
	return ScaleDepth * exp(-0.00287 + x*(0.459 + x*(3.83 + x*(-6.80 + x*5.25))));
}

float2 GetIntersections(float3 v3CameraPos, float3 v3Ray, float radius) {
	float B = 2.0 * dot(v3CameraPos, v3Ray);
	float C = CameraHeight*CameraHeight - radius*radius;
	float fDet = sqrt(max(0.0, B*B - 4.0 * C));
	return float2(0.5 * (-B - fDet), 0.5 * (-B + fDet));
}

// Calculates the Mie phase function
float getMiePhase(float fCos, float fCos2, float g, float g2)
{
	return 1.5 * ((1.0 - g2) / (2.0 + g2)) * (1.0 + fCos2) / pow(1.0 + g2 - 2.0*g*fCos, 1.5);
}

// Calculates the Rayleigh phase function
float getRayleighPhase(float fCos2)
{
	//return 1.0;
	return 0.75 + 0.75*fCos2;
}