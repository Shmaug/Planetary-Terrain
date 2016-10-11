#include "model.hlsl"

float4 main(v2f i) : SV_TARGET
{
	float3 col = DiffuseTexture.Sample(DiffuseSampler, i.uv).rgb;
	if (length(LightDirection) > 0) {
		float light = dot(LightDirection, -i.normal);
		col *= clamp(light, 0, 1);

		if (SpecularIntensity > 0) {
			float3 r = reflect(-LightDirection, i.normal);
			float3 v = normalize(i.worldPos);
			float dp = dot(r, v);
			if (dp > 0)
				col += SpecularColor * SpecularIntensity * pow(dp, Shininess);
		}
	}
	return float4(col, 1);
}