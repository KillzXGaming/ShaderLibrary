#version 450 core


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

layout (binding = 0) uniform sampler2D cTextureBaseColor; //1
layout (binding = 1) uniform sampler2D cTextureNormal; //2
layout (binding = 2) uniform sampler2D cTextureUniform0; //4
layout (binding = 3) uniform sampler2D cTextureUniform1;
layout (binding = 4) uniform sampler2D cTextureUniform2;
layout (binding = 5) uniform sampler2D cTextureUniform3;
layout (binding = 6) uniform sampler2D cTextureUniform4;

layout (binding = 9) uniform samplerCube cTextureMaterialLightCube;
layout (binding = 22) uniform sampler2D cTextureMaterialLightSphere;
layout (binding = 23) uniform sampler2D cExposureTexture;



layout (location = 0) in vec4 fNormalsDepth;
layout (location = 1) in vec4 fTexCoords01;
layout (location = 2) in vec4 fTangents;
layout (location = 3) in vec4 fTexCoords23;
layout (location = 4) in vec4 fLightColor;
layout (location = 5) in vec4 fViewDirection;
layout (location = 6) in vec4 fVertexColor;

layout (location = 0) out vec4 oLightBuf;
layout (location = 1) out vec4 oWorldNrm;
layout (location = 2) out vec4 oNormalizedLinearDepth;
layout (location = 3) out vec4 oBaseColor;

//Selectors for what UV mtx config to use for each sampler
const int FUV_MTX0 = 10;
const int FUV_MTX1 = 11;
const int FUV_MTX2 = 12;
const int FUV_MTX3 = 13;

#define enable_base_color true
#define enable_base_color_mul_color false
#define enable_normal true
#define enable_ao true
#define enable_emission true
#define enable_sss true
#define enable_alpha_mask false

#define is_apply_irradiance_pixel true

#define alpha_test_func 60 //GEQUAL

#define emission_type 1
#define emission_scale_type 7

#define vtxcolor_type 0

#define o_base_color     10
#define o_normal         20
#define o_roughness      50
#define o_metalness      51
#define o_sss            52
#define o_emission       50
#define o_ao 50
#define o_transparent_tex 50
#define o_alpha 116

#define roughness_component 30 //red
#define metalness_component 30 //red
#define emission_component 10 //rgba
#define alpha_component 60 //alpha

#define base_color_uv_selector   FUV_MTX0
#define normal_uv_selector       FUV_MTX0
#define uniform0_uv_selector     FUV_MTX0
#define uniform1_uv_selector     FUV_MTX0
#define uniform2_uv_selector     FUV_MTX0
#define uniform3_uv_selector     FUV_MTX0
#define uniform4_uv_selector     FUV_MTX0

#define enable_uniform0     true
#define enable_uniform1     true
#define enable_uniform2     true
#define enable_uniform3     true
#define enable_uniform4     true

#define enable_uniform0_mul_vtxcolor     false
#define enable_uniform1_mul_vtxcolor     false
#define enable_uniform2_mul_vtxcolor     false
#define enable_uniform3_mul_vtxcolor     false
#define enable_uniform4_mul_vtxcolor     false

#define enable_uniform0_mul_color     false
#define enable_uniform1_mul_color     false
#define enable_uniform2_mul_color     false
#define enable_uniform3_mul_color     false
#define enable_uniform4_mul_color     false

#define enable_uniform0_roughness_lod false
#define enable_uniform1_roughness_lod false
#define enable_uniform2_roughness_lod false
#define enable_uniform3_roughness_lod false
#define enable_uniform4_roughness_lod false
#define enable_uniform5_roughness_lod false

#define blend0_src           10
#define blend1_src           10
#define blend2_src           10
#define blend3_src           10
#define blend4_src           10
#define blend5_src           10

#define blend0_src_ch        10
#define blend1_src_ch        10
#define blend2_src_ch        10
#define blend3_src_ch        10
#define blend4_src_ch        10
#define blend5_src_ch        10

#define blend0_dst           50
#define blend1_dst           50
#define blend2_dst           50
#define blend3_dst           50
#define blend4_dst           50
#define blend5_dst           50

