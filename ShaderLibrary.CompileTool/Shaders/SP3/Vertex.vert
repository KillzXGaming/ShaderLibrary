#version 450

#define vertex_color 0
#define enable_fog_y 0
#define enable_fog_z 0

#define gsys_assign_material 0
#define gsys_assign_zonly 1

#define gsys_weight 0
#define gsys_assign_type gsys_assign_material

#define SCREEN_COORDS true // todo find macro that needs these. blitz_paint_type might use this?

// Note that tex coord 1 is reserved for bake, so 0 2 3 are valid coords
// Values over 3 may be other map types like projection, sphere, etc
#define texcoord_calc_texcoord0 0
#define texcoord_calc_texcoord2 0
#define texcoord_calc_texcoord3 0

// Selectors which determine what tex coord to use
// Possible values : 0 2 3
#define texcoord_select_albedo 0
#define texcoord_select_normal 0
#define texcoord_select_teamcolormap 0
#define texcoord_select_rghmap 0
#define texcoord_select_mtlmap 0
#define texcoord_select_emmmap 0
#define texcoord_select_trsmap 0
#define texcoord_select_ao 0
#define texcoord_select_comppaint 0
#define texcoord_select_sfxmask 0
#define texcoord_select_res0 0
#define texcoord_select_res1 0
#define texcoord_select_res2 0
#define texcoord_select_opacity 0

#define enable_bake_shadow 0
#define enable_bake_ao 0

#define bake_light_type -1
#define bake_shadow_type -1

const int MAX_BONE_COUNT = 120;

layout(binding = 0, std140) uniform Context
{
    mat3x4 cView; 
    mat4 cViewProj;
    mat4 cProj;
    mat3x4 cViewInv; 
    vec4 cNearFar; //znear, zfar, ratio, inverse ratio
    vec4 cScreen; //screen width/height
    vec4 cDist; //zfar - znear
    vec4 cFovParams; //fov
    vec4 cf;
    vec4 cc;
    vec4 sf;
    mat3x4 cPrevView;
    mat4 cPrevViewProj; // [24]
    mat4 cPrevProj; // [28]
    mat3x4 cPrevViewInv; // [31]
    mat3x4 cProjectionTexMtx0; // [35]
    mat3x4 cProjectionTexMtx1; // [38]
    vec4 cProjParams; // [39]
    mat4 cCascadeMtx0; // [40]
    mat4 cCascadeMtx1;
    mat4 cCascadeMtx2;
    mat4 cCascadeMtx3; // [52]
    vec4 cShadowParam1; // [56]
    vec4 cShadowParam2; // [57]
    mat4 cDepthShadow; // [60]

    vec4 cCubemapHDR;
} context;


layout(binding = 1, std140) uniform ShpMtx
{
    mat3x4 cTransform;
	vec4 cParams; //x = skin count
} shape;

