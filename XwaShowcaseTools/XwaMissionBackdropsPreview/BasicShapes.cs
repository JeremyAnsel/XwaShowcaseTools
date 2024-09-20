using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DXMath;
using System;

namespace XwaMissionBackdropsPreview;

internal sealed class BasicShapes
{
    private readonly D3D11Device d3dDevice;

    public BasicShapes(D3D11Device d3dDevice)
    {
        this.d3dDevice = d3dDevice ?? throw new ArgumentNullException(nameof(d3dDevice));
    }

    public void CreateSphereArc(
        float angleWidth,
        float angleHeight,
        out D3D11Buffer vertexBuffer,
        out D3D11Buffer indexBuffer,
        out int vertexCount,
        out int indexCount)
    {
        int thetaDiv = (int)(angleWidth / Math.PI * 16) + 1;
        int phiDiv = (int)(angleHeight / Math.PI * 16) + 1;

        float dt = angleWidth / thetaDiv;
        float dtStart = XMMath.PI - angleWidth / 2;
        float dp = angleHeight / phiDiv;
        float dpStart = XMMath.PIDivTwo - angleHeight / 2;

        int numVertices = (thetaDiv + 1) * (phiDiv + 1);
        var vertices = new BasicVertex[numVertices];

        for (int pi = 0; pi <= phiDiv; pi++)
        {
            float phi = dpStart + pi * dp;

            for (int ti = 0; ti <= thetaDiv; ti++)
            {
                float theta = dtStart + ti * dt;

                float x = -XMScalar.Cos(theta) * XMScalar.Sin(phi);
                float y = XMScalar.Cos(phi);
                float z = XMScalar.Sin(theta) * XMScalar.Sin(phi);

                var p = new XMFloat3(x, y, z);
                var n = new XMFloat3(-x, -y, -z);
                var uv = new XMFloat2((float)ti / thetaDiv, (float)pi / phiDiv);

                int vetexIndex = pi * (thetaDiv + 1) + ti;
                vertices[vetexIndex] = new(p, n, uv);
            }
        }

        int rows = phiDiv + 1;
        int columns = thetaDiv + 1;

        int numIndices = (rows - 1) * (columns - 1) * 6;
        var indices = new ushort[numIndices];
        uint index = 0;

        for (var i = 0; i < rows - 1; i++)
        {
            for (var j = 0; j < columns - 1; j++)
            {
                var ij = (i * columns) + j;

                indices[index++] = (ushort)(ij + 1);
                indices[index++] = (ushort)(ij + 1 + columns);
                indices[index++] = (ushort)(ij);

                indices[index++] = (ushort)(ij + columns);
                indices[index++] = (ushort)(ij);
                indices[index++] = (ushort)(ij + 1 + columns);
            }
        }

        this.CreateVertexBuffer(vertices, out vertexBuffer);

        try
        {
            this.CreateIndexBuffer(indices, out indexBuffer);
        }
        catch
        {
            D3D11Utils.DisposeAndNull(ref vertexBuffer);
            throw;
        }

        vertexCount = numVertices;
        indexCount = numIndices;
    }

    public void CreatePlane(
        float width,
        float height,
        float z,
        out D3D11Buffer vertexBuffer,
        out D3D11Buffer indexBuffer,
        out int vertexCount,
        out int indexCount)
    {
        var p0 = new XMFloat3(z, -height / 2, -width / 2);
        var n0 = new XMFloat3(-p0.X, -p0.Y, -p0.Z);
        var uv0 = new XMFloat2(0, 0);

        var p1 = new XMFloat3(z, height / 2, -width / 2);
        var n1 = new XMFloat3(-p1.X, -p1.Y, -p1.Z);
        var uv1 = new XMFloat2(1, 0);

        var p2 = new XMFloat3(z, height / 2, width / 2);
        var n2 = new XMFloat3(-p2.X, -p2.Y, -p2.Z);
        var uv2 = new XMFloat2(1, 1);

        var p3 = new XMFloat3(z, -height / 2, width / 2);
        var n3 = new XMFloat3(-p3.X, -p3.Y, -p3.Z);
        var uv3 = new XMFloat2(0, 1);

        var vertices = new BasicVertex[4];
        vertices[0] = new(p0, n0, uv0);
        vertices[1] = new(p1, n1, uv1);
        vertices[2] = new(p2, n2, uv2);
        vertices[3] = new(p3, n3, uv3);

        var indices = new ushort[6];
        uint index = 0;

        indices[index++] = 0;
        indices[index++] = 1;
        indices[index++] = 2;

        indices[index++] = 0;
        indices[index++] = 2;
        indices[index++] = 3;

        this.CreateVertexBuffer(vertices, out vertexBuffer);

        try
        {
            this.CreateIndexBuffer(indices, out indexBuffer);
        }
        catch
        {
            D3D11Utils.DisposeAndNull(ref vertexBuffer);
            throw;
        }

        vertexCount = vertices.Length;
        indexCount = indices.Length;
    }

    private void CreateVertexBuffer(BasicVertex[] vertexData, out D3D11Buffer vertexBuffer)
    {
        D3D11BufferDesc vertexBufferDesc = D3D11BufferDesc.From(vertexData, D3D11BindOptions.VertexBuffer);
        vertexBuffer = this.d3dDevice.CreateBuffer(vertexBufferDesc, vertexData, 0, 0);
    }

    private void CreateIndexBuffer(ushort[] indexData, out D3D11Buffer indexBuffer)
    {
        D3D11BufferDesc indexBufferDesc = D3D11BufferDesc.From(indexData, D3D11BindOptions.IndexBuffer);
        indexBuffer = this.d3dDevice.CreateBuffer(indexBufferDesc, indexData, 0, 0);
    }
}
