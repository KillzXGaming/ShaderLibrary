#version 450 core

//must match the mesh using this material
#define SKIN_COUNT 4

// tex srt to use
#define o_texcoord0_srt -1
#define o_texcoord1_srt -1
#define o_texcoord2_srt -1
#define o_texcoord3_srt -1
#define o_texcoord4_srt -1
#define o_texcoord5_srt -1

#define o_texcoord0_mapping 0
#define o_texcoord1_mapping 0
#define o_texcoord2_mapping 0
#define o_texcoord3_mapping 0
#define o_texcoord4_mapping 0
#define o_texcoord5_mapping 0

#define o_texcoord0_offset_srt -1
#define o_texcoord1_offset_srt -1
#define o_texcoord2_offset_srt -1
#define o_texcoord3_offset_srt -1
#define o_texcoord4_offset_srt -1
#define o_texcoord5_offset_srt -1

#define o_texcoord_toon_spec_mapping 1
#define o_texcoord_toon_spec_offset_srt -1
#define o_texcoord_toon_spec_srt 3

const int MAX_BONE_COUNT = 100;

const int undef = 0;


layout (binding = 2, std140) uniform GsysContext
{
    mat3x4 cView;
    mat4 cViewProj;
    mat4 cProj;
    mat3x4 cViewInv;
    vec4 cNearFar; //znear, zfar, ratio, inverse ratio
} context;

layout (binding = 3, std140) uniform GsysSkeleton
{
    mat3x4 cBoneMatrices[MAX_BONE_COUNT];
};

layout (binding = 5, std140) uniform GsysShape
{
    mat3x4 cTransform;
	vec4 cParams; //x = skin count
	vec4 cUnknown1; //0
	vec4 cUnknown2; //0
	vec4 cUnknown3; //1 0 0 0
	vec4 cTranslation; //needed to transform into world space
} shape;

struct Fog {
    vec4 Color;
    vec4 Direction;
    float Start;
    float End;
    float Damp;
    float Padding;
};

layout (binding = 6, std140) uniform GsysEnvironment
{
    vec4 cAmbientColor;
    vec4 cHemiSkyColor;
    vec4 cHemiGroundColor;
    vec4 cHemiDirection;

    vec4 cLightDirection0;
    vec4 cLightColor;
    vec4 cLightSpecColor;

    vec4 cLightDirection1;
    vec4 cLightColor1;
    vec4 cLightSpecColor1;

    Fog fog[4];
}environment;

layout (binding = 9, std140) uniform GsysMaterial
{
	mat2x4 p_tex_srt0;
	mat2x4 p_tex_srt1;
	mat2x4 p_tex_srt2;
	mat2x4 p_tex_srt3;
	mat2x4 p_tex_srt4;
	mat2x4 p_tex_srt5;
	vec4 p_const_color0;
	vec4 p_const_color1;
	vec4 p_const_color2;
	vec4 p_const_color3;
	vec4 p_const_color4;
	vec4 p_const_color5;
	vec4 p_const_vector0;
	vec4 p_const_vector1;
	vec4 p_damage_color;
	vec4 p_debug_add_color;
	vec4 p_debug_mul_color;
	vec4 gsys_bake_st0;
	vec2 p_indirect_scale0;
	vec2 p_indirect_scale1;
	vec2 p_indirect_scale2;
	vec2 p_indirect_scale3;
	vec2 p_indirect_scale4;
	vec2 p_indirect_scale5;
	vec2 p_indirect_scale6;
	vec2 p_indirect_scale7;
	float p_user_data;
	float p_user_data1;
	float gsys_material_id;
	float p_attachment_effect;
	float p_attachment_noise_scale;
	float p_blue_print_alpha;
	float p_blue_print_alternative;
	float p_blue_print_alternative_emission;
	float p_blue_print_alternative_vanishing;
	float p_const_value0;
	float p_const_value1;
	float p_const_value2;
	float p_const_value3;
	float p_const_value4;
	float p_const_value5;
	float p_const_value6;
	float p_const_value7;
	float p_cook_ratio;
	float p_edit_proc_discard_scale;
	float p_enable_attachment;
	float p_figure;
	float p_frame_count_coef;
	float gsys_alpha_test_ref_value;
	float gsys_xlu_zprepass_alpha;
	float p_kari_chemical_ice_ratio0;
	float p_kari_mottled_ratio;
	float p_lumberjack_canopy_radius;
	float p_lumberjack_height_eps;
	float p_lumberjack_jaggy_width;
	float p_lumberjack_uv_scale;
	float p_miasma_max_local_y;
	float p_miasma_min_local_y;
	float p_miasma_ratio;
	float p_miasma_scale;
	float p_mimicly_ratio;
	float p_model_tree_scale;
	float p_mottled_scale;
	float p_object_attribute;
	float p_proc_discard;
	float p_proc_vanishing;
	float p_reverse_vanishing;
	float p_round_discard;
	float p_shadow_alpha;
	float p_special_effect_A;
	float p_texture_array_index0;
	float p_texture_array_index1;
	float p_texture_array_index2;
	float p_texture_array_index3;
	float p_texture_array_index4;
	float p_texture_array_index5;
	float p_texture_array_index6;
	float p_texture_array_index7;
	float p_wind_vtx_transform_intensity;
	float p_wind_vtx_transform_lie_height;
	float p_wind_vtx_transform_lie_intensity;
}mat;