layout(binding = 2, std140) uniform Mat
{
    float opacity;
    float gsys_xlu_zprepass_alpha;
    float refract_intensity;
    float refract_depth_fog_start;
    vec4 refract_depth_fog_end;
    vec4 refract_depth_fog_color;
    float refract_opa_start;
    float refract_opa_end;
    float refract_caustics_pow;
    float refract_caustics_intens;
    vec4 refract_caustics_color;
    float refract_caustics_uv_scale;
    float refract_caustics_uv_height_scale;
    float glass_thickness;
    float thick_glass_tex_coord_offset;
    float thick_glass_opacity_offset;
    float model_dither_normal_power;
    vec2 model_dither_time_shift_amount;
    vec4 albedo_color;
    float team_color_blend;
    float team_color_blend_alpha;
    float multi_normal_weight;
    float roughness;
    float metalness;
    float fabric_tone_param;
    float fabric_peak_offset;
    float fabric_peak_stop;
    float fabric_peak_intens;
    float fabric_noise_intens;
    float fabric_noise_decay;
    float envmap_mip_bias;
    vec4 emission_intensity;
    vec4 emission_color;

    float emission_normalize_offset; //vp_c5.data[13].x
    float night_emission_intensityX; //vp_c5.data[14].x
    float night_emission_intensityY;
    float night_emission_intensityZ; 

    vec4 night_emission_color;

    float emission_intens_in_envmap;
    float emission_intens_not_in_envmap;
    float night_emission_intens_in_envmap;
    float saturation_in_envmap;

    float saturation_offset_in_envmap;
    float inner_light_diffusivity;
    vec2 inner_light_min_clamp;

    vec3 inner_light_pos_offset;
    float transmission_rate;

    float scattering_rate;
    float edge_transmission_powerX;
    float edge_transmission_powerY;
    float edge_transmission_powerZ;

    vec4 transmission_color_backlight;

    float transmission_attenu_pos;
    float transmission_attenu_range;
    float transmission_attenu_min;
    float light_occlude_check_len;

    vec4 gsys_bake_st0;
    vec4 gsys_bake_st1;
    vec4 bake_tex_st0;
    vec4 bake_tex_st1;

    vec2 col_paint_uv_offset;
    float two_color_complement_paint_intensity;
    float two_comp_paint_team;

    float thr_comp_paint_intens_alpha;
    float thr_comp_paint_intens_bravo;
    float thr_comp_paint_intens_charlie;
    float comp_paint_texcoord_offset;

    float comp_paint_norm_intens;
    float comp_paint_lame_scale;
    float rainbow_ink_hue_range;
    float rainbow_ink_hue_offset;

    float private_paint_thickness;
    float private_paint_thickness_ratio;
    float ink_sheet_edge_radius;
    float display_team_type;

    float kebaink_marble_noise_scroll_anim;
    float kebaink_marble_noise_densityX;
    float kebaink_marble_noise_densityY;
    float kebaink_marble_noise_densityZ;

    vec4 kebaink_multi_color;

    float kebaink_marble_roughness;
    float kebaink_fur_roughness;
    float kebaink_mask_threshold;
    float kebaink_mask_vertex_noise_density;

    float kebaink_mask_vertex_noise_scale;
    float kebaink_mask_vertex_fin_threshold;
    float kebaink_mask_noise_density;
    float kebaink_mask_noise_scale;

    vec3 kebaink_core_pos;
    float kebaink_core_radius;

    float kebaink_mask_from_core_dist;
    float kebaink_mask_from_core_noiseX;
    float kebaink_mask_from_core_noiseY;
    float kebaink_mask_from_core_noiseZ;

    vec4 kebaink_darken_color;

    float kebaink_darken_dist_offset;
    float kebaink_darken_dist_noise;
    float kebaink_wave_mask_dist_offset;
    float kebaink_wave_mask_gradation;

    float kebaink_wave_intens;
    float kebaink_wave_freq;
    float kebaink_wave_phase;
    float scatter_distance;

    vec4 scattering_color;
    vec4 fabric_blend_color;

    float fabric_facing_gain;
    float fabric_facing_bias;
    float manual_fresnelX;
    float manual_fresnelY;

    vec4 manual_fresnel_color;

    float film_transmission_power;
    float film_transmission_rateX;
    float film_transmission_rateY;
    float film_transmission_rateZ;

    vec4 under_film_color;

    float edge_light_intens;
    float edge_light_powerX;
    float edge_light_powerY;
    float edge_light_powerZ;

    vec4 edge_light_color;

    float indirect_intens;
    float spec_glaze_intens;
    float specular_intensityX;
    float specular_intensityY;

    vec4 specular_color;

    float specular_roughness;
    float planer_ref_spec_offset;
    float planer_ref_normal_intens;
    float reflector_intens;

    float reflector_scroll;
    float reflector_power;
    float reflector_uniform_norm_coefX;
    float reflector_uniform_norm_coefY;

    vec4 flake_color;

    float flake_roughness;
    float big_lame_appearance;
    float big_lame_epsilon;
    float micro_flakes_diffuse_rate;

    float micro_flakes_specular_rate;
    float flake_rare_intens;
    float flake_rare_appearance;
    float flake_rare_offset;

    float flake_rare_shadow_intes;
    float emission_eq_bokeh;
    float emission_eq_channel_a;
    float emission_eq_channel_b;

    float emission_eq_channel_c;
    float emission_eq_channel_d;
    float emission_eq_channel_e;
    float emission_fader_channel_a;

    float emission_fader_channel_b;
    float emission_fader_channel_c;
    float emission_fader_channel_d;
    float emission_fader_channel_e;

    float mirror_material_eff;
    float aniso_pararell_speculality;
    float aniso_perpendicular_speculality;
    float aniso_specular_bokeh;

    float aniso_normal_mask;
    float decal_depth_range;
    float decal_depth_alpha_coef;
    float polygonal_light_mask;

    float monochrome_filter_saturation;
    float phantom_edge_offset_in_material;
    float parallax_height;
    float parallax_height_darken;

    float parallax_height_darken_trans;
    float parallax_occlusion_height;
    float parallax_occlusion_ray_clamp;
    float parallax_occlusion_map_uv_scale;

    float parallax_occulusion_height_darken;
    float parallax_occlusion_shadow;
    float parallax_fur_dilate;
    float fur_dilate_edge_angle;

    float parallax_occlusion_fur_fin_height_scale;
    float parallax_occlusion_fur_fin_height_offset;
    float parallax_fur_occlusion_off_dist_offset;
    float parallax_fur_fin_off_dist_offset;

    float fur_fin_length;
    float fur_fin_density;
    float fur_fin_gravity;
    float fur_fin_offset;

    float fur_fin_cull_angle;
    float fur_fin_depth_write;
    float fur_shell_length;
    float fur_shell_density;

    float fur_shell_thickness;
    float fur_shell_gravity;
    float fur_shell_offsetX;
    float fur_shell_offsetY;

    vec4 fur_shell_root_color;

    float fur_shell_sharp_amount;
    float interior_map_depth;
    float interior_map_x_depth_offset;
    float interior_map_y_depth_offset;

    vec2 interior_map_uv_aspect;
    vec2 interior_map_uv_scale;

    float interior_map_shadow_scale;
    float interior_map_night_shadow_scale;
    float interior_map_shadow_gradation_coef;
    float interior_map_emission_intens;

    float interior_map_night_emission_intens;
    float interior_map_night_color_scale;
    float interior_mask_depthX;
    float interior_mask_depthY;

    vec4 interior_mask_night_color;

    float interior_mask_night_color_intens;
    float vat_anim_pos;
    float graffiti_normal_rateX;
    float graffiti_normal_rateY;

    vec4 graffiti_back_color;

    float graffiti_back_color_rate;
    float holo_graffiti_normal_scale;
    float holo_graffiti_hueX;
    float holo_graffiti_hueY;

    vec4 holo_graffiti_base_color;

    float holo_graffiti_rainbow_scale;
    float holo_graffiti_color_intensity;
    float holo_graffiti_rainbow_width;
    float holo_graffiti_rainbow_blur_width;

    vec4 mantaking_shadow_color;

    float holo_model_normal_scale;
    float holo_model_rainbow_scale;
    float holo_model_color_intensity;
    float holo_model_rainbow_width;

    float holo_model_rainbow_blur_width;
    float camera_xlu_draw_bottom_fade_out_height;
    float camera_xlu_draw_bottom_fade_in_height;
    float field_lame_uv_scale;

    vec4 field_lame_intensity;
    vec4 field_lame_color;
    vec4 const_color0;
    vec4 const_color1;
    vec4 const_color2;

    float const_value0;
    float const_value1;
    float const_value2;
    float const_value3;

    float const_value4;
    float const_value5;
    float const_value6;
    float const_value7;

    float const_value8;
    float const_value9X;
    float const_value9Y;
    float const_value9Z;

    vec4 const_vector0;
    vec4 const_vector1;
    vec4 const_vector2;
    vec4 const_vector3;
    vec4 my_team_color;
    vec4 my_team_color_bright;
    vec4 my_team_color_dark;
    vec4 my_team_color_hue_bright;
    vec4 my_team_color_hue_bright_half;
    vec4 my_team_color_hue_dark;
    vec4 my_team_color_hue_dark_half;
    vec4 my_team_color_hue_complement;
    vec4 my_alpha_team_color;
    vec4 my_bravo_team_color;
    vec4 my_charlie_team_color;
    vec4 my_alpha_team_color_hue_bright;
    vec4 my_bravo_team_color_hue_bright;
    vec4 my_charlie_team_color_hue_bright;
    vec4 my_alpha_team_color_hue_dark;
    vec4 my_bravo_team_color_hue_dark;
    vec4 my_charlie_team_color_hue_dark;
    vec4 player_skin_color;
    vec4 team_flag;
    mat2x4 tex_mtx0;
    mat2x4 tex_mtx1;
    mat2x4 tex_mtx2;

    float tex_repetition_noise_division_num;
    float tex_repetition_noise_scale;
    float tex_repetition_angle_deg;
    float model_y_fog_up_start;

    vec4 model_y_fog_up_end;
    vec4 model_y_fog_up_color;

    vec3 model_y_fog_up_dir;
    float model_y_fog_down_start;

    vec4 model_y_fog_down_end;
    vec4 model_y_fog_down_color;

    vec3 model_y_fog_down_dir;
    float dynamic_vert_alpha_coeff;

    float fade_dither_alpha;
    float fade_dither_manual_alpha;
    vec2 camera_xlu_alpha;
    vec4 fade_player_pos;
    float map_min_height;
    vec3 map_max_height;
    vec4 map_ink_gradation_rate;
    vec4 map_min_gradation_color;
    vec4 map_max_gradation_color;
    vec4 map_ink_color_bright_offset;
    float shell_fur_scale;
    vec3 actor_instance_id;
    vec4 mantaking_parameter0;
    vec4 is_use_graffiti_bake_paint_uv;
    vec4 camera_xlu_moire_param0;
    vec4 camera_xlu_moire_param1;
    vec4 depth_silhouette_color;
    float height_draw_mode;
    float gsys_alpha_test_ref_value;
} mat;

