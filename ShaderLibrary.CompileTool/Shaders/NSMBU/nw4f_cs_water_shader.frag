#version 450 core

#include "shared.glsl"

#define ENABLE_NORMAL_MAP 1

layout (binding = 0) uniform sampler2D cTexMap0;
layout (binding = 2) uniform sampler2D cLightMap0;
layout (binding = 3) uniform sampler2D cLightMap1;
layout (binding = 1) uniform sampler2D cNormalMap0;
layout (binding = 4) uniform sampler2D cReflectionMap;

layout (location = 0) in vec2 fTexCoords0;
layout (location = 1) in vec4 fTexCoordIndirectA;
layout (location = 2) in vec4 fTexCoordIndirectB;
layout (location = 3) in vec4 fFragPos;
layout (location = 4) in vec4 fNormals;
layout (location = 5) in vec4 fTangents;
layout (location = 6) in vec4 fBitangents;
layout (location = 7) in vec4 fFogColor;

layout (location = 0) out vec4 FragData0;

void main()
{	
	// Texture maps
    vec4 colorOutput   = texture(cTexMap0,      fTexCoords0.xy).xyzw;
    vec4 normalMapA     = texture(cNormalMap0,   fTexCoordIndirectA.xy).xyzw;
    vec4 normalMapB     = texture(cNormalMap0,   fTexCoordIndirectB.xy).xyzw;

	// Fragment normals using TBN, blue channel calculated
	vec3 normals = ReconstructNormal(fNormals.xy);
	if (ENABLE_NORMAL_MAP == 1)
		  normals = CalculateNormals(fNormals.xyz, fTangents, fBitangents, normalMap.xy);

	// Sphere maps
	vec2 sphere_coords = calc_sphere_coords(normals.xyz);
    vec3 diffuseLight  = texture(cLightMap0, sphere_coords).xyz;
    vec3 specularLight = texture(cLightMap1, sphere_coords).xyz;

	vec2 screen_coords = gl_Position.xy / gl_Position.w; //perspective divide/normalize

	vec2 reflect_coords = ((screen_coords.xy * 0.5 + normals.xz) * material.indirect_power.xy) + 0.5;
    vec3 reflection = texture(cReflectionMap, reflect_coords);

	float reflect_mix = abs(normals.z) * material.reflect_power;
	colorOutput.rgb = mix(colorOutput.rgb, reflection.rgb, reflect_mix);

	colorOutput.rgb *= diffuseLight.rgb;
	colorOutput.rgb += specularLight.rgb * material.spec_power.w;

	FragData0 = colorOutput;
    return;
}
