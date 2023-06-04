#include "SceneCommon.hlsl"

Texture2D g_texture : register(t0);
Texture2D g_texture2 : register(t1);
SamplerState g_sampler : register(s0);

float4 main(PSSceneIn input) : SV_TARGET
{
    if (input.posWorld.z < cuttingDistanceFrom || input.posWorld.z > cuttingDistanceTo)
    {
        discard;
    }

    if (isWireframe)
    {
        return float4(0.0f, 0.0f, 1.0f, 1.0f);
    }
    
    float4 texelColor = g_texture.Sample(g_sampler, input.tex);
    float4 texelColorIllum = g_texture2.Sample(g_sampler, input.tex);

    float4 color;

    if (texelColorIllum.w >= 0.2f)
    {
        color = float4(texelColorIllum.xyz, 1.0f);

    }
    else if (texelColor.w <= 0.8f)
    {
        float colorAttenuation = max(0, dot(lightDirection.xyz, input.norm));
        float3 colorAmbient = texelColor.xyz * g_ambientFactor;
        float3 colorDiffuse = texelColor.xyz * colorAttenuation * g_diffuseFactor;
        color = float4(colorAmbient + colorDiffuse, texelColor.w);
    }
    else
    {
        color = float4(texelColor.xyz * g_shadowFactor, texelColor.w);
    }
    
    return saturate(color);
}
