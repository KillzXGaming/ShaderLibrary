#version 450 core

precision mediump float;

//must match the mesh using this material
#define SKIN_COUNT 4

#define is_apply_irradiance_pixel true
#define enable_add_stain_proc_texture_3d false
#define enable_motion_vec false
#define enable_material_light true

//The uv transform method to use. 
//0 = none 1 = tex_mtx0, 2 = tex_mtx1, 3 = tex_mtx2, 4 = tex_mtx3
#define fuv0_mtx 0
#define fuv1_mtx 0
#define fuv2_mtx 0
#define fuv3_mtx 0

#define fuv0_selector 0
#define fuv1_selector 0
#define fuv2_selector 0
#define fuv3_selector 0

#define enable_fuv0 true
#define enable_fuv1 false
#define enable_fuv2 false
#define enable_fuv3 false

#define enable_blend_tangent false
#define o_normal         20

#define enable_displacement false
#define enable_displacement1 false
#define displacement_component 10
#define displacement1_component 10
#define displacement_fuv_selector 10
#define displacement1_fuv_selector 10
#define displacement_mul_vtx_color 0
#define displacement1_mul_vtx_color 0
#define displacement_mul_vtx_alpha 0
#define displacement1_mul_vtx_alpha 0

#define FUV_SELECT_UV0 10

#define base_color_fuv_selector   FUV_SELECT_UV0
#define normal_fuv_selector       FUV_SELECT_UV0
#define uniform0_fuv_selector     FUV_SELECT_UV0
#define uniform1_fuv_selector     FUV_SELECT_UV0
#define uniform2_fuv_selector     FUV_SELECT_UV0
#define uniform3_fuv_selector     FUV_SELECT_UV0
#define uniform4_fuv_selector     FUV_SELECT_UV0

layout (binding = 6) uniform samplerCube cTexCubeMapRoughness;
layout (binding = 9) uniform samplerCube cTextureMaterialLightCube;
layout (binding = 14) uniform sampler2D cTextureDisplacement0;
layout (binding = 16) uniform sampler2D cTextureDisplacement1;
layout (binding = 15) uniform sampler2D cDirectionalLightColor;

const int MAX_BONE_COUNT = 100;

layout (binding = 4, std140) uniform MdlMtx
{
    mat3x4 cBoneMatrices[MAX_BONE_COUNT];
};

layout (binding = 8, std140) uniform HDRTranslate 
{
    float Power;
    float Range;
}hdr;

layout (binding = 7, std140) uniform ModelAdditionalInfo 
{
    float model_alpha_mask;
    float normal_axis_x_scale;
    vec2 uv_offset;
    mat4 proj_mtx0;
    mat4 proj_mtx1;
    mat4 proj_mtx2;
    mat4 proj_mtx3;
    vec4 prog_constant0;
    vec4 prog_constant1;
}modelInfo;

layout (binding = 5, std140) uniform _Shp
{
    mat3x4 cTransform;
} shape;

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
    vec4 const_color0;
    vec4 const_color1;
    vec4 const_color2;
    vec4 const_color3;

    float const_single0;
    float const_single1;
    float const_single2;
    float const_single3;

    vec4 base_color_mul_color;
    vec4 uniform0_mul_color;
    vec4 uniform1_mul_color;
    vec4 uniform2_mul_color;
    vec4 uniform3_mul_color;
    vec4 uniform4_mul_color;
    vec4 proc_texture_2d_mul_color;
    vec4 proc_texture_3d_mul_color;

    mat2x4 tex_mtx0;
    mat2x4 tex_mtx1;
    mat2x4 tex_mtx2;
    mat2x4 tex_mtx3;

    float displacement_scale;
    float displacement1_scale;
    vec2 padding;

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
  //  vec4 flow0_param;
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
layout (location = 7) in vec4 vTexCoords3;
layout (location = 8) in vec4 vBitangent;
layout (location = 10) in vec4 vBoneWeight;
layout (location = 11) in ivec4 vBoneIndices;


layout (location = 0) out vec4 fNormalsDepth; //Normals xyz, depth
layout (location = 1) out vec4 fTangents; //Tangents xyz. bitagents x
layout (location = 2) out vec4 fBitangents; //Bitangents yz
layout (location = 3) out vec4 fViewPos; //Screen coords?, then view pos xy
layout (location = 4) out vec4 fLightColorVPosZ; //Light RGB, view pos z
layout (location = 5) out vec4 fTexCoords01; //Tex coords 0 -> 1 xy


layout (location = 6) out vec4 fVertexColor;
layout (location = 7) out vec4 fTexCoords23;
layout (location = 8) out vec4 fIrradianceVertex;
layout (location = 9) out vec2 fSphereCoords;
layout (location = 11) out vec4 fVertexPos; //todo loc 5 when used
layout (location = 12) out vec4 fPreviousPos; //todo loc 3 when used
layout (location = 13) out noperspective vec4 fPerspDiv;


