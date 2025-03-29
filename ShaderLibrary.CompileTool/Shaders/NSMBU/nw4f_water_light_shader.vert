#version 450 core

#include "shared.glsl"

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
layout (location = 1) out vec4 fTexCoords1;
layout (location = 2) out vec4 fTexCoords2;
layout (location = 3) out vec4 fTexCoords3;
layout (location = 4) out vec4 fProjPos;

void main()
{	
	//position
	vec4 worldPosition = skin(aPosition.xyz, aBlendIndex);
    gl_Position = vec4(worldPosition.xyz, 1.0) * cViewProj;

	// Todo unsure
	vec3 view_pos = (vec4(worldPosition.xyz, 1.0) * mat4(cView)).xyz;

	fTexCoords0.xy = calc_texcoord_matrix(material.texmtx0, vec2(view_pos.x, view_pos.y));	
	fTexCoords1.xy = calc_texcoord_matrix(material.texmtx0, vec2(view_pos.x, view_pos.y));	
	fTexCoords2.xy = calc_texcoord_matrix(material.texmtx0, vec2(view_pos.x, view_pos.y));	
	fTexCoords3.xy = calc_texcoord_matrix(material.texmtx0, vec2(view_pos.x, view_pos.y));	

	vec3 ndc = gl_Position.xyz / gl_Position.w; //perspective divide/normalize
	fProjPos.x = ndc.x;
	fProjPos.y = ndc.y;
    return;
}
