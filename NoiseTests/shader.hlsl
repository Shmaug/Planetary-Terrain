struct v2f {
	float4 position : SV_POSITION;
	float4 color : COLOR0;
};

v2f vsmain(float4 vertex : POSITION0, float4 color : COLOR0) {
	v2f v;
	v.position = vertex;
	v.color = color;
	return v;
}

float4 psmain(v2f i) : SV_TARGET
{
	return i.color;
}