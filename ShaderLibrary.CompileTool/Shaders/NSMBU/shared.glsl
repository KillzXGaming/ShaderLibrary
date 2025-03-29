

const int FOG_MAX  = 8;
const int LIGHT_OBJ_MAX  = 8;
const int CHANNEL_MAX  = 2;

layout (binding = 0, std140) uniform MdlEnvView
{
    mat3x4        cView;
    mat4x4        cViewProj;
    vec3        cLightDiffDir[ LIGHT_OBJ_MAX ];
    vec4        cLightDiffColor[ LIGHT_OBJ_MAX ];
    vec4        cAmbColor[ CHANNEL_MAX ];
    vec3        cFogColor[ FOG_MAX ];
    float       cFogStart[ FOG_MAX ];
    float       cFogStartEndInv[ FOG_MAX ];
    mat4x4        cShadowMtx;
    vec3        cFogDir[ FOG_MAX ];
    mat4x4        cTexProjMtx;
};

layout (binding = 1, std140) uniform MdlMtx
{
    mat3x4        cMtxPalette[ 64 ];
};

layout (binding = 2, std140) uniform Shp
{
    mat3x4        cShpMtx[ 3 ];
    int         cWeightNum;
};

layout (binding = 3, std140) uniform Material
{
    vec4 mat_color0;
    vec4 mat_color1;
    vec4 amb_color0;
    vec4 amb_color1;
    vec4 tev_color0;
    vec4 tev_color1;
    vec4 tev_color2;
    vec4 konst0;
    vec4 konst1;
    vec4 konst2;
    vec4 konst3;
    mat2x4 ind_texmtx0;
    mat2x4 ind_texmtx1;
    mat2x4 ind_texmtx2;
    mat2x4 texmtx0;
    mat2x4 texmtx1;
    mat2x4 texmtx2;
    mat2x4 texmtx3;
    mat2x4 texmtx4;
    mat2x4 texmtx5;
    mat2x4 texmtx6;
    mat2x4 texmtx8;

    float gsys_alpha_test_ref_value;
    float fog_index;
    float shadow_power;
    float padding;

    vec4 color0;
    vec4 shadow_color;
    vec4 gsys_bake_st0;
    vec4 depth_shadow_color;
    vec4 gsys_bake_light_scale;

    mat2x4 tex_proj_anim_mtx;

    vec2 indirect_power;
    float reflect_power;
    float spec_power;

    vec4 texture_warp_scale;
}material;

float saturate(float v)
{
    return clamp(v, 0.0, 1.0);
}

vec3 ReconstructNormal(in vec2 t_NormalXY) {
    float t_NormalZ = sqrt(clamp(1.0 - dot(t_NormalXY.xy, t_NormalXY.xy), 0.0, 1.0));
    return vec3(t_NormalXY.xy, t_NormalZ);
}

vec3 CalculateNormals(vec3 normals, vec4 tangents, vec4 bitangents, vec2 normal_map)
{
    vec3 N = vec3(normals);
    vec3 T = vec3(tangents.xyz);
    vec3 B = vec3(bitangents.xyz);

    vec3 tangent_normal = ReconstructNormal(normal_map);
    return normalize(mat3(T, B, N) * tangent_normal).xyz;
}

vec2 calc_sphere_coords(vec3 view_normal)
{
	//center the uvs
	return view_normal.xy * vec2(0.5) + vec2(0.5,-0.5);
}

vec4 skin(vec3 pos, ivec4 index)
{
    vec4 newPosition = vec4(pos.xyz, 1.0);

    if (cWeightNum == 0)
        newPosition = vec4(pos, 1.0) * mat4(cShpMtx);
    if (cWeightNum == 1)
        newPosition = vec4(pos, 1.0) * mat4(cMtxPalette[index.x]);

    if (cWeightNum >  1)
        newPosition =  vec4(pos, 1.0) * mat4(cMtxPalette[index.x]) * aBoneWeight.x;
    if (cWeightNum >= 2)
        newPosition += vec4(pos, 1.0) * mat4(cMtxPalette[index.y]) * aBoneWeight.y;
    if (cWeightNum >= 3)
        newPosition += vec4(pos, 1.0) * mat4(cMtxPalette[index.z]) * aBoneWeight.z;
    if (cWeightNum >= 4)
        newPosition += vec4(pos, 1.0) * mat4(cMtxPalette[index.w]) * aBoneWeight.w;
        
    return newPosition;
}

vec3 skinNormal(vec3 nr, ivec4 index)
{
    vec3 newNormal = nr;

    if (cWeightNum == 0)
        newNormal =  nr * mat3(cShpMtx);
    if (cWeightNum == 1)
        newNormal =  nr * mat3(cMtxPalette[index.x]);

    if (cWeightNum >  1)
        newNormal =  nr * mat3(cMtxPalette[index.x]) * aBoneWeight.x;
    if (cWeightNum >= 2)
        newNormal += nr *  mat3(cMtxPalette[index.y]) * aBoneWeight.y;
    if (cWeightNum >= 3)
        newNormal += nr * mat3(cMtxPalette[index.z]) * aBoneWeight.z;
    if (cWeightNum >= 4)
        newNormal += nr * mat3(cMtxPalette[index.w]) * aBoneWeight.w;
    
    return newNormal;
}

vec2 CalcScaleBias(in vec2 t_Pos, in vec4 t_SB) {
    return t_Pos.xy * t_SB.xy + t_SB.zw;
}

vec2 calc_texcoord_matrix(mat2x4 mat, vec2 tex_coord)
{
	//actually a 2x3 matrix stored in 2x4
    vec2 tex_coord_out;
    tex_coord_out.x = fma(tex_coord.x, mat[0].x, tex_coord.y * mat[0].z) + mat[1].x;
    tex_coord_out.y = fma(tex_coord.x, mat[0].y, tex_coord.y * mat[0].w) + mat[1].y;
	return tex_coord_out;
}
