/*
Hyllian's 5xBR v3.7a (rounded) Shader

Copyright (C) 2011/2012 Hyllian/Jararaca - sergiogdb@gmail.com

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

*/

const static float coef = 2.0;
const static float3 yuv_weighted = float3(14.352, 28.176, 5.472);


float4 df(float4 A, float4 B)
{
	return float4(abs(A.x - B.x), abs(A.y - B.y), abs(A.z - B.z), abs(A.w - B.w));
}


float4 weighted_distance(float4 a, float4 b, float4 c, float4 d, float4 e, float4 f, float4 g, float4 h)
{
	return (df(a, b) + df(a, c) + df(d, e) + df(d, f) + 4.0*df(g, h));
}

struct out_vertex {
	float4 position : SV_POSITION;
	float2 texCoord : TEXCOORD0;
	float4 t1 : TEXCOORD1;
};

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

/*    VERTEX_SHADER    */
out_vertex main (float4 position	: POSITION, float4 color : COLOR, float2 texCoord : TEXCOORD0)
{
	out_vertex OUT = (out_vertex)0;

	OUT.position = mul(position, mul(World, Projection));

	float2 ps = float2(1.0 / texture_size.x, 1.0 / texture_size.y);
	float dx = ps.x;
	float dy = ps.y;

	OUT.texCoord = texCoord;
	OUT.t1.xy = float2(dx, 0); // F
	OUT.t1.zw = float2(0, dy); // H

	return OUT;
}