vec4 skin(vec3 pos, ivec4 index)
{
    vec4 newPosition = vec4(pos.xyz, 1.0);

    if (SKIN_COUNT == 0)
        newPosition = vec4(pos, 1.0) * mat4(shape.cTransform);
    if (SKIN_COUNT == 1)
        newPosition = vec4(pos, 1.0) * mat4(cBoneMatrices[index.x]);

    if (SKIN_COUNT >  1)
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
    vec3 newNormal = nr;

    if (SKIN_COUNT == 0)
        newNormal =  nr * mat3(shape.cTransform);
    if (SKIN_COUNT == 1)
        newNormal =  nr * mat3(cBoneMatrices[index.x]);

    if (SKIN_COUNT >  1)
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

vec2 calc_texcoord_matrix(mat2x4 mtx, vec2 tex_coord)
{
    //actually a 2x3 matrix stored in 2x4
    vec3 r0 = vec3(mtx[0].xyz); 
    vec3 r1 = vec3(mtx[0].w, mtx[1].xy);

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
        default: return tex_coord;
    }
    return tex_coord;
}

vec2 get_tex_coord(int selector, int mtx_type, bool enable)
{
    if (!enable)
        return vTexCoords0.xy;

    switch (selector)
    {
        case 0: return get_tex_mtx(vTexCoords0.xy, mtx_type);
        case 1: return get_tex_mtx(vTexCoords1.xy, mtx_type);
        case 2: return get_tex_mtx(vTexCoords2.xy, mtx_type);
        case 3: return get_tex_mtx(vTexCoords3.xy, mtx_type);
        default: //unknown type
            return vTexCoords0.xy;
    }
}

vec4 DecodeCubemap(samplerCube cube, vec3 n, float lod)
{
    vec4 tex = textureLod(cube, n, lod);

    float scale = pow(tex.a, hdr.Power) * hdr.Range;
    return vec4(tex.rgb * scale, scale);
}

vec2 SelectDisplacementTexCoord(int mtx_select, vec3 position)
{
    if (mtx_select == 10)  //tex coord 0
        return fTexCoords01.xy;
    else if  (mtx_select == 11) //tex coord 1
        return fTexCoords01.zy;
    else if  (mtx_select == 12) //tex coord 2
        return fTexCoords23.xy;
    else if  (mtx_select == 13) //tex coord 3
        return fTexCoords23.zw;
    else if  (mtx_select == 14) //proj coordinates with y pos
    {
        vec2 proj_pos = (vec4(position, 1.0) * modelInfo.proj_mtx0).xy;
        return proj_pos; //todo confirm if this is right
    }

    return fTexCoords01.xy;
}

vec3 CalculateDisplacement0(vec3 position, vec3 normals)
{
    vec3 tex = textureLod(cTextureDisplacement0, (SelectDisplacementTexCoord(displacement_fuv_selector, position)), 0.0).xyz;

    vec3 displacement = tex.rgb * mat.displacement_scale * mat.displacement_color.rgb;
    if (displacement_component == 30) //By normals
         displacement = normals.xyz * tex.x * mat.displacement_scale * mat.displacement_color.rgb;

    if (displacement_mul_vtx_color == 1) //Multiply vertex colors
        displacement *= vColor.rgb;
    if (displacement_mul_vtx_alpha == 1) //Multiply vertex alpha
        displacement *= vColor.a;

    return position;
}

vec3 CalculateDisplacement1(vec3 position, vec3 normals)
{
    vec3 tex = textureLod(cTextureDisplacement1, (SelectDisplacementTexCoord(displacement1_fuv_selector, position)), 0.0).xyz;

    vec3 displacement = tex.rgb * mat.displacement1_scale * mat.displacement1_color.rgb;
    if (displacement1_component == 30) //By normals
         displacement = fNormalsDepth.xyz * tex.x * mat.displacement1_scale * mat.displacement1_color.rgb;

    if (displacement1_mul_vtx_color == 1) //Multiply vertex colors
        displacement *= fVertexColor.rgb;
    if (displacement1_mul_vtx_alpha == 1) //Multiply vertex alpha
        displacement *= fVertexColor.a;

    return position;
}

