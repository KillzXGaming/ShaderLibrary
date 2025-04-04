﻿#version 450 core

#include "shared.glsl"

#define enable_vertex_alpha 0
#define material_alpha 0
#define gsys_alpha_test_enable 0
#define gsys_alpha_test_func 6
#define gsys_renderstate 0
#define enable_debug_flag 0

#define ENABLE_NORMAL_MAP 0
#define ENABLE_SPEC_MASK 0
#define INDIRECT_LIGHT_TYPE 0
#define AO_TYPE 0

#define ENABLE_SHADOW 0
#define ENABLE_PROJ_FOG 0
#define ENABLE_ADD_COLOR 0

layout (binding = 1) uniform sampler2D cNormalMap0;
layout (binding = 6) uniform sampler2D cBakeMap1;
layout (binding = 5) uniform sampler2D cBakeMap0;
layout (binding = 2) uniform sampler2D cSpecMaskMap0;
layout (binding = 8) uniform sampler2DShadow cShadowMap;
layout (binding = 0) uniform sampler2D cTexMap0;
layout (binding = 3) uniform sampler2D cLightMap0;
layout (binding = 4) uniform sampler2D cLightMap1;
layout (binding = 7) uniform sampler2D cTexMapProjFog;

layout (location = 0) in vec2 fTexCoords0;
layout (location = 1) in vec4 fTexCoordNormal;
layout (location = 2) in vec4 fTexCoordSpecMask;
layout (location = 3) in vec4 fTexCoordBake0;
layout (location = 4) in vec4 fShadowCoords;
layout (location = 5) in vec4 fFogProjCoords;
layout (location = 6) in vec4 fNormals;
layout (location = 7) in vec4 fTangents;
layout (location = 8) in vec4 fBitangents;
layout (location = 9) in vec4 fVtxColor0;

layout (location = 0) out vec4 FragData0;
layout (location = 1) out vec4 FragData1;

void main()
{	
	// Texture maps
    vec4 colorOutput   = texture(cTexMap0,      fTexCoords0.xy).xyzw;
    vec4 normalMap     = texture(cNormalMap0,   fTexCoordNormal.xy).xyzw;
    vec4 bake_shadow   = texture(cBakeMap0,     fTexCoordBake0.xy).xyzw;
    vec4 bake_light    = texture(cBakeMap1,     fTexCoordBake0.xy).xyzw;
	float specularMask = texture(cSpecMaskMap0, fTexCoordSpecMask.xy).x;

	float specular = 0.0;
	#if (ENABLE_SPEC_MASK == 1)
		 specular  = specularMask;
	#endif

	// Fragment normals using TBN, blue channel calculated
	vec3 normals = ReconstructNormal(fNormals.xy);
	#if  (ENABLE_NORMAL_MAP == 1)
		 normals = CalculateNormals(fNormals.xyz, fTangents, fBitangents, normalMap.xy);
	#endif

	// Sphere maps
	vec2 sphere_coords = calc_sphere_coords(normals.xyz);
    vec3 diffuseLight  = texture(cLightMap0, sphere_coords).xyz;
    vec3 specularLight = texture(cLightMap1, sphere_coords).xyz;

	vec3 shadow = vec3(1.0);

	// AO_TYPE 1 == vertex colors
	// AO_TYPE 2 == texture bake0
	#if (AO_TYPE == 1)
		shadow = mix(vec3(1.0) - material.shadow_color.rgb, fVtxColor0.rgb, fVtxColor0.rgb);
	#endif
	#if (AO_TYPE == 2)
		shadow = mix(vec3(1.0) - material.shadow_color.rgb, bake_shadow.rgb, bake_shadow.rgb);
	#endif
	// Shadow cast
	#if (ENABLE_SHADOW == 1)
		float depth_shadow = texture(cShadowMap, fShadowCoords.xyz * (gl_FragCoord.w * (1.0 / fShadowCoords.w))).x;
		shadow = mix(vec3(1.0) - material.depth_shadow_color.rgb, vec3(depth_shadow), depth_shadow);
		shadow *= material.depth_shadow_color.a;
	#endif

	vec3 lighting = vec3(1.0) - shadow * colorOutput.a;
	// Bake lighting
	#if (INDIRECT_LIGHT_TYPE == 1)
		 lighting += bake_light.rgb * bake_light.a * material.gsys_bake_light_scale.rgb ;
	#endif

	// Color
	#if (ENABLE_ADD_COLOR == 1)
	{
		colorOutput.rgb += material.color0.rgb;
		colorOutput.a *= material.color0.a;
	}
	#endif

	colorOutput.rgb *= diffuseLight.rgb;
	colorOutput.rgb += specular * specularLight.rgb;
	colorOutput.rgb += lighting;

	// Fog

	// AO as vertex colors
	#if (ENABLE_PROJ_FOG == 1)
		vec4 fogProj = texture(cTexMapProjFog, fFogProjCoords.xy * (gl_FragCoord.w * (1.0 / fFogProjCoords.w)));
		// Color output with fog applied
		FragData0.rgb = mix(colorOutput.rgb, fogProj.rgb, fogProj.a);
		FragData0.a = colorOutput.a;
	#else
		FragData0.rgb = colorOutput.rgb;
		FragData0.a = colorOutput.a;
	#endif

	// Normal output
	FragData1.rgb = normalize(normals.rgb) * 0.5 + 0.5;
	FragData1.a = 1.0;
    return;
}