layout(binding = 3, std140) uniform BlitzUBO0
{
    vec4 _m0[4096];
} blitzUBO0;

layout(binding = 4, std140) uniform BlitzUBO1
{
    vec4 _m0[4096];
} blitzUBO1;

layout (binding = 5, std140) uniform Mtx
{
    mat3x4 cBoneMatrices[MAX_BONE_COUNT];
};

layout(binding = 6, std140) uniform ShaderOption
{
    vec4 _m0[4096];
} shaderOption;

layout(binding = 0) uniform sampler2D cTexSfxMask;
layout(binding = 1) uniform sampler2D cTexResource0;
layout(binding = 2) uniform sampler2D cTexResource1;
layout(binding = 3) uniform sampler2D cTexCompPaint;

layout(location = 0) in vec4 aPosition;
layout(location = 1) in vec4 aNormal;
layout(location = 2) in vec4 aTangent;
layout(location = 3) in vec4 aColor1;
layout(location = 4) in vec4 aBlendWeight0;
layout(location = 5) in vec4 aBlendWeight1;
layout(location = 6) in ivec4 aBlendIndex0;
layout(location = 7) in vec4 aBlendIndex1;
layout(location = 8) in vec4 aTexCoord0;
layout(location = 9) in vec4 aTexCoord1;
layout(location = 10) in vec4 aTexCoord2;
layout(location = 11) in vec4 aTexCoord3;
layout(location = 12) in vec4 aColor;
layout(location = 13) in vec4 aPaintUVTangent;
layout(location = 14) in vec4 aPaintUV;
layout(location = 15) in vec4 aPaintUVSwitch;

