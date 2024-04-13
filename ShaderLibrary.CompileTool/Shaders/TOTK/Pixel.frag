#version 450 core

#define o_texture0_texcoord 0
#define o_texture1_texcoord 1
#define o_texture2_texcoord 1
#define o_texture3_texcoord 1
#define o_texture4_texcoord 0
#define o_texture5_texcoord 1
#define o_texture6_texcoord 0
#define o_texture7_texcoord 0

#define o_texture_array0 (0)
#define o_texture_array1 (0)
#define o_texture_array2 (0)
#define o_texture_array3 (0)
#define o_texture_array4 (0)
#define o_texture_array5 (0)
#define o_texture_array6 (0)
#define o_texture_array7 (0)

#define o_enable_ao true
#define o_enable_decode_normalmap true //unorm -> snorm
#define o_enable_emission false
#define o_enable_normalmap true
#define o_enable_miasma true
#define o_enable_normalmap2 true
#define o_enable_bake_texture false
#define o_enable_alt_bake_uv true

#define gsys_alpha_test_enable false
#define gsys_alpha_test_func 1
#define gsys_display_face_type 0

#define o_albedo_color 200
#define o_specmask_scaler 1
#define o_specular_hair 1
#define o_normal_color 2
#define o_normal_color2 2
#define o_metal_color 3
#define o_metal_channel 20
#define o_ao_color 5
#define o_ao_channel 10
#define o_specular_channel 10
#define o_specular_hair_channel 10
#define o_emission_color 0
#define o_normalmap_blend_ratio 300
#define o_normalmap_blend_channel 41
#define o_normalmap_blend_method 0
#define o_transmission_color 405
#define o_transmission_channel 0
#define o_transparency_color 0
#define o_transparency_channel 40

#define o_material_attribute 0 //10 = field
#define o_material_behave 101

#define bake_shadow_type 0
#define bake_light_type -1

#define o_enable_indirect0 false
#define o_enable_indirect1 false
#define o_enable_indirect2 false
#define o_enable_indirect3 false
#define o_enable_indirect4 false
#define o_enable_indirect5 false
#define o_enable_indirect6 false
#define o_enable_indirect7 false

#define o_indirect0_texcoord 0
#define o_indirect1_texcoord 0
#define o_indirect2_texcoord 0
#define o_indirect3_texcoord 0
#define o_indirect4_texcoord 0
#define o_indirect5_texcoord 0
#define o_indirect6_texcoord 0
#define o_indirect7_texcoord 0

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
}mat;

layout (binding = 0, std430) buffer _SceneShadingInfo //storage buffer shader info
{
    uint Tiles[];
}sceneShadingInfo;

#define SAMPLER_TEX_2D(index)       layout (binding = 15 + index) uniform sampler2D cTexture##index
#define SAMPLER_TEX_2D_ARRAY(index) layout (binding = 15 + index) uniform sampler2DArray cTexture##index

//Used when transparent materials are used
layout (binding = 0) uniform sampler2D cTex_GBuffAlbedo;
layout (binding = 1) uniform sampler2D cTex_GBuffNormal;

#if (o_texture_array0)  
	SAMPLER_TEX_2D_ARRAY(0); 
#else
	SAMPLER_TEX_2D(0); 
#endif 

#if (o_texture_array1)  
	SAMPLER_TEX_2D_ARRAY(1); 
#else
	SAMPLER_TEX_2D(1); 
#endif 

#if (o_texture_array2)  
	SAMPLER_TEX_2D_ARRAY(2); 
#else
	SAMPLER_TEX_2D(2); 
#endif 

#if (o_texture_array3)  
	SAMPLER_TEX_2D_ARRAY(3); 
#else
	SAMPLER_TEX_2D(3); 
#endif 

#if (o_texture_array4)  
	SAMPLER_TEX_2D_ARRAY(4); 
#else
	SAMPLER_TEX_2D(4); 
#endif 

#if (o_texture_array5)  
	SAMPLER_TEX_2D_ARRAY(5); 
