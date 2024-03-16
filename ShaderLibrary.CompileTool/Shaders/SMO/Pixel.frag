#version 450 core

layout (binding = 2, std140) uniform MdlEnvView
{
    mat3x4 cView;
    mat3x4 cViewInv;
    mat4 cViewProj;
    mat3x4 cViewProjInv;
    mat4 cProjInv;
    mat3x4 cProjInvNoPos;
} mdlEnvView;

layout (binding = 1, std140) uniform GsysMaterial
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

layout (location = 0) in vec4 fPositions;
layout (location = 1) in vec4 fTexCoords0;
layout (location = 2) in vec4 fTangents;
layout (location = 3) in vec4 fNormals;
layout (location = 4) in vec4 fTexCoords23;

layout (location = 0) out vec4 oLightBuf;
layout (location = 1) out vec4 oWorldNrm;
layout (location = 2) out vec4 oNormalizedLinearDepth;
layout (location = 3) out vec4 oBaseColor;

void main()
{
	oLightBuf = vec4(1.0, 0.0, 0.0, 1.0);

	oWorldNrm = fNormals;

    oNormalizedLinearDepth.r = fPositions.w;

	oBaseColor = texture(cTextureBaseColor, fTexCoords0.xy);
}