layout (location = 0) in vec4 vPosition;
layout (location = 1) in vec4 vNormal;
layout (location = 2) in vec4 vTangent;
layout (location = 3) in vec4 u254; 
layout (location = 4) in vec4 vBoneWeight;
layout (location = 5) in vec4 vBoneWeight2;
layout (location = 6) in ivec4 vBoneIndices;
layout (location = 7) in ivec4 vBoneIndices2;
layout (location = 8) in vec2 vTexCoords0;
layout (location = 9) in vec2 vTexCoords1;
layout (location = 10) in vec2 vTexCoords2;
layout (location = 11) in vec2 vTexCoords3;
layout (location = 11) in vec2 vTexCoords4;
layout (location = 12) in vec4 vColor0;
layout (location = 13) in vec4 vColor1;

//Confirmed for these to match up with the contents
layout (location = 0) out vec4 fTexCoords0; //tex coords 0 -> 1

//tex coords 2 -> 5 go here TODO #if with expected locations

layout (location = 1) out vec4 fNormals;
layout (location = 2) out vec4 fTangents;
layout (location = 3) out vec4 fColor;
layout (location = 4) out vec4 fToonSpecCoords;
layout (location = 5) out vec4 fScreenCoords;

//TODO check the rest
layout (location = 7) out vec4 fTexCoordsBake; //xy shadow, zw lightmap
layout (location = 8) out vec4 fViewDirection;

layout (location = 10) out vec4 fTexCoords23;
layout (location = 11) out vec4 fTexCoords4;


vec4 calc_fog(vec3 pos)
{
	Fog fog = environment.fog[0];
	float z = dot(fog.Direction.xyz, pos.xyz);

	vec4 fog_output = vec4(fog.Color.xyz, 1.0);
	float amount = clamp(z * fog.End + fog.Start, 0.0, 1.0);

	fog_output.a = fog.Color.a * amount * amount;

	return fog_output;
}

vec4 skin(vec3 pos)
{
    vec4 newPosition = vec4(pos, 1.0);
	
	if (SKIN_COUNT == 0) newPosition =  vec4(pos, 1.0) * mat4(shape.cTransform);

	if (SKIN_COUNT >= 1) newPosition =  vec4(pos, 1.0) * mat4(cBoneMatrices[vBoneIndices.x]) * vBoneWeight.x;
	if (SKIN_COUNT >= 2) newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[vBoneIndices.y]) * vBoneWeight.y;
	if (SKIN_COUNT >= 3) newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[vBoneIndices.z]) * vBoneWeight.z;
	if (SKIN_COUNT >= 4) newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[vBoneIndices.w]) * vBoneWeight.w;
	if (SKIN_COUNT >= 5) newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[vBoneIndices2.x]) * vBoneWeight2.x;
	if (SKIN_COUNT >= 6) newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[vBoneIndices2.y]) * vBoneWeight2.y;
	if (SKIN_COUNT >= 7) newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[vBoneIndices2.z]) * vBoneWeight2.z;
	if (SKIN_COUNT >= 8) newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[vBoneIndices2.w]) * vBoneWeight2.w;

    return newPosition;
}

vec3 skinNormal(vec3 nr)
{
    vec3 newNormal = nr;

	if (SKIN_COUNT == 0) newNormal =  nr * mat3(shape.cTransform);

	if (SKIN_COUNT >= 1) newNormal =  nr * mat3(cBoneMatrices[vBoneIndices.x]) * vBoneWeight.x;
	if (SKIN_COUNT >= 2) newNormal += nr * mat3(cBoneMatrices[vBoneIndices.y]) * vBoneWeight.y;
	if (SKIN_COUNT >= 3) newNormal += nr * mat3(cBoneMatrices[vBoneIndices.z]) * vBoneWeight.z;
	if (SKIN_COUNT >= 4) newNormal += nr * mat3(cBoneMatrices[vBoneIndices.w]) * vBoneWeight.w;
	if (SKIN_COUNT >= 5) newNormal += nr * mat3(cBoneMatrices[vBoneIndices2.x]) * vBoneWeight2.x;
	if (SKIN_COUNT >= 6) newNormal += nr * mat3(cBoneMatrices[vBoneIndices2.y]) * vBoneWeight2.y;
	if (SKIN_COUNT >= 7) newNormal += nr * mat3(cBoneMatrices[vBoneIndices2.z]) * vBoneWeight2.z;
	if (SKIN_COUNT >= 8) newNormal += nr * mat3(cBoneMatrices[vBoneIndices2.w]) * vBoneWeight2.w;

    return newNormal;
}