#else
	SAMPLER_TEX_2D(5); 
#endif 

#if (o_texture_array6)  
	SAMPLER_TEX_2D_ARRAY(6); 
#else
	SAMPLER_TEX_2D(6); 
#endif 

#if (o_texture_array7)  
	SAMPLER_TEX_2D_ARRAY(7); 
#else
	SAMPLER_TEX_2D(7); 
#endif 

layout (location = 0) in vec4 fTexCoords0;
//tex coords 2 -> 5 go here TODO #if with expected locations
layout (location = 1) in vec4 fNormals;
layout (location = 2) in vec4 fTangents;
layout (location = 3) in vec4 fColor;
layout (location = 4) in vec4 fToonSpecCoords;
layout (location = 5) in vec4 fScreenCoords;

//TODO check the rest
layout (location = 7) in vec4 fTexCoordsBake; //xy shadow, zw lightmap
layout (location = 8) in vec4 fViewDirection;

layout (location = 10) in vec4 fTexCoords23;
layout (location = 11) in vec4 fTexCoords4;

layout (location = 0) out vec4 oMaterialID;
layout (location = 1) out vec4 oAlbedoColor;
layout (location = 3) out vec4 oNormals;
layout (location = 5) out vec4 oEmission;

//deferred shading material IDs
//Chara uses cell shading
const float CHARA_SKIN       = 0.0470588244; //material_behave 101
const float CHARA_METAL	   	 = 0.0625007; //material_behave 1 or (2 if pixels with metal map > 0.5)
const float CHARA_NONMETAL	 = 0.03529412; //material_behave 2 if pixels with metal map <= 0.5
const float CHARA_NONMETAL_2 = 0.0313725509; //material_behave 0
const float CHARA_HAIR		 = 0.0431372561; //material_behave 100
const float CHARA_GROSSY     = 0.039215688; //material_behave 10
const float CHARA_EYE        = 0.0509803928; //material_behave 104

const float UNK1_ID          = 0.0196078438; //
const float UNK2_ID          = 0.0627451017; //

const float FIELD            = 0; //material_behave 0, material_attribute 10 ?

const float FIELD_MASK        = 0.0156862754; //material_behave 105, material_attribute 10
 

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

vec4 GetComp(vec4 v, int comp_mask)
{
    switch (comp_mask)
    {
        case 0:  return v.rgba;
        case 10: return v.rrrr;
        case 20: return v.gggg;
        case 30: return v.bbbb;
        case 40: return v.aaaa;
        case 41: return v.aaaa; //Alpha as scalar?
    }
    return v.rgba;
}

int ConvertFloatToByte(float v)
{
	return int(trunc(v * 255.0));
}

float ConvertByteToFloat(int v)
{
	return float(v * 0.0039215688593685626983642578125);
}

struct GBufferEncode
{
	float SpecularPack;
	float MetalPack;
	float ShadingPack;
};

void EncodeGBuffer(float spec_mask, float metal, float ao, out GBufferEncode gbuffer)
{
	gbuffer.SpecularPack	     = ((ConvertFloatToByte(spec_mask) & 248) | 4);
	gbuffer.MetalPack			 = ((ConvertFloatToByte(metal) & 252) | 2);
	gbuffer.ShadingPack		     = ((ConvertFloatToByte(ao) & 252) | 2);
}

