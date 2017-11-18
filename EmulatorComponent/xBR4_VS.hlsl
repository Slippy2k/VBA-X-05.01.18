/*
Hyllian's 4xBR v4.0 (LEVEL 3) Shader

Copyright (C) 2011/2013 Hyllian/Jararaca - sergiogdb@gmail.com

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.


Incorporates some of the ideas from SABR shader. Thanks to Joshua Street.
*/

const static float coef = 2.0;
const static float4 eq_threshold = float4(10.0, 10.0, 10.0, 10.0);
const static float y_weight = 48.0;
const static float u_weight = 7.0;
const static float v_weight = 6.0;
const static float3x3 yuv = float3x3(0.299, 0.587, 0.114, -0.169, -0.331, 0.499, 0.499, -0.418, -0.0813);
const static float3x3 yuv_weighted = float3x3(y_weight*yuv[0], u_weight*yuv[1], v_weight*yuv[2]);
const static float4 delta = float4(0.4, 0.4, 0.4, 0.4);


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
	return (df(A, B) < eq_threshold);
}

bool4 eq2(float4 A, float4 B)
{
	return (df(A, B) < float4(2.0, 2.0, 2.0, 2.0));
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
	float2 texCoord : TEXCOORD0;
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

	//    A1 B1 C1
	// A0  A  B  C C4
	// D0  D  E  F F4
	// G0  G  H  I I4
	//    G5 H5 I5

	// This line fix a bug in ATI cards.
	float2 texCoord = texCoord1 + float2(0.0000001, 0.0000001);

	OUT.texCoord = texCoord;

	return OUT;
}