void main()
{   
    ivec4 bone_index = vBoneIndices;
    
    //position
    vec4 position = skin(vPosition.xyz, bone_index);

    //normals
    fNormalsDepth = vec4(skinNormal(vNormal.xyz, bone_index).xyz, 1.0);

    if (enable_displacement)
        position.xyz = CalculateDisplacement0(position.xyz, fNormalsDepth.xyz);
    if (enable_displacement1)
        position.xyz = CalculateDisplacement1(position.xyz, fNormalsDepth.xyz);

    gl_Position = vec4(position.xyz, 1.0) * mdlEnvView.cViewProj;

    float linear_depth = (gl_Position.w - mdlEnvView.ZNearFar.x) * mdlEnvView.ZNearFar.w;
    fNormalsDepth.w = linear_depth;

    //tangents
    if (enable_blend_tangent)
    {
        vec3 tangents = skinNormal(vTangent.xyz, bone_index);
        fTangents.xyz = tangents.xyz * vTangent.w;

        vec4 bi_tangent = vec4(skinNormal(vBitangent.xyz, bone_index).xyz, 1.0);

        fTangents.w   = bi_tangent.x   * vBitangent.w;
        fBitangents.xy = bi_tangent.yz * vBitangent.w;
    }
    else if (o_normal != 30) //if normal output is not just vertex normals
    {
        vec3 tangents = skinNormal(vTangent.xyz, bone_index);
        fTangents.xyz = tangents.xyz * vTangent.w;

        //Compute bitangent output
        vec3 B = normalize(cross(fNormalsDepth.xyz, tangents.xyz) * vTangent.w);

        fTangents.w   = B.x;
        fBitangents.x = B.y;
        fBitangents.y = B.z;
    }

    //material tex coords
    fTexCoords01.xy  = get_tex_coord(fuv0_selector, fuv0_mtx, enable_fuv0); 
    fTexCoords01.zw  = get_tex_coord(fuv1_selector, fuv1_mtx, enable_fuv1); 
    fTexCoords23.xy  = get_tex_coord(fuv2_selector, fuv2_mtx, enable_fuv2); 
    fTexCoords23.zw  = get_tex_coord(fuv3_selector, fuv3_mtx, enable_fuv3); 

    fVertexColor = vColor;

    vec4 view_pos = vec4(position.xyz, 1.0) * mat4(mdlEnvView.cView);

    fViewPos.zw = view_pos.xy;
    fLightColorVPosZ.w = view_pos.z;

    vec3 light_color = textureLod(cDirectionalLightColor, vec2(mdlEnvView.Dir.w, 0.5), 0.0).xyz;
    fLightColorVPosZ.xyz = light_color;

    if (!is_apply_irradiance_pixel)
    {
        if (enable_material_light) //use material light cubemap
        {
            const float MAX_LOD = 5.0;
            vec4 irradiance_cubemap = DecodeCubemap(cTextureMaterialLightCube, fNormalsDepth.xyz, MAX_LOD);
            fIrradianceVertex = irradiance_cubemap *= mdlEnvView.Exposure.y;
        }
        else //use material roughness cubemap
        {
            const float MAX_LOD = 5.0;
            vec4 irradiance_cubemap = DecodeCubemap(cTexCubeMapRoughness, fNormalsDepth.xyz, MAX_LOD);
            fIrradianceVertex.rgba = irradiance_cubemap.rgba * mdlEnvView.Exposure.y;
        }
    }

    if (enable_add_stain_proc_texture_3d)
    {
        fVertexPos.xyz = vPosition.xyz;
    }

    if (enable_motion_vec)
    {
        vec3 invProjPos = (vec4(position.xyz, 1.0) * mat4(mdlEnvView.cViewProjInv)).xyz;
        fBitangents.z = invProjPos.x;
        fBitangents.y = invProjPos.y;
        fPreviousPos.w = invProjPos.z;

        //vPosition * previous mtx pos * previous view proj TODO
        fPreviousPos.x = 0;
        fPreviousPos.y = 0;
        fPreviousPos.z = 0;
    }

    //Check if any proj textures are used
    if (uniform0_fuv_selector == 50 || uniform1_fuv_selector == 50 || uniform2_fuv_selector == 50 ||
        uniform3_fuv_selector == 50 || base_color_fuv_selector == 50 || normal_fuv_selector == 50||
        uniform4_fuv_selector == 50)
    {
        //TODO mtx0 -> mtx2 (sets 3 attributes)
        vec2 proj_pos0 = (vec4(position.xyz, 1.0) * modelInfo.proj_mtx0).xy;
        vec2 proj_pos1 = (vec4(position.xyz, 1.0) * modelInfo.proj_mtx1).xy;
        vec2 proj_pos2 = (vec4(position.xyz, 1.0) * modelInfo.proj_mtx2).xy;
        vec2 proj_pos3 = (vec4(position.xyz, 1.0) * modelInfo.proj_mtx3).xy;

    }

    //Sphere mapping 
	vec3 view_n = (normalize(fNormalsDepth.xyz) * mat3(mdlEnvView.cView)).xyz;
	//center the uvs
	fSphereCoords.xy = view_n.xy * vec2(0.5) + vec2(0.5,0.5);

    fPerspDiv.xy =  gl_Position.xy / gl_Position.w; //the game seems to center/shift this in frag shader

    return;
}
