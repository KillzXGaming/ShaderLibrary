#version 450 core

precision mediump float;

//must match the mesh using this material
#define cSkinWeightNum 4

const int MAX_BONE_COUNT = 100;

layout (binding = 3, std140) uniform MdlMtx
{
    vec4 cBoneMatrices[3 * MAX_BONE_COUNT];
};

layout (binding = 4, std140) uniform _Shp
{
    vec4 cTransform[3];
} shape;

layout (binding = 1, std140) uniform MdlEnvView
{
    vec4 cView[3];
    vec4 cViewInv[3];
    vec4 cViewProj[4];
    mat3x4 cViewProjInv;
    mat4 cProjInv;
    mat3x4 cProjInvNoPos;
    vec4 Exposure; //[20]
    vec4 Dir;
    vec4 ZNearFar; //[22] //Near, Far, Far - Near, 1 / (Far - Near)
    vec2 TanFov;
    vec2 ProjOffset;
    vec4 ScreenSize;
    vec4 CameraPos;

} mdlEnvView;

layout (location = 0) in vec4 vPosition;
layout (location = 10) in vec4 vBoneWeight;
layout (location = 11) in ivec4 vBoneIndices;

#define dot2(a, b)	((a).x * (b).x + (a).y * (b).y)
#define dot3(a, b)	(dot2((a), (b)) + (a).z * (b).z)
#define calcDotVec4Vec3One(a, b)	(dot3((a), (b)) + (a).w)

vec4 multMtx44Vec3( vec4 mtx[4], vec3 v )
{
	vec4 ret;
	ret.x = calcDotVec4Vec3One( mtx[0], v );
	ret.y = calcDotVec4Vec3One( mtx[1], v );
	ret.z = calcDotVec4Vec3One( mtx[2], v );
	ret.w = calcDotVec4Vec3One( mtx[3], v );
	return ret;
}

vec3 multMtx34Vec3( vec4 mtx[3], vec3 v )
{
	vec3 ret;
	ret.x = calcDotVec4Vec3One( mtx[0], v );
	ret.y = calcDotVec4Vec3One( mtx[1], v );
	ret.z = calcDotVec4Vec3One( mtx[2], v );
	return ret;
}


vec3 calculateBoneWeight(vec3 v, int index, float weight)
{
	vec3 ret;
	ret.x = calcDotVec4Vec3One(cBoneMatrices[index + 0], v );
	ret.y = calcDotVec4Vec3One(cBoneMatrices[index + 1], v );
	ret.z = calcDotVec4Vec3One(cBoneMatrices[index + 2], v );
    return weight * ret;
}

vec3 skin(vec3 pos, ivec4 index)
{
    vec3 newPosition = vec3(pos.xyz);

    if (cSkinWeightNum == 0) newPosition = multMtx34Vec3(shape.cTransform, pos);

    if (cSkinWeightNum >= 1) newPosition =  calculateBoneWeight(pos, index.x * 3,  vBoneWeight.x);
    if (cSkinWeightNum >= 2) newPosition += calculateBoneWeight(pos, index.y * 3,  vBoneWeight.y);
    if (cSkinWeightNum >= 3) newPosition += calculateBoneWeight(pos, index.z * 3,  vBoneWeight.z);
    if (cSkinWeightNum >= 4) newPosition += calculateBoneWeight(pos, index.w * 3,  vBoneWeight.w);

    return newPosition;
}
 
void main()
{   
    ivec4 bone_index = vBoneIndices;
    //position
    vec3 position = skin(vPosition.xyz, bone_index);
    gl_Position = multMtx44Vec3(mdlEnvView.cViewProj, position.xyz);
    return;
}