// Expected order based on debug shader
// layout(location = 0) out vec4 _45; fTexCoords0
// layout(location = 1) out vec4 _47; fFogDir
// layout(location = 2) out vec4 _49; fColor
// layout(location = 3) out vec4 _51; fNormals
// layout(location = 4) out vec4 _53; fTangents
// layout(location = 5) out vec4 _55; fVertexViewPos
// layout(location = 6) out vec4 _57; fViewDirection
// layout(location = 7) out vec4 _59; fVertexWorldPos (direct gl_Position)
// layout(location = 8) out vec4 _61; fFogParams
// layout(location = 15) out vec4 _75; fTexCoordsBake

/*
out vec4 fTexCoords0;
out vec4 fNormals;
out vec4 fFogDir;
out vec4 fColor;
out vec4 fTangents;
out vec4 fVertexViewPos; // vertex view pos
out vec4 fViewDirection; // uses view mtx pos and vertex pos (_57 in debug shader)
out vec4 fVertexWorldPos;
out vec4 fScreenCoords;*/

layout (location = 0) out vec4 fTexCoords0;
layout (location = 1) out vec4 fNormals;
layout (location = 2) out vec4 fTangents;
layout (location = 3) out vec4 fVertexViewPos; // vertex view pos
layout (location = 4) out vec4 fViewDirection; // uses view mtx pos and vertex pos (_57 in debug shader)
layout (location = 5) out vec4 fVertexWorldPos;
layout (location = 6) out vec4 fProjectionCoord;




