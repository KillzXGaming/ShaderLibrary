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

layout (location = 0) out vec2 fTexCoords0;
layout (location = 1) out vec4 fTexCoordIndirectA;
layout (location = 2) out vec4 fTexCoordIndirectB;
layout (location = 3) out vec4 fFragPos;
layout (location = 4) out vec4 fNormals;
layout (location = 5) out vec4 fTangents;
layout (location = 6) out vec4 fBitangents;
layout (location = 7) out vec4 fFogColor;

void main()
{	
	//position
	vec4 worldPosition = skin(aPosition.xyz, aBlendIndex);
    gl_Position = vec4(worldPosition.xyz, 1.0) * cViewProj;

	vec3 view_pos = (vec4(worldPosition.xyz, 1.0) * mat4(cView)).xyz;

	// Material tex coords
	// tex_mtx0 used always
	fTexCoords0.xy = aTexCoord0.xy;	

	fTexCoordIndirectA.xy = calc_texcoord_matrix(material.texmtx0, aTexCoordNormal.xy);	
	fTexCoordIndirectB.xy = calc_texcoord_matrix(material.texmtx1, aTexCoordNormal.xy);	

	fFragPos = gl_Position;

	// Normals which are in view space
	vec3 worldNormal = skinNormal(aNormal.xyz, aBlendIndex).xyz;
	fNormals.xyz = normalize(worldNormal * mat3(cView));

	// Tangents
	fTangents.xyz = normalize(skinNormal(aTangent.xyz, aBlendIndex).xyz) * mat3(cView);
	fTangents.w = aTangent.w;
    fBitangents.xyz = normalize(cross(fNormals.xyz, fTangents.xyz) * fTangents.w);
	fBitangents.w = 1.0;

    fTexCoordBake0.xy = CalcScaleBias(aTexCoordBake0.xy, material.gsys_bake_st0);

	int fogIndex = int(material.fog_index);
	// Fog color
	fFogColor.xyz = cFogColor[fogIndex].xyz;
	// Fog amount
	fFogColor.w = saturate((-view_pos.z - cFogStart[fogIndex]) * cFogStartEndInv[fogIndex]);
    return;
}
