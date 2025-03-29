#version 450 core

#include "shared.glsl"

#define enable_vertex_alpha 0
#define material_alpha 0
#define gsys_alpha_test_enable 0
#define gsys_alpha_test_func 6
#define gsys_renderstate 0
#define enable_debug_flag 0

layout (binding = 1) uniform sampler2D cNormalMap0;
layout (binding = 2) uniform sampler2D cSpecMaskMap0;
layout (binding = 4) uniform sampler2D cLightMap1;
layout (binding = 3) uniform sampler2D cLightMap0;
layout (binding = 0) uniform sampler2D cTexMap0;

layout (location = 0) in vec2 fTexCoords0;
layout (location = 1) in vec4 fTexCoordNormal;
layout (location = 2) in vec4 fTexCoordSpecMask;
layout (location = 3) in vec4 fNormals;
layout (location = 4) in vec4 fTangents;
layout (location = 5) in vec4 fBitangents;
layout (location = 6) in vec4 fVtxColor0;
layout (location = 7) in vec4 fFogColor;

layout (location = 0) out vec4 FragData0;
layout (location = 1) out vec4 FragData1;

void main()
{	
	// Texture maps
    vec4 colorOutput   = texture(cTexMap0,      fTexCoords0.xy).xyzw;
    vec4 normalMap     = texture(cNormalMap0,   fTexCoordNormal.xy).xyzw;
    vec4 specularMask  = texture(cSpecMaskMap0, fTexCoordSpecMask.xy).xyzw;

	// Fragment normals using TBN, blue channel calculated
	vec3 normals = CalculateNormals(fNormals.xyz, fTangents, fTangents, normalMap.xy);

	// Sphere maps
	vec2 sphere_coords = calc_sphere_coords(normals.xyz);
    vec3 diffuseLight  = texture(cLightMap0, sphere_coords).xyz;
    vec3 specularLight = texture(cLightMap1, sphere_coords).xyz;

	// Color
	colorOutput *= fFogColor.rgb;
	colorOutput *= diffuseLight.rgb;
	colorOutput += specularMask.rrr * specularLight.rgb; // only red channel for mask

	// Alpha
	colorOutput.a *= fFogColor.a;

	// Color output with fog applied
	FragData0.rgb = mix(colorOutput.rgb, fFogColor.rgb, fFogColor.a);
	FragData0.a = colorOutput.a;
	// Fog distance
	FragData1.rgb = normalize( fNormals.rgb ) * 0.5 + 0.5;
	FragData1.a = 1.0;
    return;
}
