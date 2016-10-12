#include "_constants.hlsli"
#include "_quadnode.hlsli"
#include "_planet.hlsli"
#include "_atmosphere.hlsli"

Texture2D ColorMapTexture : register(t0);
SamplerState ColorMapSampler : register(s0);

struct v2f {
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD0;
	float2 uv : TEXCOORD1;
	float height : TEXCOORD2;
	float3 worldPos : TEXCOORD3;

	float3 c0 : COLOR0;
	float3 c1 : COLOR1;
};

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0, float height : TEXCOORD1) {
	v2f v;
	float4 worldPosition = mul(vertex, World);
	v.position = mul(worldPosition, mul(View, Projection));
	v.normal = mul(float4(normal, 1), WorldInverseTranspose).xyz;
	v.uv = uv;
	v.height = height;
	v.worldPos = worldPosition.xyz;

	float3 v3CameraPos = -planetPos;

	// Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
	float3 v3Pos = worldPosition.xyz - planetPos;
	float3 v3Ray = worldPosition.xyz;
	v3Pos = normalize(v3Pos);
	float fFar = length(v3Ray);
	v3Ray / fFar;

	float fNear = GetIntersections(v3CameraPos, v3Ray, OuterRadius).x;

	// Calculate the ray's starting position, then calculate its scattering offset
	float3 v3Start = v3CameraPos + v3Ray * fNear;
	fFar -= fNear;
	float fDepth = exp((InnerRadius - OuterRadius) * InvScaleDepth);
	float fCameraAngle = dot(-v3Ray, v3Pos);
	float fLightAngle = dot(-LightDirection, v3Pos);
	float fCameraScale = scale(fCameraAngle);
	float fLightScale = scale(fLightAngle);
	float fCameraOffset = fDepth*fCameraScale;
	float fTemp = (fLightScale + fCameraScale);

	// Initialize the scattering loop variables
	//gl_FrontColor = vec4(0.0, 0.0, 0.0, 0.0);
	float fSampleLength = fFar / fSamples;
	float fScaledLength = fSampleLength * Scale;
	float3 v3SampleRay = v3Ray * fSampleLength;
	float3 v3SamplePoint = v3Start + v3SampleRay * 0.5;

	// Now loop through the sample rays
	float3 v3FrontColor = 0;
	float3 v3Attenuate = 0;
	for (int i = 0; i<nSamples; i++)
	{
		float fHeight = length(v3SamplePoint);
		float fDepth = exp(ScaleOverScaleDepth * (InnerRadius - fHeight));
		float fScatter = fDepth*fTemp - fCameraOffset;
		v3Attenuate = exp(-fScatter * (InvWavelength * Kr4PI + Km4PI));
		v3FrontColor += v3Attenuate * (fDepth * fScaledLength);
		v3SamplePoint += v3SampleRay;
	}

	v.c0 = v3FrontColor * (InvWavelength * KrESun + KmESun);
	v.c1 = v3Attenuate;

	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	float3 col = ColorMapTexture.Sample(ColorMapSampler, i.uv).rgb;
	bool spec = false;

	if (i.height <= waterHeight && drawWaterFar) {
		col = waterColor;
		spec = true;
	}
	
	if (length(LightDirection) > 0) {
		col *= clamp(dot(LightDirection, -i.normal), 0, 1);

		if (spec) {
			// specular lighting
			float3 r = reflect(-LightDirection, i.normal);
			float3 v = normalize(i.worldPos);
			float dp = dot(r, v);
			if (dp > 0)
				col += pow(dp, 200);
		}
	}

	col = i.c0 + col * i.c1;

	return float4(col, 1);
}
