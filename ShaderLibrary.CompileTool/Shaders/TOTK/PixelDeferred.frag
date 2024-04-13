#version 450 core

layout (binding = 2, std140) uniform _Context
{
    precise vec4 data[4096];
} Context;

layout (binding = 7, std140) uniform _Env
{
    precise vec4 data[4096];
} Env;

layout (binding = 11, std140) uniform _SceneMat
{
    precise vec4 data[4096];
} SceneMat;


layout (binding = 0, std430) buffer _fp_s0
{
    uint data[];
} fp_s0;

layout (binding = 1) uniform sampler2D cTex_GBuffNormal;
layout (binding = 4) uniform sampler2D cTex_NormalizedLinearDepth;
layout (binding = 5) uniform sampler2D cTex_HalfNormalizedLinearDepth;
layout (binding = 19) uniform sampler2D cTex_PreMisc;
layout (binding = 0) uniform sampler2D cTex_GBuffAlbedo;
layout (binding = 17) uniform sampler2D cTex_PreShadow;
layout (binding = 9) uniform samplerCube cTex_CubeEnvMap;
layout (binding = 29) uniform sampler2DArray cTex_DeferredLightPrePass;
layout (binding = 18) uniform sampler2D cTex_PreFog;

layout (location = 0) in vec4 in_attr0;
layout (location = 1) in vec4 in_attr1;
layout (location = 3) in vec4 in_attr3;

layout (location = 0) out vec4 out_attr0;

void main()
{
    //Dynamic light sources
    vec3 light_diffuse = texture(cTex_DeferredLightPrePass, vec3(in_attr0.xy, 0.0)).xyz;
    vec3 light_specular = texture(cTex_DeferredLightPrePass, vec3(in_attr0.xy, 1.0)).xyz;

	out_attr0.rgb = light_diffuse.rgb;
    out_attr0.a = 1.0;
}