layout (location = 7) out vec4 fFogDir;
layout (location = 8) out vec4 fColor;
layout (location = 9) out vec4 fFogParams;

// Todo find right location
layout (location = 13) out vec4 fScreenCoords;
layout (location = 14) out vec4 fTexCoordsBake;
layout (location = 15) out vec4 fTexCoords23;

vec4 skin(vec3 pos, ivec4 index, vec4 weight)
{
    vec4 newPosition = vec4(pos.xyz, 1.0);
    if (gsys_weight == 0)
        newPosition = vec4(pos, 1.0) * mat4(shape.cTransform);

    if (gsys_weight == 1)
        newPosition = vec4(pos, 1.0) * mat4(cBoneMatrices[index.x]);

    if (gsys_weight >  1)
        newPosition =  vec4(pos, 1.0) * mat4(cBoneMatrices[index.x]) * weight.x;
    if (gsys_weight >= 2)
        newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[index.y]) * weight.y;
    if (gsys_weight >= 3)
        newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[index.z]) * weight.z;
    if (gsys_weight >= 4)
        newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[index.w]) * weight.w;
        
    return newPosition;
}

vec3 skinNormal(vec3 nr, ivec4 index, vec4 weight)
{
    vec3 newNormal = nr;
    if (gsys_weight == 0)
        newNormal = nr * mat3(shape.cTransform);

    if (gsys_weight == 1)
        newNormal =  nr * mat3(cBoneMatrices[index.x]);

    if (gsys_weight >  1)
        newNormal =  nr * mat3(cBoneMatrices[index.x]) * weight.x;
    if (gsys_weight >= 2)
        newNormal += nr *  mat3(cBoneMatrices[index.y]) * weight.y;
    if (gsys_weight >= 3)
        newNormal += nr * mat3(cBoneMatrices[index.z]) * weight.z;
    if (gsys_weight >= 4)
        newNormal += nr * mat3(cBoneMatrices[index.w]) * weight.w;
    
    return newNormal;
}

vec2 calc_texcoord_matrix(mat2x4 mat, vec2 tex_coord)
{
	//actually a 2x3 matrix stored in 2x4
    vec2 tex_coord_out;
    tex_coord_out.x = fma(tex_coord.x, mat[0].x, tex_coord.y * mat[0].z) + mat[1].x;
    tex_coord_out.y = fma(tex_coord.x, mat[0].y, tex_coord.y * mat[0].w) + mat[1].y;
	return tex_coord_out;
}

vec2 get_tex_coord(vec2 tex_coord, mat2x4 mat, int type)
{
	if (type == 0)
		return calc_texcoord_matrix(mat, tex_coord);
	if (type == 4) //sphere mapping used on metal characters
	{
		//view normal
		vec3 view_n = (normalize(aNormal.xyz) * mat3(context.cView)).xyz;
		//center the uvs
		return view_n.xy * vec2(0.5) + vec2(0.5,-0.5);
	}
	return tex_coord;
}

