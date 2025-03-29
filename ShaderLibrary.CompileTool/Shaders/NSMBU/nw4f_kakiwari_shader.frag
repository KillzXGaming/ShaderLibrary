#version 450 core

#include "shared.glsl"


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
	// Alpha
	colorOutput.a *= fFogColor.a;
	// Color output with fog applied
	FragData0.rgb = mix(colorOutput.rgb, fFogColor.rgb, fFogColor.a);
	FragData0.a = colorOutput.a;

	vec3 normals = ReconstructNormal(fNormals.xy);
	FragData1.rgb = normalize(normals.rgb) * 0.5 + 0.5;
	FragData1.a = 1.0;
    return;
}
