#include "_constants.hlsli"
#include "_atmobuffer.hlsli"

cbuffer SkyboxConstants : register(b1) {
	float4x4 World;
	float3 LightDirection;
}

// TEXTURED //
Texture2D Texture : register(t1);

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 worldPos : TEXCOORD1;
};

v2f vsmain(float4 vertex : POSITION0, float2 uv : TEXCOORD) {
	v2f v;
	float4 wp = mul(vertex, World);
	v.position = mul(wp, mul(View, Projection));
	v.uv = uv;
	v.worldPos = wp;

	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	float4 col = Texture.Sample(AnisotropicSampler, i.uv);

	// Calculate attenuation from the camera to the top of the atmosphere toward the vertex
	if (CameraHeight > 0 && CameraHeight < OuterRadius){
		float3 v3Ray = normalize(i.worldPos);
		float fScaledLength = OuterRadius - CameraHeight;
		float3 v3SamplePoint = -planetPos;
		float3 v3Start = -planetPos;

		float fHeight = length(v3Start);
		float fDepth = exp(ScaleOverScaleDepth * (InnerRadius - CameraHeight));
		float fStartAngle = dot(v3Ray, v3Start) / fHeight;
		float fStartOffset = fDepth*scale(fStartAngle);

		float fLightAngle = dot(-LightDirection, v3SamplePoint) / fHeight;
		float fCameraAngle = dot(v3Ray, v3SamplePoint) / fHeight;
		float fScatter = (fStartOffset + fDepth*(scale(fLightAngle) - scale(fCameraAngle)));
		float3 v3Attenuate = exp(-fScatter * (InvWavelength * Kr4PI + Km4PI));

		float3 c1 = v3Attenuate * (fDepth * fScaledLength);

		col.rgb = c1;// lerp(col.rgb, c1, clamp(2 * length(c1), 0, 1));
	}

	return col;
}