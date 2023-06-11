#include "SceneCommon.hlsl"

PSSceneIn main(VSSceneIn input)
{
    PSSceneIn output;

    float4 pos = float4(input.pos, 1.0f);
    pos = mul(pos, world);
    pos = mul(pos, view);
    pos = mul(pos, projection);
    output.pos = pos;

    output.norm = normalize(mul(normalize(input.norm), (float3x3) world));

    output.tex = input.tex;

    float4 posWorld = float4(input.pos, 1.0f);
    posWorld = mul(posWorld, world);
    output.posWorld = posWorld;

    output.posView = mul(posWorld, view);

    return output;
}
