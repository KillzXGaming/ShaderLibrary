#version 450 core

#define TEXTURE_0_TEXCOORD 0
#define TEXTURE_1_TEXCOORD 1
#define TEXTURE_2_TEXCOORD 1
#define TEXTURE_3_TEXCOORD 1
#define TEXTURE_4_TEXCOORD 0
#define TEXTURE_5_TEXCOORD 1
#define TEXTURE_6_TEXCOORD 0
#define TEXTURE_7_TEXCOORD 0

#define ENABLE_AO true
#define ENABLE_NORMAL_MAP true
#define ENABLE_ALPHA_TEST true

#define ALPHA_TEST_FUNC 1

layout (binding = 2, std140) uniform GsysContext
{
    precise vec4 data[4096];
} context;

layout (binding = 5, std140) uniform GsysShape
{
    mat3x4 transform;
	vec4 cParams; //x = skin count
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
};

layout (binding = 0, std430) buffer _SceneShadingInfo //storage buffer shader info
{
    uint Tiles[];
}sceneShadingInfo;

layout (binding = 15) uniform sampler2D cAlbedoTexture; //cTexture0
layout (binding = 16) uniform sampler2D cSpecularTexture; //cTexture1
layout (binding = 17) uniform sampler2D cNormalMapTexture; //cTexture2
layout (binding = 18) uniform sampler2D cEmissiveMapTexture; //cTexture3
layout (binding = 19) uniform sampler2D cRedMap; //cTexture4
layout (binding = 20) uniform sampler2D cAmientOccMap; //cTexture5

layout (location = 0) in vec4 fTexCoords0;
layout (location = 1) in vec4 fFog;
layout (location = 2) in vec4 fTangents;
layout (location = 3) in vec4 fNormals;
layout (location = 4) in vec4 fTexCoordsBake; //xy shadow
layout (location = 5) in vec4 fAttr5;
layout (location = 6) in vec4 fViewDirection;
layout (location = 7) in vec4 fScreenCoords;
layout (location = 10) in vec4 fTexCoords23;
layout (location = 11) in vec4 fTexCoords4;

layout (location = 0) out vec4 oMaterialID;
layout (location = 1) out vec4 oAlbedoColor;
layout (location = 3) out vec4 oNormals;
layout (location = 5) out vec4 oEmission;

//cell shading material IDs
const float CHARA_SKIN_MATID = 0.0470588244;

vec2 GetTexCoords(int tex_id)
{
	//0 - 4 tex coordinate mappers from vertex shader
	switch (tex_id)
	{
		case 0: return fTexCoords0.xy;
		case 1: return fTexCoords0.zw;
		case 2: return fTexCoords23.xy;
		case 3: return fTexCoords23.zw;
		case 4: return fTexCoords4.xy;
	}
	return fTexCoords0.xy;
}

int ConvertFloatToByte(float v)
{
	return int(trunc(v * 255.0));
}

float ConvertByteToFloat(int v)
{
	return float(v * 0.0039215688593685626983642578125);
}

vec2 EncodeSpecular(vec2 spec_mask)
{
	int spec	  = ((ConvertFloatToByte(spec_mask.x) & 248) | 4);
	int metalness = ((ConvertFloatToByte(spec_mask.y) & 252) | 2);

	return vec2(
		ConvertByteToFloat(spec), 
		ConvertByteToFloat(metalness));
}

vec2 EncodeSpecularAO(float spec_mask, float ambient_occ_w)
{
	int spec	  = ((ConvertFloatToByte(spec_mask) & 248) | 4);
	int metalness = ((ConvertFloatToByte(ambient_occ_w) & 252) | 2);

	return vec2(
		ConvertByteToFloat(spec), 
		ConvertByteToFloat(metalness));
}

vec2 CalculateNormals(vec2 normals, vec2 normal_map)
{
	vec3 N = vec3(normals, 1);
	vec3 T = vec3(fTangents.xyz);
	vec3 B = normalize(cross(T, N) * fTangents.w);

	mat3 tbn_matrix = mat3(T, B, N);

	vec3 tangent_normal = N;
	if (ENABLE_NORMAL_MAP)
	{
		tangent_normal = vec3(normal_map, 1);
		//adjust space to -1 1 range
		tangent_normal = normalize(2.0 * tangent_normal - vec3(1));
	}
	return normalize(tbn_matrix * tangent_normal).xy;
}

void main()
{	
	vec4 base_color = texture(cAlbedoTexture,    GetTexCoords(TEXTURE_0_TEXCOORD)).rgba;
	vec2 spec_mask  = texture(cSpecularTexture,  GetTexCoords(TEXTURE_1_TEXCOORD)).rg;
	vec2 norm_map   = texture(cNormalMapTexture, GetTexCoords(TEXTURE_2_TEXCOORD)).rg;
	float red	    = texture(cRedMap,           GetTexCoords(TEXTURE_4_TEXCOORD)).r;
	float bake_ao   = texture(cAmientOccMap,     GetTexCoords(TEXTURE_5_TEXCOORD)).r;
	float bake_shad = texture(cAmientOccMap,     GetTexCoords(TEXTURE_5_TEXCOORD)).w;

	vec2 specular_encoded = EncodeSpecular(spec_mask);
	vec2 normals = CalculateNormals(fNormals.xy, norm_map);

	oMaterialID.x = CHARA_SKIN_MATID; //material ID to display in the deferred pass
	oMaterialID.y = p_object_attribute; //always set as Y
		
	oAlbedoColor.xyz = base_color.rgb; 
	oAlbedoColor.a = 1.0; //sort of shading affect. Todo not working correctly, set to 1.0 for now

	float alpha = base_color.a;

    //Alpha test
    if (ENABLE_ALPHA_TEST)
    {
        switch (ALPHA_TEST_FUNC)
        {
            case 0: //gequal
                if (alpha <= gsys_alpha_test_ref_value)
                {
                     discard;
                }
            break;
            case 1: //greater
                if (alpha < gsys_alpha_test_ref_value)
                {
                     discard;
                }
            break;
            case 2: //equal
                if (alpha == gsys_alpha_test_ref_value)
                {
                     discard;
                }
            break;
            case 3: //less
                if (alpha > gsys_alpha_test_ref_value)
                {
                     discard;
                }
            break;
            case 4: //lequal
                if (alpha >= gsys_alpha_test_ref_value)
                {
                     discard;
                }
            break;
        }
    }

	oNormals.xy = normals.xy; 
	oNormals.z = bake_ao; 
	oNormals.a = specular_encoded.x; 

	oEmission = vec4(0.0, 0, 0.0, 0.0);

	//storage buffer calculations

	//Calculate index for shader info tile grid
	float index_x = gl_FragCoord.x * context.data[146].x; //w
	float index_y = gl_FragCoord.y * context.data[146].y; //h
	//TODO is this correct???
	int index = int(index_x) * int(index_y) + int(index_x * context.data[146].z);
	sceneShadingInfo.Tiles[index >> 2] = 1u;



    return;
}
