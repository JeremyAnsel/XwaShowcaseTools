#include "SceneCommon.hlsl"

Texture2D g_texture : register(t0);
Texture2D g_texture2 : register(t1);
Texture2D g_textureNormalMap : register(t2);
SamplerState g_sampler : register(s0);

float4 main(PSSceneIn input) : SV_TARGET
{
    //if (input.posWorld.z < cuttingDistanceFrom || input.posWorld.z > cuttingDistanceTo)
    //{
    //    discard;
    //}

    if (isWireframe)
    {
        return float4(0.0f, 0.0f, 1.0f, 1.0f);
    }
    
    return float4(0.0f, 0.0f, 1.0f, 1.0f);
}
