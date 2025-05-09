﻿#version 450 core

#include "shared.glsl"

#define ENABLE_NORMAL_MAP 0
#define ENABLE_SPEC_MASK 0
#define ENABLE_ENV_MAP 0
#define ENABLE_VERTEX_COLOR 0

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec4 aTangent;
layout (location = 3) in vec4 aBlendWeight;
layout (location = 4) in ivec4 aBlendIndex;
layout (location = 5) in vec2 aTexCoord0;
layout (location = 6) in vec2 aTexCoordNormal;
layout (location = 7) in vec2 aTexCoordSpecMask;
layout (location = 8) in vec2 aTexCoordBake0;
layout (location = 9) in vec4 aColor0;
layout (location = 10) in vec4 aColor1;

layout (location = 0) out vec4 fVtxColor0;
layout (location = 1) out vec4 fFogColor;

void main()
{	
	//position
	vec4 worldPosition = skin(aPosition.xyz, aBlendIndex);
    gl_Position = vec4(worldPosition.xyz, 1.0) * cViewProj;

	// Vertex color
	fVtxColor0 = aColor0;

	int fogIndex = int(material.fog_index);
	// Fog color
	fFogColor.xyz = cFogColor[fogIndex].xyz;
	// Fog amount
	fFogColor.w = saturate((-view_pos.z - cFogStart[fogIndex]) * cFogStartEndInv[fogIndex]);
    return;
}
