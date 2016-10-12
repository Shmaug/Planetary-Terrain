#include "star.hlsli"

float4 main(v2f i) : SV_TARGET
{
	float3 col = ColorMapTexture.Sample(ColorMapSampler, i.uv).rgb;
	return float4(col, 1);
}