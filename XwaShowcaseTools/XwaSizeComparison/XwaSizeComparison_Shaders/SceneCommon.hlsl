
cbuffer ConstantBufferGlobal : register(b0)
{
    matrix world;
    matrix view;
    matrix projection;
    float4 lightDirection;
    float lightBrightness;
};

struct VSSceneGroundIn
{
    float3 pos : POSITION;
};

struct PSSceneGroundIn
{
    float4 pos : SV_POSITION;
};

struct VSSceneIn
{
    uint4 index : POSITION;
};

struct PSSceneIn
{
    uint4 index : POSITION;
    float4 pos : SV_POSITION;
    float3 norm : NORMAL;
    float2 tex : TEXCOORD0;
    float4 posWorld : POSITION1;
    float4 posView : POSITION2;
};

struct GSShadowIn
{
    float3 pos : POS;
    float3 norm : TEXCOORD0;
};

struct PSShadowIn
{
    float4 pos : SV_Position;
};

#define g_diffuseFactor (1.0f * lightBrightness)
//#define g_ambientFactor (0.2f * lightBrightness)
#define g_ambientFactor (0.3f * lightBrightness)
//#define g_shadowFactor (0.1f * lightBrightness)
#define g_shadowFactor (0.2f * lightBrightness)
