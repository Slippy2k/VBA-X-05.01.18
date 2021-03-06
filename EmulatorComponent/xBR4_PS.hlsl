/* COMPATIBILITY
- HLSL compilers
- Cg compilers
*/

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

cbuffer OnGameResizeBuffer : register(b0)
{
	float2 texture_size;
}

struct out_vertex {
	float4 position : SV_POSITION;
	float4 color    : COLOR;
	float2 texCoord : texCoord0;
};

Texture2D<float4> Texture : register(t0);
sampler TextureSampler : register(s0);

/*    FRAGMENT SHADER    */
float4 main(in out_vertex VAR) : SV_TARGET
{
	float2 ps = float2(1.0 / texture_size.x, 1.0 / texture_size.y);
	float dx = ps.x;
	float dy = ps.y;

	float4 t1 = VAR.texCoord.xxxy + float4(-dx, 0, dx, -2.0*dy); // A1 B1 C1
	float4 t2 = VAR.texCoord.xxxy + float4(-dx, 0, dx, -dy); //  A  B  C
	float4 t3 = VAR.texCoord.xxxy + float4(-dx, 0, dx, 0); //  D  E  F
	float4 t4 = VAR.texCoord.xxxy + float4(-dx, 0, dx, dy); //  G  H  I
	float4 t5 = VAR.texCoord.xxxy + float4(-dx, 0, dx, 2.0*dy); // G5 H5 I5
	float4 t6 = VAR.texCoord.xyyy + float4(-2.0*dx, -dy, 0, dy); // A0 D0 G0
	float4 t7 = VAR.texCoord.xyyy + float4(2.0*dx, -dy, 0, dy); // C4 F4 I4

	bool4 edr, edr_left, edr_up, edr3_left, edr3_up, px; // px = pixel, edr = edge detection rule
	bool4 interp_restriction_lv1, interp_restriction_lv2_left, interp_restriction_lv2_up;
	bool4 interp_restriction_lv3_left, interp_restriction_lv3_up;
	bool4 nc, nc30, nc60, nc45, nc15, nc75; // new_color
	float4 fx, fx_left, fx_up, final_fx, fx3_left, fx3_up; // inequations of straight lines.
	float3 res1, res2, pix1, pix2;
	float blend1, blend2;

	float2 fp = frac(VAR.texCoord*texture_size);

	float3 A1 = Texture.Sample(TextureSampler, t1.xw).rgb;
	float3 B1 = Texture.Sample(TextureSampler, t1.yw).rgb;
	float3 C1 = Texture.Sample(TextureSampler, t1.zw).rgb;

	float3 A = Texture.Sample(TextureSampler, t2.xw).rgb;
	float3 B = Texture.Sample(TextureSampler, t2.yw).rgb;
	float3 C = Texture.Sample(TextureSampler, t2.zw).rgb;

	float3 D = Texture.Sample(TextureSampler, t3.xw).rgb;
	float3 E = Texture.Sample(TextureSampler, t3.yw).rgb;
	float3 F = Texture.Sample(TextureSampler, t3.zw).rgb;

	float3 G = Texture.Sample(TextureSampler, t4.xw).rgb;
	float3 H = Texture.Sample(TextureSampler, t4.yw).rgb;
	float3 I = Texture.Sample(TextureSampler, t4.zw).rgb;

	float3 G5 = Texture.Sample(TextureSampler, t5.xw).rgb;
	float3 H5 = Texture.Sample(TextureSampler, t5.yw).rgb;
	float3 I5 = Texture.Sample(TextureSampler, t5.zw).rgb;

	float3 A0 = Texture.Sample(TextureSampler, t6.xy).rgb;
	float3 D0 = Texture.Sample(TextureSampler, t6.xz).rgb;
	float3 G0 = Texture.Sample(TextureSampler, t6.xw).rgb;

	float3 C4 = Texture.Sample(TextureSampler, t7.xy).rgb;
	float3 F4 = Texture.Sample(TextureSampler, t7.xz).rgb;
	float3 I4 = Texture.Sample(TextureSampler, t7.xw).rgb;

	float4 b = mul(float4x3(B, D, H, F), yuv_weighted[0]);
	float4 c = mul(float4x3(C, A, G, I), yuv_weighted[0]);
	float4 e = mul(float4x3(E, E, E, E), yuv_weighted[0]);
	float4 d = b.yzwx;
	float4 f = b.wxyz;
	float4 g = c.zwxy;
	float4 h = b.zwxy;
	float4 i = c.wxyz;

	float4 i4 = mul(float4x3(I4, C1, A0, G5), yuv_weighted[0]);
	float4 i5 = mul(float4x3(I5, C4, A1, G0), yuv_weighted[0]);
	float4 h5 = mul(float4x3(H5, F4, B1, D0), yuv_weighted[0]);
	float4 f4 = h5.yzwx;

	float4 c1 = i4.yzwx;
	float4 g0 = i5.wxyz;
	float4 b1 = h5.zwxy;
	float4 d0 = h5.wxyz;

	float4 Ao = float4(1.0, -1.0, -1.0, 1.0);
	float4 Bo = float4(1.0,  1.0, -1.0,-1.0);
	float4 Co = float4(1.5,  0.5, -0.5, 0.5);
	float4 Ax = float4(1.0, -1.0, -1.0, 1.0);
	float4 Bx = float4(0.5,  2.0, -0.5,-2.0);
	float4 Cx = float4(1.0,  1.0, -0.5, 0.0);
	float4 Ay = float4(1.0, -1.0, -1.0, 1.0);
	float4 By = float4(2.0,  0.5, -2.0,-0.5);
	float4 Cy = float4(2.0,  0.0, -1.0, 0.5);

	float4 Az = float4(6.0, -2.0, -6.0, 2.0);
	float4 Bz = float4(2.0, 6.0, -2.0, -6.0);
	float4 Cz = float4(5.0, 3.0, -3.0, -1.0);
	float4 Aw = float4(2.0, -6.0, -2.0, 6.0);
	float4 Bw = float4(6.0, 2.0, -6.0,-2.0);
	float4 Cw = float4(5.0, -1.0, -3.0, 3.0);

	// These inequations define the line below which interpolation occurs.
	fx = (Ao*fp.y + Bo*fp.x);
	fx_left = (Ax*fp.y + Bx*fp.x);
	fx_up = (Ay*fp.y + By*fp.x);
	fx3_left = (Az*fp.y + Bz*fp.x);
	fx3_up = (Aw*fp.y + Bw*fp.x);

	interp_restriction_lv1 = ((e != f) && (e != h) && (!eq(f,b) && !eq(h,d) || eq(e,i) && !eq(f,i4) && !eq(h,i5) || eq(e,g) || eq(e,c)) && (f != f4 && f != i || h != h5 && h != i || h != g || f != c || eq(b,c1) && eq(d,g0)));
	interp_restriction_lv2_left = ((e != g) && (d != g));
	interp_restriction_lv2_up = ((e != c) && (b != c));
	interp_restriction_lv3_left = (eq2(g,g0) && !eq2(d0,g0));
	interp_restriction_lv3_up = (eq2(c,c1) && !eq2(b1,c1));

	float4 fx45 = smoothstep(Co - delta, Co + delta, fx);
	float4 fx30 = smoothstep(Cx - delta, Cx + delta, fx_left);
	float4 fx60 = smoothstep(Cy - delta, Cy + delta, fx_up);
	float4 fx15 = smoothstep(Cz - delta, Cz + delta, fx3_left);
	float4 fx75 = smoothstep(Cw - delta, Cw + delta, fx3_up);


	edr = (weighted_distance(e, c, g, i, h5, f4, h, f) < weighted_distance(h, d, i5, f, i4, b, e, i)) && interp_restriction_lv1;
	edr_left = ((coef*df(f,g)) <= df(h,c)) && interp_restriction_lv2_left;
	edr_up = (df(f,g) >= (coef*df(h,c))) && interp_restriction_lv2_up;
	edr3_left = interp_restriction_lv3_left;
	edr3_up = interp_restriction_lv3_up;


	nc45 = (edr &&             bool4(fx45));
	nc30 = (edr && edr_left && bool4(fx30));
	nc60 = (edr && edr_up   && bool4(fx60));
	nc15 = (edr && edr_left && edr3_left && bool4(fx15));
	nc75 = (edr && edr_up   && edr3_up   && bool4(fx75));

	px = (df(e,f) <= df(e,h));

	nc = (nc75 || nc15 || nc30 || nc60 || nc45);

	float4 final45 = nc45*fx45;
	float4 final30 = nc30*fx30;
	float4 final60 = nc60*fx60;
	float4 final15 = nc15*fx15;
	float4 final75 = nc75*fx75;

	float4 maximo = max(max(max(final15, final75),max(final30, final60)), final45);

	if (nc.x) { pix1 = px.x ? F : H; blend1 = maximo.x; }
	else if (nc.y) { pix1 = px.y ? B : F; blend1 = maximo.y; }
	else if (nc.z) { pix1 = px.z ? D : B; blend1 = maximo.z; }
	else if (nc.w) { pix1 = px.w ? H : D; blend1 = maximo.w; }

	if (nc.w) { pix2 = px.w ? H : D; blend2 = maximo.w; }
	else if (nc.z) { pix2 = px.z ? D : B; blend2 = maximo.z; }
	else if (nc.y) { pix2 = px.y ? B : F; blend2 = maximo.y; }
	else if (nc.x) { pix2 = px.x ? F : H; blend2 = maximo.x; }

	res1 = lerp(E, pix1, blend1);
	res2 = lerp(E, pix2, blend2);

	float3 res = lerp(res1, res2, step(c_df(E, res1), c_df(E, res2)));

	return float4(res, 1.0);
}


