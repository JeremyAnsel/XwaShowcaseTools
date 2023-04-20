
cbuffer ConstantBuffer : register(b0)
{
    matrix world;
    matrix view;
    matrix projection;
    float4 lightDirection;
    float cuttingDistanceFrom;
    float cuttingDistanceTo;
    int isWireframe;
    float lightBrightness;
};

struct VSSceneIn
{
    float3 pos : POSITION;
    float3 norm : NORMAL;
    float2 tex : TEXCOORD0;
};

struct PSSceneIn
{
    float4 pos : SV_POSITION;
    float3 norm : NORMAL;
    float2 tex : TEXCOORD0;
    float4 posWorld : POSITION;
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
#define g_ambientFactor (0.2f * lightBrightness)
#define g_shadowFactor (0.1f * lightBrightness)

#define EXTRUDE_EPSILON 0.01f
#define g_fExtrudeAmt (100.0f - EXTRUDE_EPSILON)
#define g_fExtrudeBias EXTRUDE_EPSILON
