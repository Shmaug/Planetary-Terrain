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

struct ScatterOutput {
	float3 c0;
	float3 c1;
};
ScatterOutput GroundScatter(float3 pos) {
	float3 v3CameraPos = -planetPos;

	float3 v3Ray = pos - v3CameraPos;
	float fFar = length(v3Ray);
	v3Ray /= fFar;

	float3 v3Start;

	if (CameraHeight > OuterRadius) {
		// GroundFromSpace

		// Calculate the closest intersection of the ray with the outer atmosphere (which is the near point of the ray passing through the atmosphere)
		float B = 2.0 * dot(v3CameraPos, v3Ray);
		float C = CameraHeight*CameraHeight - OuterRadius*OuterRadius;
		float fDet = max(0.0, B*B - 4.0 * C);
		float fNear = 0.5 * (-B - sqrt(fDet));

		// Calculate the ray's starting position, then calculate its scattering offset
		v3Start = v3CameraPos + v3Ray * fNear;
		fFar -= fNear;
	}
	else {
		// GroundFromAtmosphere
		v3Start = v3CameraPos;
	}

	float fDepth = exp((InnerRadius - OuterRadius) / ScaleDepth);
	float fCameraAngle = dot(-v3Ray, pos) / length(pos);
	float fLightAngle = dot(-LightDirection, pos) / length(pos);
	float fCameraScale = scale(fCameraAngle);
	float fLightScale = scale(fLightAngle);
	float fCameraOffset = fDepth*fCameraScale;
	float fTemp = (fLightScale + fCameraScale);

	// Initialize the scattering loop variables
	float fSampleLength = fFar / fSamples;
	float fScaledLength = fSampleLength * Scale;
	float3 v3SampleRay = v3Ray * fSampleLength;
	float3 v3SamplePoint = v3Start + v3SampleRay * 0.5;

	// Now loop through the sample rays
	float3 v3FrontColor = 0;
	float3 v3Attenuate;
	for (int i = 0; i < nSamples; i++)
	{
		float fHeight = length(v3SamplePoint);
		float fDepth = exp(ScaleOverScaleDepth * (InnerRadius - fHeight));
		float fScatter = fDepth*fTemp - fCameraOffset;
		v3Attenuate = exp(-fScatter * (InvWavelength * Kr4PI + Km4PI));
		v3FrontColor += v3Attenuate * (fDepth * fScaledLength);
		v3SamplePoint += v3SampleRay;
	}

	// Calculate the attenuation factor for the ground
	ScatterOutput o;
	o.c0 = v3Attenuate;
	o.c1 = v3FrontColor * (InvWavelength * KrESun + KmESun);
	return o;
}