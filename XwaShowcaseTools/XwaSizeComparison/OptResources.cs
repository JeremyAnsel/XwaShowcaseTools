using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace XwaSizeComparison
{
    class OptResources
    {
        private DeviceResources deviceResources;

        public D3D11ShaderResourceView PositionsSRV;

        public D3D11ShaderResourceView NormalsSRV;

        public D3D11ShaderResourceView TextureCoordinatesSRV;

        public readonly D3D11ShaderResourceView[] Textures = new D3D11ShaderResourceView[100];

        public uint SolidVertexBufferLength;

        public D3D11Buffer SolidVertexBuffer;

        public uint TransparentVertexBufferLength;

        public D3D11Buffer TransparentVertexBuffer;

        public XMUInt4[] TransparentIndices;

        public XMVector[] TransparentCenters;

        public OptResources()
        {
        }

        public void CreateDeviceDependentResources(DeviceResources resources, SceneData sceneData)
        {
            this.deviceResources = resources;

            if (sceneData is null)
            {
                return;
            }

            this.CreateBuffers(sceneData);

            Dictionary<string, int> texturesNameToIndex = this.CreateTextures(sceneData);

            this.CreateSolidVertexBuffer(sceneData, texturesNameToIndex);
            this.CreateTransparentVertexBuffer(sceneData, texturesNameToIndex);
        }

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.PositionsSRV);
            D3D11Utils.DisposeAndNull(ref this.NormalsSRV);
            D3D11Utils.DisposeAndNull(ref this.TextureCoordinatesSRV);

            for (int i = 0; i < this.Textures.Length; i++)
            {
                D3D11Utils.DisposeAndNull(ref this.Textures[i]);
            }

            D3D11Utils.DisposeAndNull(ref this.SolidVertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.TransparentVertexBuffer);
        }

        private void CreateBuffers(SceneData sceneData)
        {
            var device = this.deviceResources.D3DDevice;

            if (sceneData.Positions.Count != 0)
            {
                var positions = sceneData.Positions.ToArray();
                using (var buffer = device.CreateBuffer(D3D11BufferDesc.From(positions, D3D11BindOptions.ShaderResource, D3D11Usage.Immutable), positions, 0, 0))
                {
                    this.PositionsSRV = device.CreateShaderResourceView(buffer, new D3D11ShaderResourceViewDesc(buffer, DxgiFormat.R32G32B32Float, 0, (uint)positions.Length));
                }
            }

            if (sceneData.Normals.Count != 0)
            {
                var normals = sceneData.Normals.ToArray();
                using (var buffer = device.CreateBuffer(D3D11BufferDesc.From(normals, D3D11BindOptions.ShaderResource, D3D11Usage.Immutable), normals, 0, 0))
                {
                    this.NormalsSRV = device.CreateShaderResourceView(buffer, new D3D11ShaderResourceViewDesc(buffer, DxgiFormat.R32G32B32Float, 0, (uint)normals.Length));
                }
            }

            if (sceneData.TextureCoordinates.Count != 0)
            {
                var textureCoordinates = sceneData.TextureCoordinates.ToArray();
                using (var buffer = device.CreateBuffer(D3D11BufferDesc.From(textureCoordinates, D3D11BindOptions.ShaderResource, D3D11Usage.Immutable), textureCoordinates, 0, 0))
                {
                    this.TextureCoordinatesSRV = device.CreateShaderResourceView(buffer, new D3D11ShaderResourceViewDesc(buffer, DxgiFormat.R32G32Float, 0, (uint)textureCoordinates.Length));
                }
            }
        }

        private Dictionary<string, int> CreateTextures(SceneData sceneData)
        {
            var device = this.deviceResources.D3DDevice;

            var texturesNameToIndex = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            var texturesIndexToName = new Dictionary<int, string>();
            var texturesCount = new List<int>();
            var texturesSize = new List<(int Width, int Height)>();

            foreach (var textureData in sceneData.Textures)
            {
                int index = texturesSize.LastIndexOf((textureData.Value.Width, textureData.Value.Height));

                if (index == -1 || texturesCount[index] == 1024)
                {
                    index = texturesSize.Count;
                    texturesCount.Add(0);
                    texturesSize.Add((textureData.Value.Width, textureData.Value.Height));

                    if (texturesSize.Count > 100)
                    {
                        throw new NotSupportedException($"Too many different textures sizes.");
                    }
                }

                texturesCount[index]++;

                int textureIndex = index * 1024 + texturesCount[index] - 1;
                texturesNameToIndex[textureData.Key] = textureIndex;
                texturesIndexToName[textureIndex] = textureData.Key;
            }

            for (int mapArrayIndex = 0; mapArrayIndex < texturesCount.Count; mapArrayIndex++)
            {
                var dimensions = texturesSize[mapArrayIndex];
                int mipmapsCount = sceneData.Textures[texturesIndexToName[mapArrayIndex * 1024]].Texture.MipmapsCount;
                int mapCount = texturesCount[mapArrayIndex];
                int subresourcesCount = mipmapsCount * mapCount * 2;

                D3D11SubResourceData[] textureSubResData = new D3D11SubResourceData[subresourcesCount];

                for (int mapIndex = 0; mapIndex < mapCount; mapIndex++)
                {
                    var textureData = sceneData.Textures[texturesIndexToName[mapArrayIndex * 1024 + mapIndex]];

                    for (int level = 0; level < mipmapsCount; level++)
                    {
                        int width;
                        int height;

                        // color
                        {
                            int subResIndex = (mapIndex * 2 + 0) * mipmapsCount + level;

                            byte[] imageData = textureData.Texture.GetMipmapImageData(level, out width, out height);
                            textureSubResData[subResIndex] = new D3D11SubResourceData(imageData, (uint)width * 4, (uint)(width * height * 4));
                        }

                        // normal illlum
                        {
                            int subResIndex = (mapIndex * 2 + 1) * mipmapsCount + level;

                            byte[] imageData = textureData.NormalMap?.GetMipmapImageData(level, out _, out _) ?? new byte[width * height * 4];
                            byte[] illumData = textureData.Texture.GetMipmapIllumData(level, out _, out _);

                            int length = width * height;

                            for (int i = 0; i < length; i++)
                            {
                                imageData[i * 4 + 3] = illumData == null ? (byte)0 : illumData[i];
                            }

                            textureSubResData[subResIndex] = new D3D11SubResourceData(imageData, (uint)width * 4, (uint)(width * height * 4));
                        }
                    }
                }

                D3D11Texture2DDesc textureDesc = new(DxgiFormat.B8G8R8A8UNorm, (uint)dimensions.Width, (uint)dimensions.Height, (uint)mapCount * 2, (uint)mipmapsCount, D3D11BindOptions.ShaderResource, D3D11Usage.Immutable);

                using (var texture = device.CreateTexture2D(textureDesc, textureSubResData))
                {
                    D3D11ShaderResourceViewDesc textureViewDesc = new(D3D11SrvDimension.Texture2DArray, textureDesc.Format, 0, textureDesc.MipLevels, 0, textureDesc.ArraySize);
                    D3D11ShaderResourceView textureView = device.CreateShaderResourceView(texture, textureViewDesc);

                    this.Textures[mapArrayIndex] = textureView;
                }
            }

            return texturesNameToIndex;
        }

        private void CreateSolidVertexBuffer(SceneData sceneData, Dictionary<string, int> texturesNameToIndex)
        {
            var device = this.deviceResources.D3DDevice;

            this.SolidVertexBufferLength = (uint)sceneData.SolidMeshIndices.Count;

            var vertices = new XMUInt4[sceneData.SolidMeshIndices.Count];

            foreach (var meshData in sceneData.Meshes.Where(t => !t.HasAlpha))
            {
                uint textureIndex = (uint)texturesNameToIndex[meshData.TextureName];

                for (uint i = meshData.IndexStart; i < meshData.IndexStart + meshData.IndexCount; i++)
                {
                    XMUInt4 index = sceneData.SolidMeshIndices[(int)i];
                    index.W = textureIndex;

                    vertices[(int)i] = index;
                }
            }

            if (vertices.Length != 0)
            {
                this.SolidVertexBuffer = device.CreateBuffer(
                    D3D11BufferDesc.From(vertices, D3D11BindOptions.VertexBuffer, D3D11Usage.Immutable),
                    vertices,
                    0,
                    0);
            }
        }

        private void CreateTransparentVertexBuffer(SceneData sceneData, Dictionary<string, int> texturesNameToIndex)
        {
            var device = this.deviceResources.D3DDevice;

            this.TransparentVertexBufferLength = (uint)sceneData.TransparentMeshIndices.Count;

            var vertices = new XMUInt4[sceneData.TransparentMeshIndices.Count];
            this.TransparentCenters = new XMVector[sceneData.TransparentMeshIndices.Count / 3];

            foreach (var meshData in sceneData.Meshes.Where(t => t.HasAlpha))
            {
                uint textureIndex = (uint)texturesNameToIndex[meshData.TextureName];

                for (uint i = meshData.IndexStart; i < meshData.IndexStart + meshData.IndexCount; i += 3)
                {
                    XMUInt4 index0 = sceneData.TransparentMeshIndices[(int)i + 0];
                    index0.W = textureIndex;
                    vertices[(int)i + 0] = index0;

                    XMUInt4 index1 = sceneData.TransparentMeshIndices[(int)i + 1];
                    index1.W = textureIndex;
                    vertices[(int)i + 1] = index1;

                    XMUInt4 index2 = sceneData.TransparentMeshIndices[(int)i + 2];
                    index1.W = textureIndex;
                    vertices[(int)i + 2] = index2;

                    XMVector center = XMVector.Zero;
                    center += sceneData.Positions[(int)index0.X];
                    center += sceneData.Positions[(int)index1.X];
                    center += sceneData.Positions[(int)index2.X];
                    center /= 3;
                    this.TransparentCenters[(int)i / 3] = center;
                }
            }

            this.TransparentIndices = vertices;

            if (vertices.Length != 0)
            {
                this.TransparentVertexBuffer = device.CreateBuffer(
                    D3D11BufferDesc.From(vertices, D3D11BindOptions.VertexBuffer),
                    vertices,
                    0,
                    0);
            }
        }
    }
}