#define blend0_dst_ch        10
#define blend1_dst_ch        10
#define blend2_dst_ch        10
#define blend3_dst_ch        10
#define blend4_dst_ch        10
#define blend5_dst_ch        10

#define blend0_cof           61
#define blend1_cof           61
#define blend2_cof           61
#define blend3_cof           61
#define blend4_cof           61
#define blend5_cof           61

#define blend0_cof_ch        50
#define blend1_cof_ch        50
#define blend2_cof_ch        50
#define blend3_cof_ch        50
#define blend4_cof_ch        50
#define blend5_cof_ch        50

#define blend0_cof_map       50
#define blend1_cof_map       50
#define blend2_cof_map       50
#define blend3_cof_map       50
#define blend4_cof_map       50
#define blend5_cof_map       50

#define blend0_indirect_map  50
#define blend1_indirect_map  50
#define blend2_indirect_map  50
#define blend3_indirect_map  50
#define blend4_indirect_map  50
#define blend5_indirect_map  50

#define blend0_eq            0
#define blend1_eq            0
#define blend2_eq            0
#define blend3_eq            0
#define blend4_eq            0
#define blend5_eq            0

#define enable_blend0        true
#define enable_blend1        false
#define enable_blend2        false
#define enable_blend3        false
#define enable_blend4        false
#define enable_blend5        false

#define SPHERE_CONST_COLOR0 2
#define SPHERE_CONST_COLOR1 0
#define SPHERE_CONST_COLOR2 0
#define SPHERE_CONST_COLOR3 0

//Variables for setting blend for outputs later
vec4 BLEND0_OUTPUT;
vec4 BLEND1_OUTPUT;
vec4 BLEND2_OUTPUT;
vec4 BLEND3_OUTPUT;
vec4 BLEND4_OUTPUT;
vec4 BLEND5_OUTPUT;

vec4 EncodeBaseColor(vec3 baseColor, float roughness, float metalness, vec3 normal)
{
    return vec4(baseColor, roughness);
}

vec4 DecodeCubemap(samplerCube cube, vec3 n, float lod)
{
    vec4 tex = textureLod(cube, n, lod);

    float scale = pow(tex.a, hdr.Power) * hdr.Range;
    return vec4(tex.rgb * scale, scale);
}

vec2 CalcSphereCoords(vec3 n)
{
    //view normal
    vec3 view_n = (normalize(n.xyz) * mat3(mdlEnvView.cView)).xyz;
    //center the uvs
    return view_n.xy * vec2(0.5) + vec2(0.5,-0.5);
}

vec4 GetComp(vec4 v, int comp_mask)
{
    switch (comp_mask)
    {
        case 10: return v.rgba;
        case 30: return v.rrrr;
        case 40: return v.gggg;
        case 50: return v.bbbb;
        case 60: return v.aaaa;
    }
    return v.rgba;
}

vec2 SelectTexCoord(int mtx_select)
{
    if (mtx_select == 0)
        return fTexCoords01.xy;
    else if  (mtx_select == 1)
        return fTexCoords01.zy;
    else if  (mtx_select == 2)
        return fTexCoords23.xy;
    else if  (mtx_select == 3)
        return fTexCoords23.zw;
    else
        return fTexCoords01.xy;
}

vec4 CalculateSphereConstColor(int sphere_color_type, vec4 const_color, float sphere_rate_color)
{
    float sphere_p = 1.0; //1.0 
    if (sphere_color_type > 0)
    {
        vec3 normal = fNormalsDepth.xyz;
        vec3 dir = fViewDirection.xyz;

        sphere_p = clamp(
            fma(normal.z, -dir.z * mdlEnvView.cView[2].z + dir.x * mdlEnvView.cView[2].x + dir.y * mdlEnvView.cView[2].y, 
            fma(normal.x, -dir.z * mdlEnvView.cView[0].z + dir.x * mdlEnvView.cView[0].x + dir.y * mdlEnvView.cView[0].y, 
                normal.y *-dir.z * mdlEnvView.cView[1].z + dir.x * mdlEnvView.cView[1].x + dir.y * mdlEnvView.cView[1].y)), 
                0.0, 1.0);
    }
    if (sphere_color_type == 1)
    {
        float amount = clamp(exp2(log2(sphere_p) * sphere_rate_color), 0.0, 1.0);
        return const_color * amount;
    }
    else if (sphere_color_type == 2)
    {
        float amount = clamp(exp2(log2(0.0 - sphere_p + 1.0) * sphere_rate_color), 0.0, 1.0);
        return const_color * amount;
    }
    else
        return const_color; //type 0 defaults to const color
}

