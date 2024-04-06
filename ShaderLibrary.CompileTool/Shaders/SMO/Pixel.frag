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

#define ENABLE_ALPHA_MASK false
#define ENABLE_NORMAL_MAP true

#define o_base_color     10
#define o_normal         20
#define o_roughness      50
#define o_metalness      51
#define o_sss            52
#define o_emission       50

//Selectors for what UV mtx config to use for each sampler
const int FUV_MTX0 = 10;
const int FUV_MTX1 = 11;
const int FUV_MTX2 = 12;
const int FUV_MTX3 = 13;

#define base_color_uv_selector   FUV_MTX0
#define normal_uv_selector       FUV_MTX0
#define uniform0_uv_selector     FUV_MTX0
#define uniform1_uv_selector     FUV_MTX0
#define uniform2_uv_selector     FUV_MTX0
#define uniform3_uv_selector     FUV_MTX0
#define uniform4_uv_selector     FUV_MTX0

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

#define blend0_src           10
#define blend0_src_ch        10

#define blend0_dst           50
#define blend0_dst_ch        10

#define blend0_cof           61
#define blend0_cof_ch        50
#define blend0_cof_map       50

#define blend0_indirect_map  50

#define blend0_eq            0

#define enable_blend0        true

#define SPHERE_CONST_COLOR0 2
#define SPHERE_CONST_COLOR1 0
#define SPHERE_CONST_COLOR2 0
#define SPHERE_CONST_COLOR3 0

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

float BlendCompareComponent(float src, float dst, float cof)
{
    float cmp = (src.r < 0.5) ? 0.0 : 1.0;
    float n = -2.0 * src.x + 2.0;
    float func = 2.0 * src.x * dst.x * cof.x + 2.0 * src.x - src.x * cof.x;
    return func + cmp * (-func - dst.x * cof.x * (-n) + cmp);
}

vec4 CalculateBlend(vec4 src, vec4 dst, vec4 cof, vec4 ind, int equation)
{
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
    else if (equation == 8) return (src + vec4(0.0) - dst) * cof; 

    return src;
}

vec4 CalculateSphereRate(int sphere_color_type, vec4 const_color, float sphere_rate_color)
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

