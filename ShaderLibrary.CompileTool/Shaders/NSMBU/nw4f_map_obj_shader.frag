#version 450 core

#include "shared.glsl"

#define ENABLE_NORMAL_MAP 0
#define ENABLE_SPEC_MASK 0
#define ENABLE_ENV_MAP 0
#define ENABLE_VERTEX_COLOR 0

layout (binding = 0) uniform sampler2D cTexMap0;
layout (binding = 1) uniform sampler2D cNormalMap0;
layout (binding = 2) uniform sampler2D cSpecMaskMap0;
layout (binding = 3) uniform sampler2D cLightMap0;
layout (binding = 4) uniform sampler2D cLightMap1;
layout (binding = 5) uniform sampler2D cEnvMap0;

layout (location = 0) in vec2 fTexCoords0;
layout (location = 1) in vec4 fTexCoordNormal;
layout (location = 2) in vec4 fTexCoordSpecMask;
layout (location = 3) in vec4 fTexCoordBake0;
layout (location = 4) in vec4 fNormals;
layout (location = 5) in vec4 fTangents;
layout (location = 6) in vec4 fBitangents;
layout (location = 7) in vec4 fVtxColor0;

layout (location = 0) out vec4 FragData0;
layout (location = 1) out vec4 FragData1;

void main()
{	
	// Texture maps
    vec4 colorOutput   = texture(cTexMap0,      fTexCoords0.xy).xyzw;
    vec4 normalMap     = texture(cNormalMap0,   fTexCoordNormal.xy).xyzw;
	float specularMask = texture(cSpecMaskMap0, fTexCoordSpecMask.xy).x;

	float specular = 0.0;
	#if (ENABLE_SPEC_MASK == 1)
		 specular  = specularMask;
	#endif

	// Fragment normals using TBN, blue channel calculated
	vec3 normals = ReconstructNormal(fNormals.xy);
	#if (ENABLE_NORMAL_MAP == 1)
		 normals = CalculateNormals(fNormals.xyz, fTangents, fBitangents, normalMap.xy);
	#endif

	// Sphere maps
	vec2 sphere_coords = calc_sphere_coords(normals.xyz);
    vec3 diffuseLight  = texture(cLightMap0, sphere_coords).xyz;
    vec3 specularLight = texture(cLightMap1, sphere_coords).xyz;
    vec3 envLight      = texture(cEnvMap0,   sphere_coords).xyz;

	colorOutput.rgb *= diffuseLight.rgb;
	colorOutput.rgb += specular * specularLight.rgb + envLight.rgb;

	#if (ENABLE_VERTEX_COLOR == 1)
		colorOutput.rgb *= fVtxColor0.rgb;
	#endif

	// Normal output
	FragData1.rgb = normalize(normals.rgb) * 0.5 + 0.5;
	FragData1.a = 1.0;
    return;
}
