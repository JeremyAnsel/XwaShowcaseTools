#include "SceneCommon.hlsl"

#define g_shadowMinOffset 0.11f
#define g_shadowFactorOffset 0.12f

float GetDepthOffset(float3 pos)
{
    float d1 = max(mul(pos, (float3x3) view).z * g_shadowFactorOffset, g_shadowMinOffset);
    //float d1 = max(sqrt(mul(pos, (float3x3) view).z), g_shadowMinOffset);
    return d1;
}

void DetectAndProcessSilhouette(
    GSShadowIn v1,
    GSShadowIn v2,
    float3 lightDir,
    inout TriangleStream<PSShadowIn> ShadowTriangleStream
)
{
    float d1 = GetDepthOffset(v1.pos);
    float fExtrudeBias1 = d1;
    float fExtrudeAmt1 = 100000.0f - d1;

    float d2 = GetDepthOffset(v2.pos);
    float fExtrudeBias2 = d2;
    float fExtrudeAmt2 = 100000.0f - d2;

    float3 outpos[4];
    outpos[0] = v1.pos - fExtrudeBias1 * lightDir;
    outpos[1] = v1.pos - fExtrudeAmt1 * lightDir;
    outpos[2] = v2.pos - fExtrudeBias2 * lightDir;
    outpos[3] = v2.pos - fExtrudeAmt2 * lightDir;

    PSShadowIn output;

    for (int v = 0; v < 4; v++)
    {
        float4 pos = float4(outpos[v], 1.0f);
        pos = mul(pos, view);
        pos = mul(pos, projection);
        output.pos = pos;

        ShadowTriangleStream.Append(output);
    }

    ShadowTriangleStream.RestartStrip();
}

[maxvertexcount(18)]
void main(triangle GSShadowIn In[3], inout TriangleStream<PSShadowIn> ShadowTriangleStream)
{
    float3 N = normalize(cross(In[1].pos - In[0].pos, In[2].pos - In[0].pos));

    if (dot(N, lightDirection.xyz) > 0.0f)
    {
        DetectAndProcessSilhouette(In[0], In[1], lightDirection.xyz, ShadowTriangleStream);
        DetectAndProcessSilhouette(In[1], In[2], lightDirection.xyz, ShadowTriangleStream);
        DetectAndProcessSilhouette(In[2], In[0], lightDirection.xyz, ShadowTriangleStream);
        
        PSShadowIn output;
        int v;

		//near cap
        for (v = 0; v < 3; v++)
        {
            float d1 = GetDepthOffset(In[v].pos);
            float fExtrudeBias1 = d1;
            float fExtrudeAmt1 = 100000.0f - d1;

            float4 pos = float4(In[v].pos - fExtrudeBias1 * lightDirection.xyz, 1.0f);
            pos = mul(pos, view);
            pos = mul(pos, projection);
            output.pos = pos;

            ShadowTriangleStream.Append(output);
        }

        ShadowTriangleStream.RestartStrip();

		//far cap (reverse the order)
        for (v = 2; v >= 0; v--)
        {
            float d1 = GetDepthOffset(In[v].pos);
            float fExtrudeBias1 = d1;
            float fExtrudeAmt1 = 100000.0f - d1;

            float4 pos = float4(In[v].pos - fExtrudeAmt1 * lightDirection.xyz, 1.0f);
            pos = mul(pos, view);
            pos = mul(pos, projection);
            output.pos = pos;

            ShadowTriangleStream.Append(output);
        }

        ShadowTriangleStream.RestartStrip();
    }
}
