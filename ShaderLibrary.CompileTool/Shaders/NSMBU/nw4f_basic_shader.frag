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

layout (binding = 0) uniform sampler2D cTexMap0;
layout (binding = 2) uniform sampler2D cLightMap1;
layout (binding = 1) uniform sampler2D cLightMap0;

layout (location = 0) in vec2 fTexCoords0;
layout (location = 1) in vec4 fNormals;
layout (location = 2) in vec4 fVtxColor0;
layout (location = 3) in vec4 fFogColor;

layout (location = 0) out vec4 FragData0;
layout (location = 1) out vec4 FragData1;

void main()
{	
	// Texture map
    vec4 colorOutput = texture(cTexMap0, fTexCoords0.xy).xyzw;
	// Sphere maps
	vec2 sphere_coords = calc_sphere_coords(fNormals.xyz);
    vec3 diffuseLight  = texture(cLightMap0, sphere_coords).xyz;
    vec3 specularLight = texture(cLightMap1, sphere_coords).xyz;

	vec3 normals = ReconstructNormal(fNormals.xy);

	// Color
	colorOutput *= fFogColor.rgb;
	colorOutput *= diffuseLight.rgb;
	colorOutput += specularLight.rgb;

	// Alpha
	colorOutput.a *= fFogColor.a;

	// Color output with fog applied
	FragData0.rgb = mix(colorOutput.rgb, fFogColor.rgb, fFogColor.a);
	FragData0.a = colorOutput.a;
	// Fog distance
	FragData1.rgb = normalize(normals.rgb) * 0.5 + 0.5;
	FragData1.a = 1.0;
    return;
}