vec4 CalculateUniform(sampler2D cTexture, int uv_selector, bool enable, vec4 mul_color,
    bool enable_mul_color, bool enable_mul_vtx_color, bool enable_roughness_lod)
{
    vec4 uniform_output = vec4(1.0);
    if (enable) //Todo third argument uses MdlEnvView.data[0x12A].x, a global LOD value
         uniform_output = texture(cTexture, SelectTexCoord(uv_selector));
    if (enable_roughness_lod)
         uniform_output = textureLod(cTexture, SelectTexCoord(uv_selector), 0.0);
    if (enable_mul_color)
        uniform_output *= mul_color;
    if (enable_mul_vtx_color)
        uniform_output *= fVertexColor;

    return uniform_output;
}

vec4 CalculateBaseColor()
{
    vec4 basecolor_output = vec4(1.0);
    if (enable_base_color) //Todo third argument uses MdlEnvView.data[0x12A].x, a global LOD value
         basecolor_output = texture(cTextureBaseColor, SelectTexCoord(base_color_uv_selector));
    if (enable_base_color_mul_color)
        basecolor_output *= mat.base_color_mul_color;
    if (vtxcolor_type == 0)
        basecolor_output.rgb *= fVertexColor.rgb;
    else if (vtxcolor_type == 3)
    {
        basecolor_output.rgb *= basecolor_output.rgb +
            (basecolor_output.rgb *-fVertexColor.rgb + fVertexColor.rgb);
    }

    return basecolor_output;
}

