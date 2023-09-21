#include "SceneCommon.hlsl"
#include "SceneTextureArray.hlsl"

float4 main(PSSceneIn input) : SV_TARGET
{
    uint c = input.index.w;

    float4 texelColor = GetTextureArrayValue(c, input.tex, 0);
    float4 texelNormalIllum = GetTextureArrayValue(c, input.tex, 1);
    
    float4 color;

    if (texelNormalIllum.w >= 0.2f)
    {
        color = float4(texelColor.xyz, 1.0f);
    }
    else if (texelColor.w <= 0.8f)
    {
        float3 lightDir = normalize(lightDirection.xyz);
        float3 normal = normalize(input.norm);
        float colorAttenuation = max(0, dot(lightDir, normal));
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
