/*
* Copyright (C) 2003 Maxim Stepin ( maxst@hiend3d.com )
*
* Copyright (C) 2010 Cameron Zemek ( grom@zeminvaders.net )
*
* Copyright (C) 2014 Jules Blok ( jules@aerix.nl )
*
* This program is free software; you can redistribute it and/or
* modify it under the terms of the GNU Lesser General Public
* License as published by the Free Software Foundation; either
* version 2.1 of the License, or (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
* Lesser General Public License for more details.
*
* You should have received a copy of the GNU Lesser General Public
* License along with this program; if not, write to the Free Software
* Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#define trY 48.0
#define trU 7.0
#define trV 6.0

static float3 yuv_threshold = float3(trY / 255.0, trU / 255.0, trV / 255.0);

const static float3x3 yuv = float3x3(0.299, 0.587, 0.114, -0.169, -0.331, 0.5, 0.5, -0.419, -0.081);
const static float3 yuv_offset = float3(0, 0.5, 0.5);

bool diff(float3 yuv1, float3 yuv2) {
	bool3 res = abs((yuv1 + yuv_offset) - (yuv2 + yuv_offset)) > yuv_threshold;
	return res.x || res.y || res.z;
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
	float2 texcoord : TEXCOORD0;
	float4 t1       : TEXCOORD1;
	float4 t2       : TEXCOORD2;
	float4 t3       : TEXCOORD3;
	float2 ps		: TEXCOORD4;
};

/*    VERTEX_SHADER    */
out_vertex main(float4 position	: POSITION, float4 color : COLOR, float2 texCoord : TEXCOORD0)
{
	out_vertex OUT;

	OUT.position = mul(position, mul(World, Projection));
	OUT.color = color;

	float2 ps = 1.0 / texture_size;
	float dx = ps.x;
	float dy = ps.y;

	//   +----+----+----+
	//   |    |    |    |
	//   | w1 | w2 | w3 |
	//   +----+----+----+
	//   |    |    |    |
	//   | w4 | w5 | w6 |
	//   +----+----+----+
	//   |    |    |    |
	//   | w7 | w8 | w9 |
	//   +----+----+----+

	OUT.ps = ps;
	OUT.texcoord = texCoord;
	OUT.t1 = texCoord.xxxy + float4(-dx, 0, dx, -dy); //  w1 | w2 | w3
	OUT.t2 = texCoord.xxxy + float4(-dx, 0, dx, 0); //  w4 | w5 | w6
	OUT.t3 = texCoord.xxxy + float4(-dx, 0, dx, dy); //  w7 | w8 | w9

	return OUT;
}