#define CALCULATE_UNIFORM(num) \
    CalculateUniform(cTextureUniform##num, \
        uniform##num##_uv_selector, \
        enable_uniform##num, \
        mat.uniform##num##_mul_color,  \
        enable_uniform##num##_mul_color, \
        enable_uniform##num##_mul_vtxcolor, \
        enable_uniform##num##_roughness_lod) \

#define CALCULATE_CONST_COLOR(num) \
    CalculateSphereConstColor(SPHERE_CONST_COLOR##num, \
                            mat.const_color##num, \
                            mat.sphere_rate_color##num) \

vec4 CalculateOutput(int flag)
{
    if (flag == 10) CalculateBaseColor();
    else if (flag == 15) return fVertexColor;
    else if (flag == 20) return texture(cTextureNormal, SelectTexCoord(normal_uv_selector));
    else if (flag == 30) return vec4(fNormalsDepth.rgb, 0.0); //used in normals when no normal map present
    else if (flag == 50) return CALCULATE_UNIFORM(0);
    else if (flag == 51) return CALCULATE_UNIFORM(1);
    else if (flag == 52) return CALCULATE_UNIFORM(2);
    else if (flag == 53) return CALCULATE_UNIFORM(2);
    else if (flag == 54) return CALCULATE_UNIFORM(3);
    else if (flag == 60) return CALCULATE_CONST_COLOR(0); //sphere rate 0
    else if (flag == 61) return CALCULATE_CONST_COLOR(1); //sphere rate 1
    else if (flag == 62) return CALCULATE_CONST_COLOR(2); //sphere rate 2
    else if (flag == 63) return CALCULATE_CONST_COLOR(3); //sphere rate 3
    else if (flag == 70) return vec4(0.0); //cFrameBufferTex TODO
    else if (flag == 71) return vec4(0.0); //cGBufferBaseColorTex TODO
    else if (flag == 72) return vec4(0.0); //cGBufferNormalTex TODO
    else if (flag == 73) return vec4(0.0); //gbuffer decode from base color TODO
    else if (flag == 74) return vec4(0.0); //gbuffer decode from base color TODO
    else if (flag == 78) return vec4(0.0); //linear depth TODO

    else if (flag == 80) return BLEND0_OUTPUT; //blend 0
    else if (flag == 81) return BLEND1_OUTPUT; //blend 1
    else if (flag == 82) return BLEND2_OUTPUT; //blend 2
    else if (flag == 83) return BLEND3_OUTPUT; //blend 3
    else if (flag == 84) return BLEND4_OUTPUT; //blend 4
    else if (flag == 85) return BLEND5_OUTPUT; //blend 5

    else if (flag == 110) return vec4(mat.const_single0); //Mat.const_single0
    else if (flag == 111) return vec4(mat.const_single1); //Mat.const_single1
    else if (flag == 112) return vec4(mat.const_single2); //Mat.const_single2
    else if (flag == 113) return vec4(mat.const_single3); //Mat.const_single3
    else if (flag == 115) return vec4(0.0); //constant
    else if (flag == 116) return vec4(1.0); //constant
    else if (flag == 140) return vec4(0.0); //ModelAdditionalInfo.uv_offset.z TODO
    else if (flag == 160) return vec4(0.0); //proc texture 2d TODO
    else if (flag == 170) return vec4(0.0); //proc texture 3d TODO

    return vec4(0.0);
}

float BlendCompareComponent(float src, float dst, float cof)
{
    float cmp = (src.r < 0.5) ? 0.0 : 1.0;
    float n = -2.0 * src.x + 2.0;
    float func = 2.0 * src.x * dst.x * cof.x + 2.0 * src.x - src.x * cof.x;
    return func + cmp * (-func - dst.x * cof.x * (-n) + cmp);
}

vec4 CalculateBlend(bool enable, int src_id, int dst_id, int cof_id,
       int src_ch, int dst_ch, int cof_ch, int ind, int equation)
{
    if (!enable)
        return vec4(0.0);

    vec4 src = GetComp(CalculateOutput(src_id), src_ch);
    vec4 dst = GetComp(CalculateOutput(dst_id), dst_ch);
    vec4 cof = GetComp(CalculateOutput(cof_id), cof_ch);

    if      (equation == 0) return fma(src - dst, cof, dst);
    else if (equation == 1) return fma(dst, cof, src);
    else if (equation == 2) return dst * cof * src;
    else if (equation == 3) return fma(dst, 0.0 - cof, src); 
    else if (equation == 4) return dst + cof + src; 
    else if (equation == 5) return fma(dst * cof, 0.0 - src, dst * cof) + src;
    else if (equation == 6) //Compare func 
    {
        src.x = BlendCompareComponent(src.x, dst.x, cof.x);
        src.y = BlendCompareComponent(src.y, dst.y, cof.y);
        src.z = BlendCompareComponent(src.z, dst.z, cof.z);
        src.w = BlendCompareComponent(src.w, dst.w, cof.w);
        return src;
    }
    else if (equation == 7) return (src + dst) * cof; 
    else if (equation == 8) return (src - dst) * cof; 

    return src;
}

#define CALCULATE_BLEND(num) \
    CalculateBlend(enable_blend##num,\
           blend##num##_src,\
           blend##num##_dst, \
           blend##num##_cof, \
           blend##num##_src_ch, \
           blend##num##_dst_ch, \
           blend##num##_cof_ch, \
           blend##num##_indirect_map, \
           blend##num##_eq) \

//Updates any blend variables referenced by flag


void TryCalculateReferencedBlend(int flag)
{
    if (     flag == 80) BLEND0_OUTPUT = CALCULATE_BLEND(0); //blend 0
    else if (flag == 81) BLEND1_OUTPUT = CALCULATE_BLEND(1); //blend 1
    else if (flag == 82) BLEND2_OUTPUT = CALCULATE_BLEND(2); //blend 2
    else if (flag == 83) BLEND3_OUTPUT = CALCULATE_BLEND(3); //blend 3
    else if (flag == 84) BLEND4_OUTPUT = CALCULATE_BLEND(4); //blend 4
    else if (flag == 85) BLEND5_OUTPUT = CALCULATE_BLEND(5); //blend 5}
}

#define CHECK_CALC_BLEND_REFS(num) \
    TryCalculateReferencedBlend(blend##num##_src);\
    TryCalculateReferencedBlend(blend##num##_dst);\
    TryCalculateReferencedBlend(blend##num##_cof);\

const float PI = 3.14159265359;

vec3 saturate(vec3 v)
{
    return clamp(v, 0.0, 1.0);
}

float saturate(float v)
{
    return clamp(v, 0.0, 1.0);
}

// Shader code adapted from learnopengl.com's PBR tutorial:
// https://learnopengl.com/PBR/Theory

vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

vec3 FresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(1.0 - cosTheta, 5.0);
}

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a      = roughness*roughness;
    float a2     = a*a;
    float NdotH  = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;

    float num   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return num / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float num   = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return num / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2  = GeometrySchlickGGX(NdotV, roughness);
    float ggx1  = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

vec3 BRDF_DFG_Polynomial(vec3 L, vec3 V, vec3 N, vec3 F0, float roughness) {
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    if (NdotV > 0.0 && NdotL > 0.0) {
        vec3 H = normalize(L + V);
        float D = DistributionGGX(N, H, roughness);
        float G = GeometrySchlickGGX(NdotV, roughness) * GeometrySchlickGGX(NdotL, roughness);
        vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

        return (D * G) / (4.0 * NdotL * NdotV) * F;
    }
    return vec3(0.0);
}

vec3 ReconstructNormal(in vec2 t_NormalXY) {
    float t_NormalZ = sqrt(clamp(1.0 - dot(t_NormalXY.xy, t_NormalXY.xy), 0.0, 1.0));
    return vec3(t_NormalXY.xy, t_NormalZ);
}

vec3 CalculateNormals(vec2 normals, vec2 normal_map)
{
    vec3 N = vec3(normals, 1);
    vec3 T = vec3(fTangents.xyz);
    vec3 B = normalize(cross(N, T) * fTangents.w);

    mat3 tbn_matrix = mat3(T, B, N);

    vec3 tangent_normal = N;
    if (enable_normal)
    {
        tangent_normal = ReconstructNormal(normal_map);
    }
    return normalize(tbn_matrix * tangent_normal).xyz;
}

vec3 CalculateEmission(vec4 sphere_light_map)
{
    vec3 emission = vec3(0.0);

    if (enable_emission)
    {
        emission = GetComp(CalculateOutput(o_emission), emission_component).rgb;

        if (vtxcolor_type == 2)
            emission *= fVertexColor.rgb;

        //Emission scale
        if (emission_scale_type == 1) //material light sphere
            emission = max(sphere_light_map.rgb * emission.rgb, emission.rgb);
        if (emission_scale_type == 2) //material light sphere max 1.0
            emission = emission * max(sphere_light_map.rgb, 1.0);
        if (emission_scale_type == 4) //emission * material light sphere color
            emission = emission * sphere_light_map.rgb;
        if (emission_scale_type == 4) //emission * material light sphere scale
            emission = emission * sphere_light_map.w;
        if (emission_scale_type == 5) //max sphere light amount
        {
            float max_scale = max(max(sphere_light_map.g, sphere_light_map.b), sphere_light_map.r);
            emission = max_scale * emission;
        }
        if (emission_scale_type == 6) //max sphere light amount limited by emission amount
        {
            float max_scale = max(max(sphere_light_map.g, sphere_light_map.b), sphere_light_map.r);
            emission = max(vec3(max_scale) * emission, emission);
        }
        if (emission_scale_type == 7)
        {   
            //exposure scale    
            float exposure = texture(cExposureTexture, vec2(0.0, 0.0)).w;
            emission *= 1.0 / exposure * mdlEnvView.cProjInvNoPos[2].x;
        }

    }
    return emission;
}

void SetupBlend()
{
    //Calc blending. Compute any references first
    CHECK_CALC_BLEND_REFS(0);
    CHECK_CALC_BLEND_REFS(1);
    CHECK_CALC_BLEND_REFS(2);
    CHECK_CALC_BLEND_REFS(3);
    CHECK_CALC_BLEND_REFS(4);
    CHECK_CALC_BLEND_REFS(5);

    BLEND0_OUTPUT = CALCULATE_BLEND(0);
    BLEND1_OUTPUT = CALCULATE_BLEND(1);
    BLEND2_OUTPUT = CALCULATE_BLEND(2);
    BLEND3_OUTPUT = CALCULATE_BLEND(3);
    BLEND4_OUTPUT = CALCULATE_BLEND(4);
    BLEND5_OUTPUT = CALCULATE_BLEND(5);
}

void main()
{
    SetupBlend();

    vec4 base_color           = CalculateOutput(o_base_color);
    vec2 normal_map           = CalculateOutput(o_normal).rg;
    float metalness   = GetComp(CalculateOutput(o_metalness), metalness_component).r;
    float roughness   = GetComp(CalculateOutput(o_roughness), roughness_component).r;
    vec4 sss                  = CalculateOutput(o_sss);
    vec4 ao                   = CalculateOutput(o_ao);
    vec4 transparent_tex     = CalculateOutput(o_transparent_tex);
    float alpha      = GetComp(CalculateOutput(o_alpha), alpha_component).r;

    //Roughness adjust
    roughness *= mat.force_roughness;
    roughness = saturate(roughness);

    //Normals
    vec3 N = CalculateNormals(fNormalsDepth.rg, normal_map);

    //Sphere mapping
    vec4 sphere_map = textureLod(cTextureMaterialLightSphere, CalcSphereCoords(N.xyz), sqrt(roughness)).rgba;

    //PBR
    vec3 I = vec3(0,0,-1) * mat3(mdlEnvView.cView);
    vec3 V = normalize(I); // view
    vec3 L = normalize(mdlEnvView.Dir.xyz ); // Light
    vec3 H = normalize(V + L); // half angle
    vec3 R = reflect(I, N); // reflection
    float NL = saturate(dot(N, L));

    vec3 f0 = mix(vec3(0.04), base_color.rgb, metalness); // dialectric
    vec3 kS = FresnelSchlickRoughness(max(dot(N, H), 0.0), f0, roughness);
    vec3 brdf = BRDF_DFG_Polynomial(L, V, N, f0, roughness);

    const float MAX_LOD = 5.0;

    vec4 irradiance_cubemap = DecodeCubemap(cTextureMaterialLightCube, N, roughness * MAX_LOD);
    irradiance_cubemap.rgb *= mdlEnvView.Exposure.y;

    vec3 specularTerm = irradiance_cubemap.rgb * 0.3 * kS;

    //Diffuse
    vec3 diffuseTerm = base_color.rgb;

    //Adjust for metalness.
    diffuseTerm *= clamp(1.0 - metalness, 0.0, 1.0);
    diffuseTerm *= vec3(1) - brdf;

    //Ambient occ
    if (enable_ao)
        diffuseTerm.rgb *= ao.rgb;

    if (enable_alpha_mask)
    {
        if (alpha_test_func == 60)
        {
            if (alpha < mat.alpha_test_value)
                discard;
        }
    }

    //Light output
    oLightBuf.rgb = diffuseTerm.rgb * fLightColor.xyz + specularTerm;

    //Emission
    if (enable_emission)
        oLightBuf.rgb += CalculateEmission(sphere_map).rgb;

    //clamp 0 - 2048 due to HDR/tone mapping
    oLightBuf.rgb = max(oLightBuf.rgb, 0.0);
    oLightBuf.rgb = min(oLightBuf.rgb, 2048.0);

    //Normals output
    oWorldNrm.rg = N.rg * 0.5 + 0.5;

    oNormalizedLinearDepth.r = 0.0; //todo. This causes flickering due to depth not matching with depth shader
 //   oNormalizedLinearDepth.r = fNormalsDepth.w;

    oBaseColor = EncodeBaseColor(saturate(base_color.rgb), roughness, metalness, N);
    return;
}