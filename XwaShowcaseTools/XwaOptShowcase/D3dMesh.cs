using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.GameWindow;

namespace XwaOptShowcase
{
    class D3dMesh
    {
        private D3D11Buffer vertexBuffer;

        private D3D11Buffer indexBuffer;

        public D3dMesh(DeviceResources deviceResources, SceneMesh mesh)
        {
            this.CreateDeviceDependentResources(deviceResources, mesh);
        }

        public D3D11Buffer VertexBuffer => this.vertexBuffer;

        public D3D11Buffer IndexBuffer => this.indexBuffer;

        public uint IndicesCount { get; private set; }

        public string Texture { get; private set; }

        public bool HasAlpha { get; private set; }

        public void CreateDeviceDependentResources(DeviceResources resources, SceneMesh mesh)
        {
            var vertices = mesh.Vertices.ToArray();
            this.vertexBuffer = resources.D3DDevice.CreateBuffer(
                D3D11BufferDesc.From(vertices, D3D11BindOptions.VertexBuffer),
                vertices,
                0,
                0);

            var indices = mesh.Indices.ToArray();
            this.indexBuffer = resources.D3DDevice.CreateBuffer(
                D3D11BufferDesc.From(indices, D3D11BindOptions.IndexBuffer),
                indices,
                0,
                0);

            this.IndicesCount = (uint)indices.Length;

            this.Texture = mesh.Texture;
            this.HasAlpha = mesh.HasAlpha;
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.indexBuffer);
        }
    }
}
