#version 450 core

#include "shared.glsl"

#define enable_vertex_alpha 0
#define material_alpha 4
#define gsys_alpha_test_enable 0
#define gsys_alpha_test_func 6
#define gsys_renderstate 0
#define enable_debug_flag 0

#define var_light_map 1
#define var_normal_map 0
#define var_spec_mask 0

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec4 aTangent;
layout (location = 3) in vec4 aBlendWeight;
layout (location = 4) in ivec4 aBlendIndex;
layout (location = 5) in vec2 aTexCoord0;
layout (location = 6) in vec2 aTexCoord1;
layout (location = 7) in vec2 aTexCoord2;
layout (location = 8) in vec2 aTexCoord3;
layout (location = 9) in vec4 aColor0;
layout (location = 10) in vec4 aColor1;

layout (location = 0) out vec2 fTexCoords0;
layout (location = 1) out vec4 fNormals;
layout (location = 2) out vec4 fVtxColor0;
layout (location = 3) out vec4 fFogColor;


void main()
{	
	//position
	vec4 position = skin(aPosition.xyz, aBlendIndex);
    gl_Position = vec4(position.xyz, 1.0) * cViewProj;

	// View position for fog calculations
	vec3 view_pos = (vec4(position.xyz, 1.0) * mat4(cView)).xyz;

	//material tex coords
	// tex_mtx0 used always
	fTexCoords0.xy = calc_texcoord_matrix(material.texmtx0, aTexCoord0.xy);	

	//normals which are in view space
	fNormals.xyz = normalize(skinNormal(aNormal.xyz, aBlendIndex).xyz) * mat3(cView);

	// Vertex color
	fVtxColor0 = aColor0;

	int fogIndex = int(material.fog_index);
	// Fog color
	fFogColor.xyz = cFogColor[fogIndex].xyz;
	// Fog amount
	fFogColor.w = saturate((-view_pos.z - cFogStart[fogIndex]) * cFogStartEndInv[fogIndex]);
    return;
}