//TODO array types
#define SAMPLE_TEX(num) \
	 texture(cTexture##num, GetTexCoords(o_texture##num##_texcoord)) \

#define SAMPLE_TEX_ARRAY(num) \
	 texture(cTexture##num, GetTexCoords(o_texture##num##_texcoord), mat.p_texture_array_index##num) \

vec4 GetTexture(int flag)
{
	if (flag == 0)	      return SAMPLE_TEX(0);
    else if (flag == 1)   return SAMPLE_TEX(1);
    else if (flag == 2)   return SAMPLE_TEX(2);
    else if (flag == 3)   return SAMPLE_TEX(3);
    else if (flag == 4)   return SAMPLE_TEX(4);
    else if (flag == 5)   return SAMPLE_TEX(5);
    else if (flag == 6)   return SAMPLE_TEX(6);
    else if (flag == 7)   return SAMPLE_TEX(7);
	return vec4(0.0);
}


#define CalculateIndirect(num) \
	GetTexture(o_indirect##num##_texcoord) * mat.p_indirect_scale##num  \

vec4 CalculateAlbedoColor(int tex_num)
{
	return GetTexture(tex_num);
}

vec4 CalculateEmissiveColor(int tex_num)
{
	return GetTexture(tex_num);
}

vec4 CalculateOutput(int flag)
{
	//Texture 0 -> 7
    if		(flag == 0)   return GetTexture(0);
    else if (flag == 1)   return GetTexture(1);
    else if (flag == 2)   return GetTexture(2);
    else if (flag == 3)   return GetTexture(3);
    else if (flag == 4)   return GetTexture(4);
    else if (flag == 5)   return GetTexture(5);
    else if (flag == 6)   return GetTexture(6);
    else if (flag == 7)   return GetTexture(7);
	//Textures again but why?
    else if (flag == 200) return GetTexture(0);
    else if (flag == 201) return GetTexture(1);
    else if (flag == 202) return GetTexture(2);
    else if (flag == 203) return GetTexture(3);
    else if (flag == 204) return GetTexture(4);

    else if (flag == 300) return vec4(1.0);

    return vec4(0.0);
}

vec3 ReconstructNormal(in vec2 t_NormalXY) {

	vec2 t_Normal_Map = t_NormalXY;
	//Unorm -> Snorm
	if (o_enable_decode_normalmap)
	    t_Normal_Map = t_Normal_Map * 2.007874 - 1.00787401;

    float t_NormalZ = sqrt(clamp(1.0 - dot(t_Normal_Map.xy, t_Normal_Map.xy), 0.0, 1.0));
    return vec3(t_Normal_Map.xy, t_NormalZ);
}

vec3 CalculateNormals(vec2 normals)
{
	vec3 N = vec3(normals, 1);
	vec3 T = vec3(fTangents.xyz);
	vec3 B = normalize(cross(N, T) * fTangents.w);

	mat3 tbn_matrix = mat3(T, B, N);

	vec3 tangent_normal = N;
	if (o_enable_normalmap)
	{
		vec2 normal_map = CalculateOutput(o_normal_color).rg;
		tangent_normal = ReconstructNormal(normal_map);
	}

	if (o_enable_normalmap2)
	{
		float normal_blend_ratio = GetComp(CalculateOutput(o_normalmap_blend_ratio), o_normalmap_blend_channel).r;

		vec2 normal_map2 = CalculateOutput(o_normal_color2).rg;
        vec3 tangent_normal2 = ReconstructNormal(normal_map2);

		vec3 normal_world0 = normalize(tbn_matrix * tangent_normal).xyz;
        vec3 normal_world1 = normalize(tbn_matrix * tangent_normal2).xyz;

        return normalize(mix(normal_world0, normal_world1, normal_blend_ratio));
    }
	return normalize(tbn_matrix * tangent_normal).xyz;
}

void ComputeMaterialBehavior(inout vec3 albedo, inout float metalness, inout float material_id) //o_material_behave
{
	//All the types

	//Field
	if (o_material_attribute == 10)
	{
		material_id = 0;
		if (o_material_behave == 105) //Field Leaf
		{
			material_id = FIELD_MASK; 
		}
		return;
	}

	if (o_material_behave == 0) //Non metal
	{
		material_id = CHARA_NONMETAL;
	}
	else if (o_material_behave == 1) //Metal
	{
	    metalness  = GetComp(CalculateOutput(o_metal_color), o_metal_channel).r;
		material_id = CHARA_METAL;
	}
	else if (o_material_behave == 2) //Non metal or metal depending on metalness map
	{
		metalness  = GetComp(CalculateOutput(o_metal_color), o_metal_channel).r;
		material_id = metalness > 0.5 ? CHARA_METAL : CHARA_NONMETAL_2;
	}
	else if (o_material_behave == 10) //Grossy mat
	{
		material_id = CHARA_GROSSY;
	}
	else if (o_material_behave == 100) //Hair
	{
		material_id = CHARA_HAIR;
	}
	else if (o_material_behave == 101) //Skin (has sun burn/frost bite)
	{
		 //todo how is this defined?
		 //This is the "red" texture which defines the areas to apply sun burn/frost bite
		float tex_effect = CalculateOutput(4).r;

		vec3 color_diff = albedo - mat.p_const_color1.rgb;
		float value_diff = mix(-mat.p_const_value1, mat.p_const_value1, tex_effect);

		albedo = mix(albedo, mat.p_const_color1.rgb + color_diff * value_diff, tex_effect);

		material_id = CHARA_SKIN;

	}
	else if (o_material_behave == 103) //Field Water
	{
		material_id = 0; //TODO
	}
	else if (o_material_behave == 104) //Eye with proc texture. Adds layer to albedo
	{
		material_id = 0; //TODO
	}
	else 
	{
		material_id = 0; //TODO
	}
}



void main()
{	
	vec4 base_color              = CalculateOutput(o_albedo_color);

	float spec_mask      = GetComp(CalculateOutput(o_specmask_scaler), o_specular_channel).r;
	float transparency   = GetComp(CalculateOutput(o_transparency_color), o_transparency_channel).r;
	vec4 transmission    = GetComp(CalculateOutput(o_transmission_color), o_transmission_channel);
	float ao             = GetComp(CalculateOutput(o_ao_color), o_ao_channel).r;


	vec4 emission                = CalculateOutput(o_emission_color);

	float metalness = 1.0;

	ComputeMaterialBehavior(base_color.rgb, metalness, oMaterialID.x); //material ID to display in the deferred pass
	oMaterialID.y = mat.p_object_attribute; //always set as Y

	float specular_encoded = 0.0;

	GBufferEncode gfbuffer;
	EncodeGBuffer(spec_mask, metalness, ao, gfbuffer);

	vec2 normals = CalculateNormals(fNormals.xy).xy;

	oAlbedoColor.xyz = base_color.rgb; 
	oAlbedoColor.a = 1.0; //sort of shading affect. Todo not working correctly, set to 1.0 for now

	float alpha = transparency;

    //Alpha test
    if (gsys_alpha_test_enable)
    {
        switch (gsys_alpha_test_func)
        {
            case 0: //gequal
                if (alpha <= mat.gsys_alpha_test_ref_value)
                {
                     discard;
                }
            break;
            case 1: //greater
                if (alpha < mat.gsys_alpha_test_ref_value)
                {
                     discard;
                }
            break;
            case 2: //equal
                if (alpha == mat.gsys_alpha_test_ref_value)
                {
                     discard;
                }
            break;
            case 3: //less
                if (alpha > mat.gsys_alpha_test_ref_value)
                {
                     discard;
                }
            break;
            case 4: //lequal
                if (alpha >= mat.gsys_alpha_test_ref_value)
                {
                     discard;
                }
            break;
        }
    }

	oNormals.xy = normals.xy; 
	oNormals.z = gfbuffer.ShadingPack;
	oNormals.a = gfbuffer.SpecularPack;

	if (o_enable_emission)
		oEmission = emission;
	else
		oEmission = vec4(0.0);

	//storage buffer calculations

	//Calculate index for shader info tile grid
	float index_x = gl_FragCoord.x * context.data[146].x; //w
	float index_y = gl_FragCoord.y * context.data[146].y; //h
	//TODO is this correct???
	int index = int(index_x) * int(index_y) + int(index_x * context.data[146].z);
	sceneShadingInfo.Tiles[index >> 2] = 1u;
    return;
}
