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

layout (binding = 0) uniform sampler2D cTextureBaseColor;
layout (binding = 1) uniform sampler2D cTextureNormal;
layout (binding = 2) uniform sampler2D cTextureUniform0;
layout (binding = 3) uniform sampler2D cTextureUniform1;
layout (binding = 4) uniform sampler2D cTextureUniform2;
layout (binding = 9) uniform samplerCube cTextureMaterialLightCube;
layout (binding = 22) uniform sampler2D cTextureMaterialLightSphere;

layout (location = 0) in vec4 fNormalsDepth;
layout (location = 1) in vec4 fTexCoords0;
layout (location = 2) in vec4 fTangents;
layout (location = 3) in vec4 fTexCoords23;

layout (location = 0) out vec4 oLightBuf;
layout (location = 1) out vec4 oWorldNrm;
layout (location = 2) out vec4 oNormalizedLinearDepth;
layout (location = 3) out vec4 oBaseColor;

vec4 EncodeBaseColor(vec3 baseColor, float roughness, float metalness, vec3 normal)
{
	return vec4(baseColor, 1.0);
}

vec4 DecodeCubemap(samplerCube cube, vec3 n, int lod)
{
	vec4 tex = textureLod(cube, n, lod);

	float scale = pow(tex.a, hdr.Power) * hdr.Range;
	return vec4(tex.rgb * scale, scale);
}

void main()
{
	vec4 baseColor = texture(cTextureBaseColor, fTexCoords0.xy);
	vec3 normals = fNormalsDepth.rgb;

	vec4 irradiance_cubemap = DecodeCubemap(cTextureMaterialLightCube, normals, 5);
	irradiance_cubemap.rgb *= mdlEnvView.Exposure.y;

	float metalness = 0.0;
	float roughness = 0.5;

	oLightBuf = baseColor * irradiance_cubemap * 4;

	oWorldNrm.rg = normals.rg * 0.5 + 0.5;

   // oNormalizedLinearDepth.r = fNormalsDepth.w;
    oNormalizedLinearDepth.r = 0.0; //todo??

	oBaseColor = EncodeBaseColor(baseColor.rgb,roughness, metalness, normals);
    return;
}