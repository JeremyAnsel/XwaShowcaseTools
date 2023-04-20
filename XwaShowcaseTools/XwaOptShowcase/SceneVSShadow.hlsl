#include "SceneCommon.hlsl"

GSShadowIn main(VSSceneIn input)
{
    GSShadowIn output;

    float4 pos = mul(float4(input.pos, 1), world);
    output.pos = pos.xyz;
    
    output.norm = mul(input.norm, (float3x3) world);
    
    return output;
}
