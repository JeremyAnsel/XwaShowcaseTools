#include "SceneCommon.hlsl"

Buffer<float3> g_vertices : register(t0);
Buffer<float3> g_normals : register(t1);
Buffer<float2> g_textureCoords : register(t2);

PSSceneIn main(VSSceneIn input)
{
    PSSceneIn output;
    
    float3 v = input.index.x == -1 ? (float3) 0 : g_vertices[input.index.x];
    float3 n = input.index.y == -1 ? (float3) 0 : g_normals[input.index.y];
    float2 t = input.index.z == -1 ? (float2) 0 : g_textureCoords[input.index.z];
    uint c = input.index.w;

    output.index = input.index;

    float4 pos = float4(v, 1.0f);
    pos = mul(pos, world);
    pos = mul(pos, view);
    pos = mul(pos, projection);
    output.pos = pos;

    output.norm = normalize(mul(normalize(n), (float3x3) world));

    output.tex = t;

    float4 posWorld = float4(v, 1.0f);
    posWorld = mul(posWorld, world);
    output.posWorld = posWorld;
    
    output.posView = mul(posWorld, view);

    return output;
}