vec2 CalcScaleBias(in vec2 t_Pos, in vec4 t_SB) {
    return t_Pos.xy * t_SB.xy + t_SB.zw;
}

vec2 calc_texcoord_matrix(mat2x4 mat, vec2 tex_coord)
{
	//actually a 2x3 matrix stored in 2x4
	vec3 r0 = vec3(mat[0].xyz);
	vec3 r1 = vec3(mat[0].w, mat[1].xy);

	return (tex_coord * mat3x2(r0, r1)).xy;
}

vec2 get_tex_coord(vec2 tex_coord, mat2x4 mat, int type)
{
	return tex_coord;

	if (type == 0)
		return calc_texcoord_matrix(mat, tex_coord);
	if (type == 4) //sphere mapping used on metal characters
	{
		//view normal
		vec3 view_n = (normalize(fNormals.xyz) * mat3(context.cView)).xyz;
		//center the uvs
		return view_n.xy * vec2(0.5) + vec2(0.5,-0.5);
	}
	return tex_coord;
}

mat2x4 get_srt(int type)
{
	switch (type)
	{
		case 0: return mat.p_tex_srt0;
		case 1: return mat.p_tex_srt1;
		case 2: return mat.p_tex_srt2;
		case 3: return mat.p_tex_srt3;
		case 4: return mat.p_tex_srt4;
	}
	return mat.p_tex_srt0;;
}

vec2 get_tex_mapping(int type)
{
	switch (type)
	{
		case 0: return fTexCoords0.xy;
		case 1: return fTexCoords0.zw;
		case 2: return fTexCoords23.xy;
		case 3: return fTexCoords23.zw;
		case 4: return fTexCoords4.xy;
	}
	return vTexCoords0.xy;;
}

vec2 CalculateToonCoords()
{
	return calc_texcoord_matrix(
		get_srt(o_texcoord_toon_spec_srt), 
		get_tex_mapping(o_texcoord_toon_spec_mapping));;
}
 
void main()
{		
	//position
	vec4 position = skin(vPosition.xyz);
	vec3 normal = skinNormal(vNormal.xyz);
	vec3 tangent = skinNormal(vTangent.xyz);

    gl_Position = vec4(shape.cTranslation.xyz + position.xyz, 1) * context.cViewProj;
	//view position to compute fog
	vec3 view_p = (position.xyz * mat3(context.cView)).xyz;

	//normals
	fNormals = vec4(normal.xyz, 1.0);

	//tangents
	fTangents.xyz = tangent.xyz;
	fTangents.w = vTangent.w;

	//material tex coords
	fTexCoords0.xy  = get_tex_coord(vTexCoords0.xy, get_srt(o_texcoord0_srt), o_texcoord0_mapping);	
	fTexCoords0.zw  = get_tex_coord(vTexCoords1.xy, get_srt(o_texcoord1_srt), o_texcoord1_mapping);	
	fTexCoords23.xy = get_tex_coord(vTexCoords2.xy, get_srt(o_texcoord2_srt), o_texcoord2_mapping);	
	fTexCoords23.zw = get_tex_coord(vTexCoords3.xy, get_srt(o_texcoord3_srt), o_texcoord3_mapping);	
	fTexCoords4.xy  = get_tex_coord(vTexCoords4.xy, get_srt(o_texcoord4_srt), o_texcoord4_mapping);	

	fColor = vColor0;

	//bake texCoords
    fTexCoordsBake.xy = CalcScaleBias(vTexCoords1.xy, mat.gsys_bake_st0);

	vec3 ndc = gl_Position.xyz / gl_Position.w; //perspective divide/normalize
	//Flip needed
    ndc.y *= -1.0;

	//used by screen effects (shadows, light prepass, color buffer)
    fScreenCoords.xy = ndc.xy * 0.5 + 0.5;
    fScreenCoords.xy *= gl_Position.w;
    fScreenCoords.zw = gl_Position.zw;

	fToonSpecCoords.xy = CalculateToonCoords();

	//world pos - camera pos for eye position
	fViewDirection.xyz = position.xyz - vec3(
	   context.cViewInv[0].w,
	   context.cViewInv[1].w, 
	   context.cViewInv[2].w);

    return;
}
