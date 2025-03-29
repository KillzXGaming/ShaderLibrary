#version 450 core

#include "shared.glsl"

layout (location = 0) in vec4 fVtxColor0;
layout (location = 1) in vec4 fFogColor;

layout (location = 0) out vec4 FragData0;

void main()
{	
	// Water edge using fog distance and color param
	// With vertex color + vertex alpha
	FragData0.rgb = mix(fVtxColor0.rgb, material.color0.rgb, fFogColor.a);
	FragData0.a = fVtxColor0.a;
    return;
}
