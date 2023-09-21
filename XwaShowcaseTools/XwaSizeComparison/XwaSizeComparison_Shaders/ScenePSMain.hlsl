#include "SceneCommon.hlsl"
#include "SceneTextureArray.hlsl"

// from http://www.thetenthplanet.de/archives/1180
float3x3 cotangent_frame(float3 N, float3 p, float2 uv)
{
    // get edge vec­tors of the pix­el tri­an­gle
    float3 dp1 = ddx(p);
    float3 dp2 = ddy(p);
    float2 duv1 = ddx(uv);
    float2 duv2 = ddy(uv);

    // solve the lin­ear sys­tem
    float3 dp2perp = cross(dp2, N);
    float3 dp1perp = cross(N, dp1);
    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    // con­struct a scale-invari­ant frame 
    float invmax = rsqrt(max(dot(T, T), dot(B, B)));
    return float3x3(T * invmax, B * invmax, N);
}

float3 perturb_normal(float3 N, float3 V, float2 texcoord, uint c)
{
    // assume N, the inter­po­lat­ed ver­tex nor­mal and 
    // V, the view vec­tor (ver­tex to eye)
    float4 texelNormalIllum = GetTextureArrayValue(c, texcoord, 1);

    if (length(texelNormalIllum.xyz) == 0)
    {
        return N;
    }

    float3x3 TBN = cotangent_frame(N, V, texcoord);
    float3 NM = normalize((texelNormalIllum.xyz * 2.0) - 1.0);
    // Align the normal map axes with the view axes
    NM.xy = -NM.xy;
    NM = mul(NM, TBN);

    // NM_intensity can be a constant set in the Constant Buffer. It's used to
    // modulate the intensity of the normal mapping effect.
    const float NM_intensity = 0.75f;
    N = lerp(N, NM, NM_intensity);
    return N;
}

float4 main(PSSceneIn input) : SV_TARGET
{
    int c = input.index.w;

    float4 texelColor = GetTextureArrayValue(c, input.tex, 0);
    float4 texelNormalIllum = GetTextureArrayValue(c, input.tex, 1);
        
    float4 color;

    if (texelNormalIllum.w >= 0.2f)
    {
        color = float4(texelColor.xyz, 1.0f);
    }
    else
    {
        float3x3 V = (float3x3) view;
        float3 N = normalize(float3(input.norm.x, input.norm.y, input.norm.z));
        // Normal mapping uses the eye vector, which is defined in viewspace coords.
        // To make the coord sys consistent, we need to compute everything in viewspace coords too.
        N = normalize(mul(N, V));
        const float3 L = normalize(mul(lightDirection.xyz, V));

        // Apply normal mapping
        N = perturb_normal(N, -input.posView.xyz, input.tex, c);

        float colorAttenuation = max(0, dot(L, N));

        //float colorAttenuation = max(0, dot(normalize(mul(lightDirection.xyz, (float3x3) view)), N));
        float3 colorAmbient = texelColor.xyz * g_ambientFactor;
        float3 colorDiffuse = texelColor.xyz * colorAttenuation * g_diffuseFactor;
        color = float4(colorAmbient + colorDiffuse, texelColor.w);
    }

    return saturate(color);
}
