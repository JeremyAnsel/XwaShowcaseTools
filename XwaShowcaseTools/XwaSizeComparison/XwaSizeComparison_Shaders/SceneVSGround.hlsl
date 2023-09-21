#include "SceneCommon.hlsl"

PSSceneGroundIn main(VSSceneGroundIn input)
{
    PSSceneGroundIn output;

    float4 pos = float4(input.pos, 1.0f);
    pos = mul(pos, world);
    pos = mul(pos, view);
    pos = mul(pos, projection);
    output.pos = pos;

    return output;
}
