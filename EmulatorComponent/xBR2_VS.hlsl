//#pragma parameter XBR_SCALE "xBR Scale" 3.0 1.0 5.0 1.0
//#pragma parameter XBR_Y_WEIGHT "Y Weight" 48.0 0.0 100.0 1.0
//#pragma parameter XBR_EQ_THRESHOLD "Eq Threshold" 15.0 0.0 50.0 1.0
//#pragma parameter XBR_LV2_COEFFICIENT "Lv2 Coefficient" 2.0 1.0 3.0 0.1

#define XBR_SCALE 3.0
#define XBR_Y_WEIGHT 48.0
#define XBR_EQ_THRESHOLD 15.0, 0.0, 50.0, 1.0
#define XBR_LV2_COEFFICIENT 2.0

// END PARAMETERS //


/*
Hyllian's xBR-lv2 Shader

Copyright (C) 2011-2015 Hyllian - sergiogdb@gmail.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Incorporates some of the ideas from SABR shader. Thanks to Joshua Street.
*/

// Uncomment just one of the three params below to choose the corner detection
//#define CORNER_A
//#define CORNER_B
#define CORNER_C
//#define CORNER_D

#ifndef CORNER_A
#define SMOOTH_TIPS
#endif

const static float4 Ao = float4(1.0, -1.0, -1.0, 1.0);
const static float4 Bo = float4(1.0, 1.0, -1.0, -1.0);
const static float4 Co = float4(1.5, 0.5, -0.5, 0.5);
const static float4 Ax = float4(1.0, -1.0, -1.0, 1.0);
const static float4 Bx = float4(0.5, 2.0, -0.5, -2.0);
const static float4 Cx = float4(1.0, 1.0, -0.5, 0.0);
const static float4 Ay = float4(1.0, -1.0, -1.0, 1.0);
const static float4 By = float4(2.0, 0.5, -2.0, -0.5);
const static float4 Cy = float4(2.0, 0.0, -1.0, 0.5);
const static float4 Ci = float4(0.25, 0.25, 0.25, 0.25);

const static float3x3 yuv = float3x3(0.299, 0.587, 0.114, -0.169, -0.331, 0.499, 0.499, -0.418, -0.0813);
const static float4   epsilon = float4(1e-12, 0.0, 0.0, 0.0);


float4 df(float4 A, float4 B)
{
	return float4(abs(A - B));
}

float c_df(float3 c1, float3 c2) {
	float3 df = abs(c1 - c2);
	return df.r + df.g + df.b;
}

bool4 eq(float4 A, float4 B)
{
	return (df(A, B) < float4(XBR_EQ_THRESHOLD));
}

float4 weighted_distance(float4 a, float4 b, float4 c, float4 d, float4 e, float4 f, float4 g, float4 h)
{
	return (df(a, b) + df(a, c) + df(d, e) + df(d, f) + 4.0*df(g, h));
}

cbuffer OnResize : register(b0)
{
	row_major float4x4 Projection;
}

cbuffer OnFrame : register(b1)
{
	row_major float4x4 World;
}

cbuffer OnGameResizeBuffer : register(b2)
{
	float2 texture_size;
}

struct out_vertex {
	float4 position : SV_POSITION;
	float4 color    : COLOR;
	float2 texCoord1 : TEXCOORD0;
};

/*    VERTEX_SHADER    */
out_vertex main
(
	float4 position	: POSITION,
	float4 color : COLOR,
	float2 texCoord1 : TEXCOORD0
	)
{
	out_vertex OUT;

	OUT.position = mul(position, mul(World, Projection));
	OUT.color = color;

	float2 ps = float2(1.0 / texture_size.x, 1.0 / texture_size.y);
	float dx = ps.x;
	float dy = ps.y;

	//    A1 B1 C1
	// A0  A  B  C C4
	// D0  D  E  F F4
	// G0  G  H  I I4
	//    G5 H5 I5

	// This line fix a bug in ATI cards.
	float2 texCoord = texCoord1 + float2(0.0000001, 0.0000001);

	OUT.texCoord1 = texCoord;

	return OUT;
}