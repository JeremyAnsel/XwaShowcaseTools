#include "SceneCommon.hlsl"

Buffer<float3> g_vertices : register(t0);
Buffer<float3> g_normals : register(t1);
Buffer<float2> g_textureCoords : register(t2);

GSShadowIn main(VSSceneIn input)
{
    GSShadowIn output;

    float3 v = input.index.x == -1 ? (float3)0 : g_vertices[input.index.x];
    float3 n = input.index.y == -1 ? (float3)0 : g_normals[input.index.y];
    float2 t = input.index.z == -1 ? (float2)0 : g_textureCoords[input.index.z];
    uint c = input.index.w;

    float4 pos = mul(float4(v, 1), world);
    output.pos = pos.xyz;
    
    output.norm = mul(n, (float3x3) world);
    
    return output;
}
