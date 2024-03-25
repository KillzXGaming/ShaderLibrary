#version 450 core

//must match the mesh using this material
#define SKIN_COUNT 4

//The uv transform method to use. 
//0 = none 1 = tex_mtx0, 2 = tex_mtx1, 3 = tex_mtx2, 4 = tex_mtx3
#define FUV0_MTX 0
#define FUV1_MTX 0
#define FUV2_MTX 0
#define FUV3_MTX 0

//The UV layer to use
#define FUV0_SELECTOR 0
#define FUV1_SELECTOR 0
#define FUV2_SELECTOR 0
#define FUV3_SELECTOR 0

#define ENABLE_FUV0 true
#define ENABLE_FUV1 false
#define ENABLE_FUV2 false
#define ENABLE_FUV3 false

layout (binding = 15) uniform sampler2D cDirectionalLightColor;

const int MAX_BONE_COUNT = 100;

layout (binding = 4, std140) uniform MdlMtx
{
    mat3x4 cBoneMatrices[MAX_BONE_COUNT];
};

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
}mat;

layout (location = 0) in vec4 vPosition;
layout (location = 1) in vec4 vNormal;
layout (location = 2) in vec4 vColor;
layout (location = 3) in vec4 vTangent;
layout (location = 4) in vec4 vTexCoords0;
layout (location = 5) in vec4 vTexCoords1;
layout (location = 6) in vec4 vTexCoords2;
layout (location = 8) in vec4 vBitangent;
layout (location = 10) in vec4 vBoneWeight;
layout (location = 11) in ivec4 vBoneIndices;


layout (location = 0) out vec4 fNormalsDepth;
layout (location = 1) out vec4 fTexCoords01;
layout (location = 2) out vec4 fTangents;
layout (location = 3) out vec4 fTexCoords23;
layout (location = 4) out vec4 fLightColor;
layout (location = 5) out vec4 fViewDirection;

vec4 skin(vec3 pos, ivec4 index)
{
    vec4 newPosition = vec4(pos.xyz, 1.0);
    
    if (SKIN_COUNT >= 1)
        newPosition =  vec4(pos, 1.0) * mat4(cBoneMatrices[index.x]) * vBoneWeight.x;
    if (SKIN_COUNT >= 2)
        newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[index.y]) * vBoneWeight.y;
    if (SKIN_COUNT >= 3)
        newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[index.z]) * vBoneWeight.z;
    if (SKIN_COUNT >= 4)
        newPosition += vec4(pos, 1.0) * mat4(cBoneMatrices[index.w]) * vBoneWeight.w;
        
    return newPosition;
}

vec3 skinNormal(vec3 nr, ivec4 index)
{
    vec3 newNormal = vec3(0);

    if (SKIN_COUNT >= 1)
        newNormal =  nr * mat3(cBoneMatrices[index.x]) * vBoneWeight.x;
    if (SKIN_COUNT >= 2)
        newNormal += nr *  mat3(cBoneMatrices[index.y]) * vBoneWeight.y;
    if (SKIN_COUNT >= 3)
        newNormal += nr * mat3(cBoneMatrices[index.z]) * vBoneWeight.z;
    if (SKIN_COUNT >= 4)
        newNormal += nr * mat3(cBoneMatrices[index.w]) * vBoneWeight.w;
    
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

vec2 get_tex_mtx(vec2 tex_coord, int type)
{
    if (type == 0) return tex_coord; //no matrix method used

    switch (type)
    {
        case 1: return calc_texcoord_matrix(mat.tex_mtx0, tex_coord);
        case 2: return calc_texcoord_matrix(mat.tex_mtx1, tex_coord);
        case 3: return calc_texcoord_matrix(mat.tex_mtx2, tex_coord);
        case 4: return calc_texcoord_matrix(mat.tex_mtx3, tex_coord);
        //TODO 5+ may include matcap and projection types
        default: return tex_coord;
    }
    return tex_coord;
}

vec2 get_tex_coord(int selector, int mtx_type, bool enable)
{
    if (!enable)
        return vec2(0.0);

    switch (selector)
    {
        case 0: get_tex_mtx(vTexCoords0.xy, mtx_type);
        case 1: get_tex_mtx(vTexCoords1.xy, mtx_type);
        case 2: get_tex_mtx(vTexCoords2.xy, mtx_type);
        default: //unknown type
        return get_tex_mtx(vTexCoords0.xy, mtx_type);
    }
}
 
void main()
{   
    ivec4 bone_index = vBoneIndices;
    
    //position
    vec4 position = skin(vPosition.xyz, bone_index);
    gl_Position = vec4(position.xyz, 1) * mdlEnvView.cViewProj;

    //normals
    fNormalsDepth = vec4(skinNormal(vNormal.xyz, bone_index).xyz, 1.0);

    float linear_depth = (gl_Position.w - mdlEnvView.ZNearFar.x) * mdlEnvView.ZNearFar.w;
    fNormalsDepth.w = linear_depth;

    //tangents
    fTangents.xyz = skinNormal(vTangent.xyz, bone_index);
    fTangents.w = vTangent.w;

    //material tex coords
    fTexCoords01.xy  = get_tex_coord(FUV0_SELECTOR, FUV0_MTX, ENABLE_FUV0); 
    fTexCoords01.zw  = get_tex_coord(FUV0_SELECTOR, FUV1_MTX, ENABLE_FUV1); 
    fTexCoords23.xy  = get_tex_coord(FUV0_SELECTOR, FUV2_MTX, ENABLE_FUV2); 
    fTexCoords23.zw  = get_tex_coord(FUV0_SELECTOR, FUV3_MTX, ENABLE_FUV3); 

    vec3 light_color = textureLod(cDirectionalLightColor, vec2(mdlEnvView.Dir.w, 0.5), 0.0).xyz;
    fLightColor.xyz = light_color;


    //world pos - camera pos for eye position
    fViewDirection.xyz = normalize(vec3(
       mdlEnvView.cView[0].w,
       mdlEnvView.cView[1].w, 
       mdlEnvView.cView[2].w) - position.xyz);
    return;
}
