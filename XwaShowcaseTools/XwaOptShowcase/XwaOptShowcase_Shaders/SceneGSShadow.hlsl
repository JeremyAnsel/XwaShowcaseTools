#include "SceneCommon.hlsl"

void DetectAndProcessSilhouette(
    GSShadowIn v1,
    GSShadowIn v2,
    float3 lightDir,
    inout TriangleStream<PSShadowIn> ShadowTriangleStream
)
{
    float3 outpos[4];
    outpos[0] = v1.pos - g_fExtrudeBias * lightDir;
    outpos[1] = v1.pos - g_fExtrudeAmt * lightDir;
    outpos[2] = v2.pos - g_fExtrudeBias * lightDir;
    outpos[3] = v2.pos - g_fExtrudeAmt * lightDir;

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

    if (dot(N, lightDirection.xyz) < 0.0f)
    {
        DetectAndProcessSilhouette(In[0], In[1], lightDirection.xyz, ShadowTriangleStream);
        DetectAndProcessSilhouette(In[1], In[2], lightDirection.xyz, ShadowTriangleStream);
        DetectAndProcessSilhouette(In[2], In[0], lightDirection.xyz, ShadowTriangleStream);
        
        PSShadowIn output;
        int v;

		//near cap
        for (v = 0; v < 3; v++)
        {
            float4 pos = float4(In[v].pos - g_fExtrudeBias * lightDirection.xyz, 1.0f);
            pos = mul(pos, view);
            pos = mul(pos, projection);
            output.pos = pos;

            ShadowTriangleStream.Append(output);
        }

        ShadowTriangleStream.RestartStrip();

		//far cap (reverse the order)
        for (v = 2; v >= 0; v--)
        {
            float4 pos = float4(In[v].pos - g_fExtrudeAmt * lightDirection.xyz, 1.0f);
            pos = mul(pos, view);
            pos = mul(pos, projection);
            output.pos = pos;

            ShadowTriangleStream.Append(output);
        }

        ShadowTriangleStream.RestartStrip();
    }
}
