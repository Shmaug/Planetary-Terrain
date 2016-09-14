#include "atmosphere.hlsl"

v2f main(float4 vertex : POSITION0, float3 normal : NORMAL0) {
	float3 ro = -planetPos;

	v2f v;
	float4 worldVertex = mul(vertex, World);
	v.position = mul(worldVertex, mul(View, Projection));
	
	float3 rd = normalize(worldVertex.xyz);

	float2 q = getIntersections(ro, rd, CameraHeight * CameraHeight, OuterRadius * OuterRadius);
	float near = q.x, far = q.y;

	float3 start = ro + rd * near;
	far -= near;
	float startAngle = dot(rd, start) / OuterRadius;
	float startDepth = exp(-InvScaleDepth);
	float startOffset = startDepth * scale(startAngle);

	float sampleLength = far / fSamples;
	float scaledLength = sampleLength * Scale;
	float3 sampleRay = rd * sampleLength;
	float3 samplePoint = rd * .5;

	float3 frontColor = 0;
	for (int i = 0; i < nSamples; i++) {
		float height = length(samplePoint);
		float depth = exp(ScaleOverScaleDepth * (InnerRadius - height));
		float lightAngle = dot(LightDirection, samplePoint) / height;
		float cameraAngle = dot(rd, samplePoint) / height;
		float scatter = startOffset + depth * (scale(lightAngle) - scale(cameraAngle));
		float3 attenuate = exp(-scatter * (InvWavelength * Kr4PI * Km4PI));
		frontColor += attenuate * depth * scaledLength;
		samplePoint += sampleRay;
	}

	v.C0 = float4(frontColor * (InvWavelength * KrESun), 1);
	v.C1 = float4(frontColor * KmESun, 1);
	v.rd = worldVertex.xyz;
	return v;
}