#include "_constants.hlsli"
#include "_atmosphere.hlsli"

cbuffer AtmoQuadNodeConstants : register(b1) {
	float4x4 World;
	float4x4 WorldInverseTranspose;
	float4x4 NodeToPlanet;
	float3 AtmoLightDirection;
}
struct v2f {
	float4 position : SV_POSITION;
	float3  c0 : TEXCOORD0;
	float3  c1 : TEXCOORD1;
	float3 rd : TEXCOORD2;
};

v2f vsmain(float4 vertex : POSITION0) {
	v2f o;

	float4 worldPosition = mul(vertex, World);
	o.position = mul(worldPosition, mul(View, Projection));
	LogDepth(o.position);
	float3 v3CameraPos = -planetPos;

	// Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
	float3 v3Pos = mul(vertex, NodeToPlanet).xyz - planetPos;
	float3 v3Ray = v3Pos - v3CameraPos;
	float fFar = length(v3Ray);
	v3Ray /= fFar;

	float3 v3Start;
	float fStartOffset;

	if (CameraHeight < OuterRadius) { // in the atmosphere
		// SkyFromAtmosphere

		// Calculate the ray's starting position, then calculate its scattering offset
		v3Start = v3CameraPos;
		float fHeight = length(v3Start);
		float fDepth = exp(ScaleOverScaleDepth * (InnerRadius - CameraHeight));
		float fStartAngle = dot(v3Ray, v3Start) / fHeight;
		fStartOffset = fDepth*scale(fStartAngle);
	} else {
		// SkyFromSpace

		float fNear = GetIntersections(v3CameraPos, v3Ray, OuterRadius).x;
		fFar -= fNear;

		// Calculate the ray's start and end positions in the atmosphere, then calculate its scattering offset
		v3Start = v3CameraPos + v3Ray * fNear;
		float fStartAngle = dot(v3Ray, v3Start) / OuterRadius;
		float fStartDepth = exp(-InvScaleDepth);
		fStartOffset = fStartDepth*scale(fStartAngle);
	}

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
		float fLightAngle = dot(-AtmoLightDirection, v3SamplePoint) / fHeight;
		float fCameraAngle = dot(v3Ray, v3SamplePoint) / fHeight;
		float fScatter = (fStartOffset + fDepth*(scale(fLightAngle) - scale(fCameraAngle)));
		float3 v3Attenuate = exp(-fScatter * (InvWavelength * Kr4PI + Km4PI));
		v3FrontColor += v3Attenuate * (fDepth * fScaledLength);
		v3SamplePoint += v3SampleRay;
	}

	// Finally, scale the Mie and Rayleigh colors and set up the varying variables for the pixel shader
	o.c0.xyz = v3FrontColor * (InvWavelength * KrESun);
	o.c1.xyz = v3FrontColor * KmESun;

	o.rd = v3CameraPos - v3Pos;

	return o;
}

float4 psmain(v2f i) : SV_TARGET
{
	float fCos = dot(-AtmoLightDirection, i.rd) / length(i.rd);
	float fCos2 = fCos*fCos;
	float3 color = getRayleighPhase(fCos2) * i.c0 + getMiePhase(fCos, fCos2, g, g*g) * i.c1;

	color = 1 - exp(-Exposure * color);

	return float4(color.rgb, color.b);
}

