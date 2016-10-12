#include "atmosphere.hlsli"

v2f main(float4 vertex : POSITION0, float3 normal : NORMAL0) {
	v2f o;

	o.position = mul(vertex, mul(World, mul(View, Projection)));

	float3 v3CameraPos = -planetPos;

	float4 worldPosition = mul(vertex, World);
	worldPosition.xyz -= planetPos;

	// Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
	float3 v3Pos = worldPosition;
	float3 v3Ray = v3Pos - v3CameraPos;
	float fFar = length(v3Ray);
	v3Ray /= fFar;

	// Calculate the closest intersection of the ray with the outer atmosphere (which is the near point of the ray passing through the atmosphere)
	float B = 2.0 * dot(v3CameraPos, v3Ray);
	float C = CameraHeight*CameraHeight - OuterRadius*OuterRadius;
	float fDet = max(0.0, B*B - 4.0 * C);
	float fNear = 0.5 * (-B - sqrt(fDet));

	// Calculate the ray's starting position, then calculate its scattering offset
	float3 v3Start = v3CameraPos + v3Ray * fNear;
	fFar -= fNear;
	float fStartAngle = dot(v3Ray, v3Start) / OuterRadius;
	float fStartDepth = exp(-1.0 / ScaleDepth);
	float fStartOffset = fStartDepth*scale(fStartAngle);

	// Initialize the scattering loop variables
	//gl_FrontColor = vec4(0.0, 0.0, 0.0, 0.0);
	float fSampleLength = fFar / fSamples;
	float fScaledLength = fSampleLength * Scale;
	float3 v3SampleRay = v3Ray * fSampleLength;
	float3 v3SamplePoint = v3Start + v3SampleRay * 0.5;

	// Now loop through the sample rays
	float3 v3FrontColor = 0;
	for (int i = 0; i < nSamples; i++)
	{
		float fHeight = length(v3SamplePoint);
		float fDepth = exp(ScaleOverScaleDepth * (InnerRadius - fHeight));
		float fLightAngle = dot(-LightDirection, v3SamplePoint) / fHeight;
		float fCameraAngle = dot(v3Ray, v3SamplePoint) / fHeight;
		float fScatter = (fStartOffset + fDepth*(scale(fLightAngle) - scale(fCameraAngle)));
		float3 v3Attenuate = exp(-fScatter * (InvWavelength * Kr4PI + Km4PI));
		v3FrontColor += v3Attenuate * (fDepth * fScaledLength);
		v3SamplePoint += v3SampleRay;
	}

	// Finally, scale the Mie and Rayleigh colors and set up the varying variables for the pixel shader
	o.c1 = v3FrontColor * KmESun;
	o.c0 = v3FrontColor * (InvWavelength * KrESun);
	o.rd = v3CameraPos - v3Pos;

	return o;
}