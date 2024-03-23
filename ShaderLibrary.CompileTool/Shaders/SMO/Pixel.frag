#version 450 core

#define ENABLE_ALPHA_MASK false

layout (binding = 2, std140) uniform MdlEnvView
{
    mat3x4 cView;
    mat3x4 cViewInv;
    mat4 cViewProj;
    mat3x4 cViewProjInv;
    mat4 cProjInv;
    mat3x4 cProjInvNoPos;
	vec4 Exposure; //[20]
	vec4 Dir;
	vec4 ZNearFar; //[22] //Near, Far, Far - Near, 1 / (Far - Near)
	vec2 TanFov;
	vec2 ProjOffset;
	vec4 ScreenSize;
	vec4 CameraPos;
} mdlEnvView;

layout (binding = 7, std140) uniform ModelAdditionalInfo 
{
    vec4[] data;
}modelInfo;

layout (binding = 8, std140) uniform HDRTranslate 
{
    float Power;
	float Range;
}hdr;

layout (binding = 3, std140) uniform Material
{
	mat2x4 tex_mtx0;
	mat2x4 tex_mtx1;
	mat2x4 tex_mtx2;
	mat2x4 tex_mtx3;
	float displacement_scale;
	vec3 displacement1_scale;
	vec4 displacement_color;
	vec4 displacement1_color;
	float wrap_coef;
	float refract_thickness;
	vec2 indirect0_scale;
	vec2 indirect1_scale;
	float alpha_test_value;
	float force_roughness;
	float sphere_rate_color0;
	float sphere_rate_color1;
	float sphere_rate_color2;
	float sphere_rate_color3;
	mat4 mirror_view_proj;
	float decal_range;
	float gbuf_fetch_offset;
	float translucence_sharpness;
	float translucence_sharpness_strength;
	float translucence_factor;
	float translucence_silhouette_stress;
	float indirect_depth_scale;
	float cloth_nov_peak_pos0;
	float cloth_nov_peak_pow0;
	float cloth_nov_peak_intensity0;
	float cloth_nov_tone_pow0;
	float cloth_nov_slope0;
	float cloth_nov_emission_scale0;
	vec3 cloth_nov_noise_mask_scale0;
	vec4 proc_texture_3d_scale;
	vec4 flow0_param;
	vec4 ripple_emission_color;
	vec4 hack_color;
	vec4 stain_color;
	float stain_uv_scale;
	float stain_rate;
	float material_lod_roughness;
	float material_lod_metalness;
};

layout (binding = 0) uniform sampler2D cTextureBaseColor; //1
layout (binding = 1) uniform sampler2D cTextureNormal; //2
layout (binding = 2) uniform sampler2D cTextureUniform0; //4
layout (binding = 3) uniform sampler2D cTextureUniform1;
layout (binding = 4) uniform sampler2D cTextureUniform2;
layout (binding = 5) uniform sampler2D cTextureUniform3;
layout (binding = 6) uniform sampler2D cTextureUniform4;

layout (binding = 9) uniform samplerCube cTextureMaterialLightCube;
layout (binding = 22) uniform sampler2D cTextureMaterialLightSphere;

layout (location = 0) in vec4 fNormalsDepth;
layout (location = 1) in vec4 fTexCoords01;
layout (location = 2) in vec4 fTangents;
layout (location = 3) in vec4 fTexCoords23;

layout (location = 0) out vec4 oLightBuf;
layout (location = 1) out vec4 oWorldNrm;
layout (location = 2) out vec4 oNormalizedLinearDepth;
layout (location = 3) out vec4 oBaseColor;

#define o_base_color	 10
#define o_normal		 20
#define o_roughness		 50
#define o_metalness		 51
#define o_sss			 52
#define o_emission		 50

//Selectors for what UV mtx config to use for each sampler
const int FUV_MTX0 = 10;
const int FUV_MTX1 = 11;
const int FUV_MTX2 = 12;
const int FUV_MTX3 = 13;

#define base_color_uv_selector   FUV_MTX0
#define normal_uv_selector	     FUV_MTX0
#define uniform0_uv_selector     FUV_MTX0
#define uniform1_uv_selector	 FUV_MTX0
#define uniform2_uv_selector	 FUV_MTX0
#define uniform3_uv_selector	 FUV_MTX0
#define uniform4_uv_selector	 FUV_MTX0

