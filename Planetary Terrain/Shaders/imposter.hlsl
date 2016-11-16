#include "_constants.hlsli"

cbuffer perFrame : register(b1) {
	float4x4 NodeWorld;
	float3 offset;
	float3 LightDirection;
}

Texture2D diffusetex : register(t1);
Texture2D normaltex : register(t2);

struct v2f {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float3 tangent : TEXCOORD2;
};

v2f vsmain(float4 vertex : POSITION0, float2 uv : TEXCOORD0, float3 pos : TEXCOORD1, float3 up : TEXCOORD2, uint instanceID : SV_InstanceID) {
	v2f v;

	vertex.y += 1;
	vertex.xyz *= .5;

	vertex.xyz *= 60;

	//up = mul(up, (float3x3)NodeWorld);
	//pos = mul((float3x3)NodeWorld, pos);

	pos += offset;

	float3 look = -normalize(pos);
	float3 right = normalize(cross(up, look));
	look = normalize(cross(right, up));

	float4x4 billboard = float4x4(
		right.x, right.y, right.z, 0,
		up.x, up.y, up.z, 0,
		look.x, look.y, look.z, 0,
		pos.x, pos.y, pos.z, 1);

	v.position = mul(vertex, mul(billboard, mul(View, Projection)));
	v.position.z = LogDepth(v.position.w);
	v.uv = float2(uv.x, 1 - uv.y);

	v.normal = mul(float3(0,0,1), (float3x3)billboard);
	v.tangent = mul(float3(0,1,0), (float3x3)billboard);

	return v;
}

float4 psmain(v2f i) : SV_TARGET
{

	float4 color = diffusetex.Sample(AnisotropicSampler, i.uv);
	clip(color.a - .1);
	color.a = 1;

	float3 n = UnpackNormal(normalize(i.normal), normalize(i.tangent), normaltex.Sample(AnisotropicSampler, i.uv).rgb);

	float light = clamp(dot(LightDirection, -n), 0, 1);
	color.rgb *= light;

	return color;
}