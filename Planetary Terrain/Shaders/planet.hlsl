#include "_constants.hlsli"
#include "_quadnode.hlsli"
#include "_planet.hlsli"
#include "_atmosphere.hlsli"

Texture2D ColorMapTexture : register(t0);

struct v2f {
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD0;
	float2 uv : TEXCOORD1;
	float height : TEXCOORD2;
	float3 worldPos : TEXCOORD3;
	float3 dir : TEXCOORD4;

	float3 c0 : TEXCOORD5;
	float3 c1 : TEXCOORD6;

	float4 c : COLOR0;
};

v2f vsmain(float4 vertex : POSITION0, float3 normal : NORMAL0, float2 uv : TEXCOORD0, float3 dir : TEXCOORD1, float height : TEXCOORD2, float4 color : COLOR0) {
	v2f v;
	float4 worldPosition = mul(vertex, World);
	v.position = mul(worldPosition, mul(View, Projection));
	v.position.z = LogDepth(v.position.w);
	v.normal = mul(float4(normal, 1), WorldInverseTranspose).xyz;
	v.dir = mul(float4(dir, 1), WorldInverseTranspose).xyz;
	v.uv = uv;
	v.height = height;
	v.worldPos = worldPosition.xyz;

	ScatterOutput o = GroundScatter(mul(vertex, NodeToPlanet).xyz - planetPos);
	v.c0 = o.c0;
	v.c1 = o.c1;

	v.c = color;

	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	float3 col = ColorMapTexture.Sample(AnisotropicSampler, i.uv).rgb * NodeColor;
	bool spec = false;

	if (i.height <= waterLevel && drawWaterFar) {
		col = waterColor;
		i.normal = i.dir;
		spec = true;
	}

	float d = length(i.worldPos) * NodeScale;
	if (d < 500)
		i.c = float4(0, 0, 1, 1);
	if (d < 200)
		i.c = float4(1, 0, 0, 1);

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

	col = i.c1 + col * i.c0;

	return float4(col, 1) * i.c;
}
