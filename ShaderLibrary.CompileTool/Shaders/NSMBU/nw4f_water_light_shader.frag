#version 450 core

#include "shared.glsl"

#define ENABLE_NORMAL_MAP 0
#define ENABLE_SPEC_MASK 0
#define ENABLE_ENV_MAP 0
#define ENABLE_VERTEX_COLOR 0

layout (binding = 0) uniform sampler2D cTexMap0;
layout (binding = 1) uniform sampler2D cTexMap1;
layout (binding = 2) uniform sampler2D cGBufferMap;
layout (binding = 3) uniform sampler2D cTexMapIndirect;

layout (location = 0) in vec2 fTexCoords0;
layout (location = 1) in vec4 fTexCoords1;
layout (location = 2) in vec4 fTexCoords2;
layout (location = 3) in vec4 fTexCoords3;
layout (location = 4) in vec4 fScreenCoords;

layout (location = 0) out vec4 FragData0;
layout (location = 1) out vec4 FragData1;

void main()
{	
	// Texture maps
    vec2 indirectA     = texture(cTexMapIndirect, fTexCoords2.xy).xy;
	vec2 indirectB = texture(cTexMapIndirect, fTexCoords3.xy).xy;
    vec4 gBuffer   = texture(cGBufferMap, fScreenCoords.xy).xyzw;

    indirectA -= vec2(0.5);
    indirectB -= vec2(0.5);
    // Todo apply ind_texmtx0
    // Todo apply ind_texmtx1

    vec4 texA   = texture(cTexMap0, indirectA.xy).xyzw;
    vec4 texB   = texture(cTexMap1, indirectB.xy).xyzw;

    vec4 color = texA * material.tev_color0.rgba;
    vec4 specular = texB * material.tev_color1.rgba;

    // Todo apply konst scale
    float scale = 1.0; 

    FragData0 = scale * color + specular;
    return;
}
