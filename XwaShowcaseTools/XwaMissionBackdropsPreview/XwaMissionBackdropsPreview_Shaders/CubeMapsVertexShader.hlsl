
cbuffer ConstantBuffer : register(b0)
{
    matrix world;
    matrix view;
    matrix projection;
}

struct VertexShaderInput
{
    float4 pos : POSITION;
};

struct PixelShaderInput
{
    float4 pos : SV_POSITION;
    float4 position : COLOR0;
};

PixelShaderInput main(VertexShaderInput input)
{
    PixelShaderInput output;

    output.pos = input.pos;
    output.pos = mul(output.pos, world);
    output.pos = mul(output.pos, view);
    output.pos = mul(output.pos, projection);
    output.position = input.pos;

    return output;
}