#define blend0_src			 10
#define blend0_src_ch		 10

#define blend0_dst			 50
#define blend0_dst_ch		 10

#define blend0_cof		     61
#define blend0_cof_ch		 50
#define blend0_cof_map	     50

#define blend0_indirect_map  50

#define blend0_eq		     0

#define enable_blend0		 true


vec4 EncodeBaseColor(vec3 baseColor, float roughness, float metalness, vec3 normal)
{
	return vec4(baseColor, 1.0);
}

vec4 DecodeCubemap(samplerCube cube, vec3 n, float lod)
{
	vec4 tex = textureLod(cube, n, lod);

	float scale = pow(tex.a, hdr.Power) * hdr.Range;
	return vec4(tex.rgb * scale, scale);
}

vec4 GetComp(vec4 v, int comp_mask)
{
	switch (comp_mask)
	{
		case 10: return v.rgba;
		case 20: return v.rrrr;
		case 30: return v.gggg;
		case 50: return v.rgba;
		case 60: return v.aaaa;
	}
	return v.rgba;
}

vec2 SelectTexCoord(int mtx_select)
{
	switch (mtx_select)
	{
		case 0: return fTexCoords01.xy;
		case 1: return fTexCoords01.zw;
		case 2: return fTexCoords23.xy;
		case 3: return fTexCoords23.zw;
		default: return fTexCoords01.xy;
	}
}


vec4 CalculateBlend(vec4 src, vec4 dst, vec4 cof, vec4 ind, int equation)
{
	return src;
}

vec4 CalculateOutput(int flag)
{
	int kind = (flag / 10) | 0;
	int instance = flag % 10;

	switch (flag)
	{
		case 10: return texture(cTextureBaseColor, SelectTexCoord(base_color_uv_selector));
		case 20: return texture(cTextureNormal,	   SelectTexCoord(normal_uv_selector));
		//uniforms0 - uniforms4
		case 50:  return texture(cTextureUniform0, SelectTexCoord(uniform0_uv_selector));
		case 51:  return texture(cTextureUniform1, SelectTexCoord(uniform1_uv_selector));
		case 52:  return texture(cTextureUniform2, SelectTexCoord(uniform2_uv_selector));
		case 53:  return texture(cTextureUniform3, SelectTexCoord(uniform3_uv_selector));
		case 54:  return texture(cTextureUniform4, SelectTexCoord(uniform4_uv_selector));
		 //constants
		case 115: return vec4(0.0);
		case 116: return vec4(1.0);
	}

	if (flag == 8)
	{
		//todo swap #
		if (instance == 0 && enable_blend0)
		{
			vec4 src_calc = GetComp(CalculateOutput(blend0_src), blend0_src_ch);
			vec4 cof_calc = GetComp(CalculateOutput(blend0_cof), blend0_cof_ch);
			vec4 dst_calc = GetComp(CalculateOutput(blend0_dst), blend0_dst_ch);
			vec4 ind_calc = CalculateOutput(blend0_indirect_map);

			return CalculateBlend(src_calc, dst_calc, cof_calc, ind_calc, blend0_eq);
		}
	}

	return vec4(0.0);
}



void main()
{
	vec4 base_color	  = CalculateOutput(o_base_color);
	float normal_map  = CalculateOutput(o_normal).r;
	float metalness   = CalculateOutput(o_metalness).r;
	float roughness   = CalculateOutput(o_roughness).r;
	vec4 sss		  = CalculateOutput(o_sss);


	vec3 normals = fNormalsDepth.rgb;

	const float MAX_LOD = 5.0;

	vec4 irradiance_cubemap = DecodeCubemap(cTextureMaterialLightCube, normals, roughness * MAX_LOD);
	irradiance_cubemap.rgb *= mdlEnvView.Exposure.y;

	oLightBuf = base_color * irradiance_cubemap * 4;

	oWorldNrm.rg = normals.rg * 0.5 + 0.5;

   // oNormalizedLinearDepth.r = fNormalsDepth.w;
    oNormalizedLinearDepth.r = 0.0; //todo??

	oBaseColor = EncodeBaseColor(base_color.rgb,roughness, metalness, normals);
    return;
}