vec2 CalcScaleBias(in vec2 t_Pos, in vec4 t_SB) {
    return t_Pos.xy * t_SB.xy + t_SB.zw;
}

void main()
{
	//position
	vec4 position = skin(aPosition.xyz, aBlendIndex0, aBlendWeight0);
	vec4 view_p = (vec4(position.xyz, 1.0) * mat4(context.cView));
    gl_Position = vec4(view_p.xyz, 1.0) * context.cProj;

	//material tex coords
	fTexCoords0.xy = get_tex_coord(aTexCoord0.xy, mat.tex_mtx0, texcoord_calc_texcoord0);	

    //Skip other calculations for depth shadow shader
    if (gsys_assign_type == gsys_assign_zonly)
        return;

    if (enable_fog_y == 1 || enable_fog_z == 1)
        fFogDir = vec4(0.0); // Todo

    if (vertex_color == 1)
	    fColor = aColor;

	//normals
	fNormals = vec4(skinNormal(aNormal.xyz, aBlendIndex0, aBlendWeight0).xyz, 1.0);
	fTangents = vec4(skinNormal(aTangent.xyz, aBlendIndex0, aBlendWeight0).xyz, 1.0);

    fVertexViewPos = view_p;

	//world pos - camera pos for eye position
	fViewDirection.xyz = position.xyz - vec3(
	   context.cViewInv[0].w,
	   context.cViewInv[1].w, 
	   context.cViewInv[2].w);

    fVertexWorldPos = gl_Position;

    fProjectionCoord = (vec4(position.xyz, 1.0) * mat4(context.cProjectionTexMtx0));

    if (SCREEN_COORDS)
    {
	    vec3 ndc = gl_Position.xyz / gl_Position.w; //perspective divide/normalize
	    //Flip needed
        ndc.y *= -1.0;

	    //used by screen effects (shadows, light prepass, color buffer)
        fScreenCoords.xy = ndc.xy * 0.5 + 0.5;
        fScreenCoords.xy *= gl_Position.w;
        fScreenCoords.zw = gl_Position.zw;
    }

    if (texcoord_select_albedo == 2 || 
       texcoord_select_normal == 2 || 
       texcoord_select_teamcolormap == 2 || 
       texcoord_select_rghmap == 2 || 
       texcoord_select_mtlmap == 2 || 
       texcoord_select_emmmap == 2 || 
       texcoord_select_trsmap == 2 ||
       texcoord_select_ao == 2 ||
       texcoord_select_comppaint == 2 ||
       texcoord_select_sfxmask == 2 ||
       texcoord_select_res0 == 2 ||
       texcoord_select_res1 == 2 ||
       texcoord_select_res2 == 2 ||
       texcoord_select_opacity == 2)
    {
	    fTexCoords0.zw = get_tex_coord(aTexCoord2.xy, mat.tex_mtx1, texcoord_calc_texcoord2);	
    }

    if (texcoord_select_albedo == 3 || 
       texcoord_select_normal == 3 || 
       texcoord_select_teamcolormap == 3 || 
       texcoord_select_rghmap == 3 || 
       texcoord_select_mtlmap == 3 || 
       texcoord_select_emmmap == 3 || 
       texcoord_select_trsmap == 3 ||
       texcoord_select_ao == 3 ||
       texcoord_select_comppaint == 3 ||
       texcoord_select_sfxmask == 3 ||
       texcoord_select_res0 == 3 ||
       texcoord_select_res1 == 3 ||
       texcoord_select_res2 == 3 ||
       texcoord_select_opacity == 3)
    {
	    fTexCoords23.xy = get_tex_coord(aTexCoord3.xy, mat.tex_mtx2, texcoord_calc_texcoord3);	
    }

	//bake texCoords
    if (bake_shadow_type != -1 || bake_light_type != -1)
    {
        fTexCoordsBake.xy = CalcScaleBias(aTexCoord1.xy, mat.gsys_bake_st0);
        fTexCoordsBake.zw = CalcScaleBias(aTexCoord1.xy, mat.gsys_bake_st1);
    }
}