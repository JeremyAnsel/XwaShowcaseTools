using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System.Collections.Generic;
using System.IO;

namespace XwaSizeComparison
{
    class GroundResources
    {
        private DeviceResources deviceResources;

        public D3D11VertexShader ShaderVSGround;
        public D3D11PixelShader ShaderPSGround;
        public D3D11PixelShader ShaderPSGroundShadow;

        public D3D11InputLayout InputLayout;

        public D3D11Buffer VertexBuffer;

        public D3D11Buffer IndexBuffer;

        public uint IndicesCount;

        public GroundResources()
        {
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var device = this.deviceResources.D3DDevice;

            this.ShaderVSGround = device.CreateVertexShader(File.ReadAllBytes("XwaSizeComparison_Shaders\\SceneVSGround.cso"), null);
            this.ShaderPSGround = device.CreatePixelShader(File.ReadAllBytes("XwaSizeComparison_Shaders\\ScenePSGround.cso"), null);
            this.ShaderPSGroundShadow = device.CreatePixelShader(File.ReadAllBytes("XwaSizeComparison_Shaders\\ScenePSGroundShadow.cso"), null);

            D3D11InputElementDesc[] basicVertexLayoutDesc = new[]
            {
                new D3D11InputElementDesc("POSITION", 0, DxgiFormat.R32G32B32Float, 0, 0, D3D11InputClassification.PerVertexData, 0),
            };

            this.InputLayout = device.CreateInputLayout(basicVertexLayoutDesc, File.ReadAllBytes("XwaSizeComparison_Shaders\\SceneVSGround.cso"));

            float size = 100000.0f;

            var verticesList = new List<XMFloat3>();
            var indicesList = new List<int>();

            verticesList.Add(new XMFloat3(-size / 2, 0, -size / 2));
            verticesList.Add(new XMFloat3(-size / 2, 0, size / 2));
            verticesList.Add(new XMFloat3(size / 2, 0, size / 2));
            verticesList.Add(new XMFloat3(size / 2, 0, -size / 2));

            indicesList.Add(0);
            indicesList.Add(1);
            indicesList.Add(2);

            indicesList.Add(0);
            indicesList.Add(2);
            indicesList.Add(3);

            var vertices = verticesList.ToArray();
            this.VertexBuffer = resources.D3DDevice.CreateBuffer(
                D3D11BufferDesc.From(vertices, D3D11BindOptions.VertexBuffer),
                vertices,
            0,
            0);

            var indices = indicesList.ToArray();
            this.IndexBuffer = resources.D3DDevice.CreateBuffer(
                D3D11BufferDesc.From(indices, D3D11BindOptions.IndexBuffer),
                indices,
                0,
                0);

            this.IndicesCount = (uint)indices.Length;
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.ShaderVSGround);
            D3D11Utils.DisposeAndNull(ref this.ShaderPSGround);
            D3D11Utils.DisposeAndNull(ref this.ShaderPSGroundShadow);

            D3D11Utils.DisposeAndNull(ref this.InputLayout);

            D3D11Utils.DisposeAndNull(ref this.VertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.IndexBuffer);
        }
    }
}