vec4 CalculateOutput(int flag)
{
    if (flag == 10)
         return texture(cTextureBaseColor, SelectTexCoord(base_color_uv_selector));
    else if (flag == 15) //in_attr11 vertex colors
          return fVertexColor;
    else if (flag == 20)
        return texture(cTextureNormal,     SelectTexCoord(normal_uv_selector));
    else if (flag == 30)
        return vec4(fNormalsDepth.rgb, 0.0); //used in normals when no normal map present
    else if (flag == 50)
         return texture(cTextureUniform0, SelectTexCoord(uniform0_uv_selector));
    else if (flag == 51)
         return texture(cTextureUniform1, SelectTexCoord(uniform1_uv_selector));
    else if (flag == 52)
         return texture(cTextureUniform2, SelectTexCoord(uniform2_uv_selector));
    else if (flag == 53)
         return texture(cTextureUniform3, SelectTexCoord(uniform3_uv_selector));
    else if (flag == 54)
         return texture(cTextureUniform4, SelectTexCoord(uniform4_uv_selector));
    else if (flag == 60) //sphere rate 0
        return CalculateSphereRate(SPHERE_CONST_COLOR0, mat.const_color0, mat.sphere_rate_color0);
    else if (flag == 61) //sphere rate 1
        return CalculateSphereRate(SPHERE_CONST_COLOR1, mat.const_color1, mat.sphere_rate_color1);
    else if (flag == 62) //sphere rate 2
        return CalculateSphereRate(SPHERE_CONST_COLOR2, mat.const_color2, mat.sphere_rate_color2);
    else if (flag == 63) //sphere rate 3
        return CalculateSphereRate(SPHERE_CONST_COLOR3, mat.const_color3, mat.sphere_rate_color3);
    else if (flag == 70) return vec4(0.0); //cFrameBufferTex tex
    else if (flag == 71) return vec4(0.0); //cGBufferBaseColorTex tex
    else if (flag == 72) return vec4(0.0); //cGBufferNormalTex tex
    else if (flag == 73) return vec4(0.0); //gbuffer decode from base color
    else if (flag == 74) return vec4(0.0); //gbuffer decode from base color
    else if (flag == 78) return vec4(0.0); //linear depth

    else if (flag == 80) return vec4(0.0); //blend 0
    else if (flag == 81) return vec4(0.0); //blend 1
    else if (flag == 82) return vec4(0.0); //blend 2
    else if (flag == 83) return vec4(0.0); //blend 3
    else if (flag == 84) return vec4(0.0); //blend 4
    else if (flag == 85) return vec4(0.0); //blend 5

    else if (flag == 110) return vec4(mat.const_single0); //Mat.const_single0
    else if (flag == 111) return vec4(mat.const_single1); //Mat.const_single1
    else if (flag == 112) return vec4(mat.const_single2); //Mat.const_single2
    else if (flag == 113) return vec4(mat.const_single3); //Mat.const_single3
    else if (flag == 115) return vec4(0.0); //constant
    else if (flag == 116) return vec4(1.0); //constant
    else if (flag == 140) return vec4(0.0); //ModelAdditionalInfo.uv_offset.z
    else if (flag == 160) return vec4(0.0); //proc texture 2d
    else if (flag == 170) return vec4(0.0); //proc texture 3d

    else if (flag == 8) 
         return vec4(0.0); //blend todo
    else
        return vec4(0.0);

    return vec4(0.0);
}

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
    if (ENABLE_NORMAL_MAP)
    {
        tangent_normal = ReconstructNormal(normal_map);
    }
    return normalize(tbn_matrix * tangent_normal).xyz;
}

void main()
{
    vec4 base_color   = CalculateOutput(o_base_color);
    vec2 normal_map   = CalculateOutput(o_normal).rg;
    float metalness   = CalculateOutput(o_metalness).r;
    float roughness   = CalculateOutput(o_roughness).r;
    vec4 sss          = CalculateOutput(o_sss);

    //Roughness adjust
    roughness *= mat.force_roughness;
    roughness = saturate(roughness);

    //Emission (todo)
    vec3 emissionTerm = vec3(0.0);

    //Normals
    vec3 N = CalculateNormals(fNormalsDepth.rg, normal_map);

    //Sphere mapping
    vec3 sphere_map = textureLod(cTextureMaterialLightSphere, CalcSphereCoords(N.xyz), sqrt(roughness)).rgb;

    //PBR
    vec3 I = vec3(0,0,-1) * mat3(mdlEnvView.cView);
    vec3 V = normalize(I); // view
    vec3 L = normalize(fViewDirection.xyz ); // Light
    vec3 H = normalize(V + L); // half angle
    vec3 R = reflect(I, N); // reflection
    float NV = saturate(dot(N, I));

    vec3 f0 = mix(vec3(0.04), base_color.rgb, metalness); // dialectric
    vec3 kS = FresnelSchlickRoughness(max(dot(N, H), 0.0), f0, roughness);

    const float MAX_LOD = 5.0;

    vec4 irradiance_cubemap = DecodeCubemap(cTextureMaterialLightCube, N, roughness * MAX_LOD);
    irradiance_cubemap.rgb *= mdlEnvView.Exposure.y;

    vec3 specularTerm = irradiance_cubemap.rgb * 0.3 * kS;

    //Diffuse
    vec3 diffuseTerm = base_color.rgb;

    //Adjust for metalness.
    diffuseTerm *= clamp(1.0 - metalness, 0.0, 1.0);
    diffuseTerm *= vec3(1) - kS;

    oLightBuf.rgb = diffuseTerm.rgb * fLightColor.xyz + specularTerm + emissionTerm;

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