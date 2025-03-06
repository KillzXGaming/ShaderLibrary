﻿#version 450

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
    float opacity; //@ default_value="1"
    float gsys_xlu_zprepass_alpha; //@ default_value="1"
    float refract_intensity; //@ default_value="0"
    float refract_depth_fog_start; //@ default_value="1"
    vec4 refract_depth_fog_end; //@ default_value="5 0 0 0"
    vec4 refract_depth_fog_color; //@ default_value="1 1 1 1"

    float refract_opa_start; //@ default_value="0"
    float refract_opa_end; //@ default_value="10"
    float refract_caustics_pow; //@ default_value="20"
    float refract_caustics_intens; //@ default_value="10"
    vec4 refract_caustics_color; //@ default_value="1 1 1 1"

    float refract_caustics_uv_scale; //@ default_value="0.2"
    float refract_caustics_uv_height_scale; //@ default_value="1"
    float glass_thickness; //@ default_value="0.1"
    float thick_glass_tex_coord_offset; //@ default_value="0.1"

    float thick_glass_opacity_offset; //@ default_value="0.2"
    float model_dither_normal_power; //@ default_value="1"
    float model_dither_time_shift_amountX; //@ default_value="0"
    float model_dither_time_shift_amountY; //@ default_value="0"
    vec4 albedo_color; //@ default_value="1 1 1 1"

    float team_color_blend; //@ default_value="0"
    float team_color_blend_alpha; //@ default_value="0"
    float multi_normal_weight; //@ default_value="0"
    float roughness; //@ default_value="0"

    float metalness; //@ default_value="0"
    float fabric_tone_param; //@ default_value="1"
    float fabric_peak_offset; //@ default_value="0.5"
    float fabric_peak_stop; //@ default_value="100"

    float fabric_peak_intens; //@ default_value="1"
    float fabric_noise_intens; //@ default_value="0.1"
    float fabric_noise_decay; //@ default_value="0"
    float envmap_mip_bias; //@ default_value="0"
    vec4 emission_intensity; //@ default_value="0 0 0 0"
    vec4 emission_color; //@ default_value="1 1 1 1"

    float emission_normalize_offset; //@ default_value="0"
    float night_emission_intensityX; //@ default_value="0"
    float night_emission_intensityY; //@ default_value="0"
    float night_emission_intensityZ; //@ default_value="0"
    vec4 night_emission_color; //@ default_value="1 1 1 1"

    float emission_intens_in_envmap; //@ default_value="1"
    float emission_intens_not_in_envmap; //@ default_value="1"
    float night_emission_intens_in_envmap; //@ default_value="1"
    float saturation_in_envmap; //@ default_value="1"

    float saturation_offset_in_envmap; //@ default_value="0"
    float inner_light_diffusivity; //@ default_value="0.5"
    float inner_light_min_clampX; //@ default_value="0"
    float inner_light_min_clampY; //@ default_value="0"

    vec3 inner_light_pos_offset; //@ default_value="0 0 0"
    float env_param_emission_material_gain; //@ default_value="1"

    float transmission_rate; //@ default_value="0.5"
    float scattering_rate; //@ default_value="1"
    float edge_transmission_powerX; //@ default_value="1.5"
    float edge_transmission_powerY; //@ default_value="0"
    vec4 transmission_color_backlight; //@ default_value="1 1 1 1"

    float transmission_attenu_pos; //@ default_value="50"
    float transmission_attenu_range; //@ default_value="100"
    float transmission_attenu_min; //@ default_value="0"
    float light_occlude_check_len; //@ default_value="10"
    vec4 gsys_bake_st0; //@ default_value="1 1 0 0"
    vec4 gsys_bake_st1; //@ default_value="1 1 0 0"
    vec4 bake_tex_st0; //@ default_value="1 1 0 0"
    vec4 bake_tex_st1; //@ default_value="1 1 0 0"

    vec2 col_paint_uv_offset; //@ default_value="0 0"
    float two_color_complement_paint_intensity; //@ default_value="0"
    float two_comp_paint_team; //@ default_value="0"

    float thr_comp_paint_intens_alpha; //@ default_value="0"
    float thr_comp_paint_intens_bravo; //@ default_value="0"
    float thr_comp_paint_intens_charlie; //@ default_value="0"
    float comp_paint_texcoord_offset; //@ default_value="0.005"

    float comp_paint_norm_intens; //@ default_value="1"
    float comp_paint_lame_scale; //@ default_value="1"
    float rainbow_ink_hue_range; //@ default_value="0"
    float rainbow_ink_hue_offset; //@ default_value="0"

    float private_paint_thickness; //@ default_value="0"
    float private_paint_thickness_ratio; //@ default_value="1"
    float ink_sheet_edge_radius; //@ default_value="0"
    float display_team_type; //@ default_value="0"

    float kebaink_marble_noise_scroll_anim; //@ default_value="0"
    float kebaink_marble_noise_densityX; //@ default_value="0.1"
    float kebaink_marble_noise_densityY; //@ default_value="0"
    float kebaink_marble_noise_densityZ; //@ default_value="0"
    vec4 kebaink_multi_color; //@ default_value="0 0 0 0"

    float kebaink_marble_roughness; //@ default_value="0"
    float kebaink_fur_roughness; //@ default_value="0"
    float kebaink_mask_threshold; //@ default_value="0.5"
    float kebaink_mask_vertex_noise_density; //@ default_value="0.5"

    float kebaink_mask_vertex_noise_scale; //@ default_value="1"
    float kebaink_mask_vertex_fin_threshold; //@ default_value="0.5"
    float kebaink_mask_noise_density; //@ default_value="0.5"
    float kebaink_mask_noise_scale; //@ default_value="1"

    vec3 kebaink_core_pos; //@ default_value="0 0 0"
    float kebaink_core_radius; //@ default_value="0"

    float kebaink_mask_from_core_dist; //@ default_value="0"
    float kebaink_mask_from_core_noiseX; //@ default_value="5"
    float kebaink_mask_from_core_noiseY; //@ default_value="0"
    float kebaink_mask_from_core_noiseZ; //@ default_value="0"
    vec4 kebaink_darken_color; //@ default_value="0 0 0 0"

    float kebaink_darken_dist_offset; //@ default_value="-3"
    float kebaink_darken_dist_noise; //@ default_value="10"
    float kebaink_wave_mask_dist_offset; //@ default_value="5"
    float kebaink_wave_mask_gradation; //@ default_value="0.3"

    float kebaink_wave_intens; //@ default_value="2"
    float kebaink_wave_freq; //@ default_value="1"
    float kebaink_wave_phaseX; //@ default_value="5"
    float kebaink_wave_phaseY; //@ default_value="0"

    vec2 comp_paint_random_uv; //@ default_value="0 0"
    float scatter_distanceX; //@ default_value="0"
    float scatter_distanceY; //@ default_value="0"
    vec4 scattering_color; //@ default_value="1 1 1 1"
    vec4 fabric_blend_color; //@ default_value="0 0 0 1"

    float fabric_facing_gain; //@ default_value="1"
    float fabric_facing_bias; //@ default_value="0"
    float manual_fresnelX; //@ default_value="1"
    float manual_fresnelY; //@ default_value="0"
    vec4 manual_fresnel_color; //@ default_value="1 1 1 1"

    float film_transmission_power; //@ default_value="1"
    float film_transmission_rateX; //@ default_value="0.5"
    float film_transmission_rateY; //@ default_value="0"
    float film_transmission_rateZ; //@ default_value="0"
    vec4 under_film_color; //@ default_value="1 1 1 1"

    float edge_light_intens; //@ default_value="0.5"
    float edge_light_powerX; //@ default_value="1.5"
    float edge_light_powerY; //@ default_value="0"
    float edge_light_powerZ; //@ default_value="0"
    vec4 edge_light_color; //@ default_value="1 1 1 1"

    float edge_main_light_ratio_dark; //@ default_value="0.15"
    float edge_main_light_ratio_bright; //@ default_value="0.15"
    float indirect_intens; //@ default_value="0"
    float spec_glaze_intens; //@ default_value="3"
    vec4 specular_intensity; //@ default_value="0 0 0 0"
    vec4 specular_color; //@ default_value="1 1 1 1"

    float specular_roughness; //@ default_value="0"
    float planer_ref_spec_offset; //@ default_value="0"
    float planer_ref_normal_intens; //@ default_value="0"
    float reflector_intens; //@ default_value="1"

    float reflector_scroll; //@ default_value="1"
    float reflector_power; //@ default_value="1"
    float reflector_uniform_norm_coefX; //@ default_value="1"
    float reflector_uniform_norm_coefY; //@ default_value="0"
    vec4 flake_color; //@ default_value="1 1 1 1"

    float flake_roughness; //@ default_value="0.1"
    float big_lame_appearance; //@ default_value="3"
    float big_lame_epsilon; //@ default_value="0.05"
    float micro_flakes_diffuse_rate; //@ default_value="1"

    float micro_flakes_specular_rate; //@ default_value="1"
    float flake_rare_intens; //@ default_value="1"
    float flake_rare_appearance; //@ default_value="10"
    float flake_rare_offset; //@ default_value="0"

    float flake_rare_shadow_intes; //@ default_value="0"
    float emission_eq_bokeh; //@ default_value="1"
    float emission_eq_channel_a; //@ default_value="0"
    float emission_eq_channel_b; //@ default_value="0"

    float emission_eq_channel_c; //@ default_value="0"
    float emission_eq_channel_d; //@ default_value="0"
    float emission_eq_channel_e; //@ default_value="0"
    float emission_fader_channel_a; //@ default_value="1"

    float emission_fader_channel_b; //@ default_value="1"
    float emission_fader_channel_c; //@ default_value="1"
    float emission_fader_channel_d; //@ default_value="1"
    float emission_fader_channel_e; //@ default_value="1"

    float mirror_material_eff; //@ default_value="1"
    float aniso_pararell_speculality; //@ default_value="1"
    float aniso_perpendicular_speculality; //@ default_value="1"
    float aniso_specular_bokeh; //@ default_value="1"

    float aniso_normal_mask; //@ default_value="0"
    float decal_depth_range; //@ default_value="0.1"
    float decal_depth_alpha_coef; //@ default_value="10"
    float polygonal_light_mask; //@ default_value="1"

    float monochrome_filter_saturation; //@ default_value="1"
    float phantom_edge_offset_in_material; //@ default_value="0"
    float parallax_height; //@ default_value="1"
    float parallax_height_darken; //@ default_value="0.5"

    float parallax_height_darken_trans; //@ default_value="0.5"
    float parallax_occlusion_height; //@ default_value="1"
    float parallax_occlusion_ray_clamp; //@ default_value="4"
    float parallax_occlusion_map_uv_scale; //@ default_value="1"

    float parallax_occulusion_height_darken; //@ default_value="0.5"
    float parallax_occlusion_shadow; //@ default_value="1"
    float parallax_fur_dilate; //@ default_value="0.2"
    float fur_dilate_edge_angle; //@ default_value="0.4"

    float parallax_occlusion_fur_fin_height_scale; //@ default_value="6"
    float parallax_occlusion_fur_fin_height_offset; //@ default_value="0.2"
    float parallax_fur_occlusion_off_dist_offset; //@ default_value="0"
    float parallax_fur_fin_off_dist_offset; //@ default_value="0"

    float fur_fin_length; //@ default_value="0.1"
    float fur_fin_density; //@ default_value="1"
    float fur_fin_gravity; //@ default_value="0"
    float fur_fin_offset; //@ default_value="0"

    float fur_fin_cull_angle; //@ default_value="0.4"
    float fur_fin_depth_write; //@ default_value="0"
    float fur_shell_length; //@ default_value="0.1"
    float fur_shell_density; //@ default_value="1"

    float fur_shell_thickness; //@ default_value="1"
    float fur_shell_gravity; //@ default_value="0"
    float fur_shell_offsetX; //@ default_value="0"
    float fur_shell_offsetY; //@ default_value="0"
    vec4 fur_shell_root_color; //@ default_value="1 1 1 1"

    float fur_shell_sharp_amount; //@ default_value="0"
    float interior_map_depth; //@ default_value="1"
    float interior_map_x_depth_offset; //@ default_value="0"
    float interior_map_y_depth_offset; //@ default_value="0"

    vec2 interior_map_uv_aspect; //@ default_value="1 0"
    float interior_map_uv_scaleX; //@ default_value="1"
    float interior_map_uv_scaleY; //@ default_value="1"

    float interior_map_shadow_scale; //@ default_value="0.98"
    float interior_map_night_shadow_scale; //@ default_value="0.98"
    float interior_map_shadow_gradation_coef; //@ default_value="0.2"
    float interior_map_emission_intens; //@ default_value="0"

    float interior_map_night_emission_intens; //@ default_value="0"
    float interior_map_night_color_scale; //@ default_value="1"
    float interior_mask_depthX; //@ default_value="0"
    float interior_mask_depthY; //@ default_value="0"
    vec4 interior_mask_night_color; //@ default_value="1 1 1 1"

    float interior_mask_night_color_intens; //@ default_value="1"
    float vat_anim_pos; //@ default_value="0"
    float graffiti_normal_rateX; //@ default_value="0"
    float graffiti_normal_rateY; //@ default_value="0"
    vec4 graffiti_back_color; //@ default_value="0.5 0.5 0.5 1"

    float graffiti_back_color_rate; //@ default_value="1"
    float holo_graffiti_normal_scale; //@ default_value="1"
    float holo_graffiti_hueX; //@ default_value="0"
    float holo_graffiti_hueY; //@ default_value="0"
    vec4 holo_graffiti_base_color; //@ default_value="0.5 0.5 0.5 1"

    float holo_graffiti_rainbow_scale; //@ default_value="2"
    float holo_graffiti_color_intensity; //@ default_value="0.34"
    float holo_graffiti_rainbow_width; //@ default_value="0.1"
    float holo_graffiti_rainbow_blur_width; //@ default_value="0.12"
    vec4 mantaking_shadow_color; //@ default_value="0 0 0 1"

    float holo_model_normal_scale; //@ default_value="1"
    float holo_model_rainbow_scale; //@ default_value="2"
    float holo_model_color_intensity; //@ default_value="0.34"
    float holo_model_rainbow_width; //@ default_value="0.1"

    float holo_model_rainbow_blur_width; //@ default_value="0.12"
    float camera_xlu_draw_bottom_fade_out_height; //@ default_value="0"
    float camera_xlu_draw_bottom_fade_in_height; //@ default_value="0"
    float field_lame_uv_scale; //@ default_value="6"

    float field_lame_intensity; //@ default_value="1"
    float field_lame_rare_intensity; //@ default_value="15"
    float field_lame_normal_random_rateX; //@ default_value="0.07"
    float field_lame_normal_random_rateY; //@ default_value="0"
    vec4 field_lame_color; //@ default_value="0 0 0 1"
    vec4 const_color0; //@ default_value="1 1 1 1"
    vec4 const_color1; //@ default_value="1 1 1 1"
    vec4 const_color2; //@ default_value="1 1 1 1"

    float const_value0; //@ default_value="0"
    float const_value1; //@ default_value="0"
    float const_value2; //@ default_value="0"
    float const_value3; //@ default_value="0"

    float const_value4; //@ default_value="0"
    float const_value5; //@ default_value="0"
    float const_value6; //@ default_value="0"
    float const_value7; //@ default_value="0"

    float const_value8; //@ default_value="0"
    float const_value9X; //@ default_value="0"
    float const_value9Y; //@ default_value="0"
    float const_value9Z; //@ default_value="0"
    vec4 const_vector0; //@ default_value="0 0 0 0"
    vec4 const_vector1; //@ default_value="0 0 0 0"
    vec4 const_vector2; //@ default_value="0 0 0 0"
    vec4 const_vector3; //@ default_value="0 0 0 0"
    vec4 my_team_color; //@ default_value="1 1 1 1"
    vec4 my_team_color_bright; //@ default_value="1 1 1 1"
    vec4 my_team_color_dark; //@ default_value="1 1 1 1"
    vec4 my_team_color_hue_bright; //@ default_value="1 1 1 1"
    vec4 my_team_color_hue_bright_half; //@ default_value="1 1 1 1"
    vec4 my_team_color_hue_dark; //@ default_value="1 1 1 1"
    vec4 my_team_color_hue_dark_half; //@ default_value="1 1 1 1"
    vec4 my_team_color_hue_complement; //@ default_value="1 1 1 1"
    vec4 my_alpha_team_color; //@ default_value="1 1 1 1"
    vec4 my_bravo_team_color; //@ default_value="1 1 1 1"
    vec4 my_charlie_team_color; //@ default_value="1 1 1 1"
    vec4 my_alpha_team_color_hue_bright; //@ default_value="1 1 1 1"
    vec4 my_bravo_team_color_hue_bright; //@ default_value="1 1 1 1"
    vec4 my_charlie_team_color_hue_bright; //@ default_value="1 1 1 1"
    vec4 my_alpha_team_color_hue_dark; //@ default_value="1 1 1 1"
    vec4 my_bravo_team_color_hue_dark; //@ default_value="1 1 1 1"
    vec4 my_charlie_team_color_hue_dark; //@ default_value="1 1 1 1"
    vec4 player_skin_color; //@ default_value="1 1 1 1"
    vec4 team_flag; //@ default_value="0 0 0 0"

    mat2x4 tex_mtx0; //@ default_value="1 -0 0 1 0 0 0 0"
    mat2x4 tex_mtx1; //@ default_value="1 -0 0 1 0 0 0 0"
    mat2x4 tex_mtx2; //@ default_value="1 -0 0 1 0 0 0 0"

    float tex_repetition_noise_division_num; //@ default_value="1E-44"
    float tex_repetition_noise_scale; //@ default_value="0.012"
    float tex_repetition_angle_deg; //@ default_value="0"
    float model_y_fog_up_start; //@ default_value="40"
    vec4 model_y_fog_up_end; //@ default_value="400 0 0 0"
    vec4 model_y_fog_up_color; //@ default_value="0 0 0 0"

    vec3 model_y_fog_up_dir; //@ default_value="0 1 0"
    float model_y_fog_down_start; //@ default_value="-40"
    vec4 model_y_fog_down_end; //@ default_value="-400 0 0 0"
    vec4 model_y_fog_down_color; //@ default_value="0 0 0 0"

    vec3 model_y_fog_down_dir; //@ default_value="0 1 0"
    float dynamic_vert_alpha_coeff; //@ default_value="1"

    float fade_dither_alpha; //@ default_value="1"
    float fade_dither_manual_alpha; //@ default_value="0"
    float camera_xlu_alphaX; //@ default_value="1"
    float camera_xlu_alphaY; //@ default_value="0"
    vec4 fade_player_pos; //@ default_value="0 0 0 1"

    float map_min_height; //@ default_value="0"
    float map_max_heightX; //@ default_value="0"
    float map_max_heightY; //@ default_value="0"
    float map_max_heightZ; //@ default_value="0"
    vec4 map_ink_gradation_rate; //@ default_value="1 1 1 1"
    vec4 map_min_gradation_color; //@ default_value="0 0 0 1"
    vec4 map_max_gradation_color; //@ default_value="1 1 1 1"
    vec4 map_ink_color_bright_offset; //@ default_value="0 0 0 0"

    float shell_fur_scale; //@ default_value="1"
    float actor_instance_idX; //@ default_value="0"
    float actor_instance_idY; //@ default_value="0"
    float actor_instance_idZ; //@ default_value="0"
    vec4 mantaking_parameter0; //@ default_value="0 0 1 0"
    vec4 is_use_graffiti_bake_paint_uv; //@ default_value="0 0 0 0"
    vec4 camera_xlu_moire_param0; //@ default_value="0 1 0.087 0.996"
    vec4 camera_xlu_moire_param1; //@ default_value="100 1 1.5 0"

    float bone_spehremap_transmission_thickness; //@ default_value="0.45"
    float bone_spehremap_aoX; //@ default_value="1"
    float bone_spehremap_aoY; //@ default_value="0"
    float bone_spehremap_aoZ; //@ default_value="0"
    vec4 instancing_skinning_param0; //@ default_value="0 0 0 0"
    vec4 instancing_skinning_param1; //@ default_value="0 0 0 0"
    vec4 output_clamp_value; //@ default_value="100 0 0 0"
    vec4 depth_silhouette_color; //@ default_value="1 1 1 1"

    float height_draw_mode; //@ default_value="0"
    float gsys_alpha_test_ref_value; //@ default_value="0.5"
    vec2 padding;
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