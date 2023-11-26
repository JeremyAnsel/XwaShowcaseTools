using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.Xwa.Dat;
using JeremyAnsel.Xwa.HooksConfig;
using JeremyAnsel.Xwa.Opt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XwaOptShowcase
{
    class OptComponent : IGameComponent
    {
        public const int AnimationTotalTime = 29;

        private DeviceResources deviceResources;

        private readonly Dictionary<string, D3D11ShaderResourceView> textureViews = new();
        private readonly Dictionary<string, D3D11ShaderResourceView> textureViews2 = new();
        private readonly Dictionary<string, D3D11ShaderResourceView> textureViewsNormalMaps = new();

        private readonly List<D3dMesh> meshes = new();

        private D3D11Buffer vertexBuffer;
        private D3D11Buffer indexBuffer;

        private D3D11VertexShader shaderVSMain;
        private D3D11PixelShader shaderPSMain;
        private D3D11PixelShader shaderPSAmbient;
        private D3D11PixelShader shaderPSDepth;
        private D3D11VertexShader shaderVSShadow;
        private D3D11GeometryShader shaderGSShadow;
        private D3D11PixelShader shaderPSShadow;

        private D3D11InputLayout inputLayout;

        private D3D11Buffer constantBuffer;

        private D3D11SamplerState sampler;
        private D3D11RasterizerState DisableCullingRasterizerState;
        private D3D11RasterizerState EnableCullingRasterizerState;
        private D3D11RasterizerState rasterizerStateWireframe;
        private D3D11DepthStencilState EnableDepthDepthStencilState;
        private D3D11DepthStencilState DisableDepthWriteDepthStencilState;
        private D3D11DepthStencilState TwoSidedStencilDepthStencilState;
        private D3D11DepthStencilState RenderNonShadowsDepthStencilState;
        private D3D11DepthStencilState RenderInShadowsDepthStencilState;
        private D3D11BlendState DefaultBlendState;
        private D3D11BlendState AlphaBlendingBlendState;
        private D3D11BlendState DisableFrameBufferBlendState;
        private D3D11BlendState NoBlendingBlendState;
        private D3D11BlendState AdditiveBlendingBlendState;
        private D3D11BlendState SrcAlphaBlendingBlendState;

        public OptComponent(string optFilename, int version, string optObjectProfile, List<string> optObjectSkins)
        {
            this.OptFilename = optFilename;
            this.OptVersion = version;
            this.OptObjectProfile = optObjectProfile ?? "Default";
            this.OptObjectSkins.AddRange(optObjectSkins ?? new());
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel100;

        public string OptFilename { get; private set; }

        public int OptVersion { get; private set; }

        public string OptObjectProfile { get; private set; }

        public List<string> OptObjectSkins { get; } = new();

        public float OptSize { get; private set; }

        public Vector OptSpanSize { get; private set; }

        public Vector OptCenter { get; private set; }

        public XMMatrix LightTransform { get; set; }

        public float LightBrightness { get; set; }

        public XMMatrix WorldTransform { get; set; }

        public XMMatrix ViewTransform { get; set; }

        public XMMatrix ProjectionTransform { get; set; }

        public double Time { get; set; }

        public bool IsPaused { get; set; }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var device = this.deviceResources.D3DDevice;

            OptFile opt = OptFile.FromFile(this.OptFilename);

            var objectProfiles = OptModel.GetObjectProfiles(opt.FileName);
            //var objectSkins = OptModel.GetSkins(opt.FileName);

            if (!objectProfiles.TryGetValue(this.OptObjectProfile, out List<int> objectProfile))
            {
                objectProfile = objectProfiles["Default"];
            }

            opt = OptModel.GetTransformedOpt(opt, this.OptVersion, objectProfile, this.OptObjectSkins);

            opt.Scale(XMVector3.Length(SceneConstants.VecEye).X * 1.2f / (opt.Size * OptFile.ScaleFactor));

            this.OptSize = opt.Size * OptFile.ScaleFactor;
            this.OptSpanSize = opt.SpanSize.Scale(OptFile.ScaleFactor, OptFile.ScaleFactor, OptFile.ScaleFactor);

            Vector max = opt.MaxSize;
            Vector min = opt.MinSize;

            this.OptCenter = new Vector()
            {
                X = (max.X + min.X) / 2,
                Y = (max.Y + min.Y) / 2,
                Z = (max.Z + min.Z) / 2
            }.Scale(OptFile.ScaleFactor, OptFile.ScaleFactor, OptFile.ScaleFactor);

            opt.ConvertTextures8To32();

            this.CreateTextures(opt);
            this.CreateTextures2(opt);
            this.CreateTexturesNormalMaps(opt);
            this.CreateMeshes(opt);

            this.shaderVSMain = device.CreateVertexShader(File.ReadAllBytes("XwaOptShowcase_Shaders\\SceneVSMain.cso"), null);
            this.shaderPSMain = device.CreatePixelShader(File.ReadAllBytes("XwaOptShowcase_Shaders\\ScenePSMain.cso"), null);
            this.shaderPSAmbient = device.CreatePixelShader(File.ReadAllBytes("XwaOptShowcase_Shaders\\ScenePSAmbient.cso"), null);
            this.shaderPSDepth = device.CreatePixelShader(File.ReadAllBytes("XwaOptShowcase_Shaders\\ScenePSDepth.cso"), null);
            this.shaderVSShadow = device.CreateVertexShader(File.ReadAllBytes("XwaOptShowcase_Shaders\\SceneVSShadow.cso"), null);
            this.shaderGSShadow = device.CreateGeometryShader(File.ReadAllBytes("XwaOptShowcase_Shaders\\SceneGSShadow.cso"), null);
            this.shaderPSShadow = device.CreatePixelShader(File.ReadAllBytes("XwaOptShowcase_Shaders\\ScenePSShadow.cso"), null);

            D3D11InputElementDesc[] basicVertexLayoutDesc = new[]
            {
                new D3D11InputElementDesc("POSITION", 0, DxgiFormat.R32G32B32Float, 0, 0, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("NORMAL", 0, DxgiFormat.R32G32B32Float, 0, 12, D3D11InputClassification.PerVertexData, 0),
                new D3D11InputElementDesc("TEXCOORD", 0, DxgiFormat.R32G32Float, 0, 24, D3D11InputClassification.PerVertexData, 0)
            };

            this.inputLayout = device.CreateInputLayout(basicVertexLayoutDesc, File.ReadAllBytes("XwaOptShowcase_Shaders\\SceneVSMain.cso"));

            this.constantBuffer = device.CreateBuffer(new D3D11BufferDesc(D3dConstantBufferData.Size, D3D11BindOptions.ConstantBuffer));

            D3D11SamplerDesc samplerDesc = new(
                D3D11Filter.Anisotropic,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                D3D11TextureAddressMode.Wrap,
                0.0f,
                D3D11Constants.DefaultMaxAnisotropy,
                D3D11ComparisonFunction.Never,
                null,
                0.0f,
                float.MaxValue);

            this.sampler = device.CreateSamplerState(samplerDesc);

            this.DisableCullingRasterizerState = device.CreateRasterizerState(D3D11RasterizerDesc.Default with
            {
                CullMode = D3D11CullMode.None
            });

            this.EnableCullingRasterizerState = device.CreateRasterizerState(D3D11RasterizerDesc.Default with
            {
                CullMode = D3D11CullMode.Back
            });

            this.rasterizerStateWireframe = device.CreateRasterizerState(D3D11RasterizerDesc.Default with
            {
                CullMode = D3D11CullMode.None,
                FillMode = D3D11FillMode.WireFrame
            });

            this.DisableDepthWriteDepthStencilState = device.CreateDepthStencilState(D3D11DepthStencilDesc.Default with
            {
                DepthWriteMask = D3D11DepthWriteMask.Zero
            });

            this.EnableDepthDepthStencilState = device.CreateDepthStencilState(D3D11DepthStencilDesc.Default with
            {
                IsDepthEnabled = true,
                DepthWriteMask = D3D11DepthWriteMask.All
            });

            this.TwoSidedStencilDepthStencilState = device.CreateDepthStencilState(new D3D11DepthStencilDesc(
                true,
                D3D11DepthWriteMask.Zero,
                D3D11ComparisonFunction.Less,
                true,
                0xff,
                0xff,
                D3D11StencilOperation.Keep,
                D3D11StencilOperation.Decrement,
                D3D11StencilOperation.Keep,
                D3D11ComparisonFunction.Always,
                D3D11StencilOperation.Keep,
                D3D11StencilOperation.Increment,
                D3D11StencilOperation.Keep,
                D3D11ComparisonFunction.Always));

            this.RenderNonShadowsDepthStencilState = device.CreateDepthStencilState(new D3D11DepthStencilDesc(
                true,
                D3D11DepthWriteMask.Zero,
                D3D11ComparisonFunction.LessEqual,
                true,
                0xff,
                0,
                D3D11StencilOperation.Zero,
                D3D11StencilOperation.Keep,
                D3D11StencilOperation.Keep,
                D3D11ComparisonFunction.Equal,
                D3D11StencilOperation.Zero,
                D3D11StencilOperation.Keep,
                D3D11StencilOperation.Keep,
                D3D11ComparisonFunction.Equal));

            this.RenderInShadowsDepthStencilState = device.CreateDepthStencilState(new D3D11DepthStencilDesc(
                true,
                D3D11DepthWriteMask.Zero,
                D3D11ComparisonFunction.LessEqual,
                true,
                0xff,
                0,
                D3D11StencilOperation.Zero,
                D3D11StencilOperation.Keep,
                D3D11StencilOperation.Keep,
                D3D11ComparisonFunction.NotEqual,
                D3D11StencilOperation.Zero,
                D3D11StencilOperation.Keep,
                D3D11StencilOperation.Keep,
                D3D11ComparisonFunction.NotEqual));

            this.DefaultBlendState = device.CreateBlendState(D3D11BlendDesc.Default);

            D3D11BlendDesc AlphaBlendingBlendDesc = D3D11BlendDesc.Default;
            D3D11RenderTargetBlendDesc[] AlphaBlendingBlendDescRenderTargets = AlphaBlendingBlendDesc.GetRenderTargets();
            AlphaBlendingBlendDescRenderTargets[0].IsBlendEnabled = true;
            AlphaBlendingBlendDescRenderTargets[0].SourceBlend = D3D11BlendValue.SourceAlpha;
            AlphaBlendingBlendDescRenderTargets[0].DestinationBlend = D3D11BlendValue.InverseSourceAlpha;
            AlphaBlendingBlendDescRenderTargets[0].BlendOperation = D3D11BlendOperation.Add;
            AlphaBlendingBlendDescRenderTargets[0].SourceBlendAlpha = D3D11BlendValue.One;
            AlphaBlendingBlendDescRenderTargets[0].DestinationBlendAlpha = D3D11BlendValue.InverseSourceAlpha;
            AlphaBlendingBlendDescRenderTargets[0].BlendOperationAlpha = D3D11BlendOperation.Add;
            AlphaBlendingBlendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            AlphaBlendingBlendDesc.SetRenderTargets(AlphaBlendingBlendDescRenderTargets);
            this.AlphaBlendingBlendState = device.CreateBlendState(AlphaBlendingBlendDesc);

            var DisableFrameBufferBlendDesc = D3D11BlendDesc.Default;
            var DisableFrameBufferBlendDescRenderTargets = DisableFrameBufferBlendDesc.GetRenderTargets();
            DisableFrameBufferBlendDescRenderTargets[0].IsBlendEnabled = false;
            DisableFrameBufferBlendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.None;
            DisableFrameBufferBlendDesc.SetRenderTargets(DisableFrameBufferBlendDescRenderTargets);
            this.DisableFrameBufferBlendState = device.CreateBlendState(DisableFrameBufferBlendDesc);

            var NoBlendingBlendDesc = D3D11BlendDesc.Default;
            NoBlendingBlendDesc.IsAlphaToCoverageEnabled = false;
            var NoBlendingBlendDescRenderTargets = NoBlendingBlendDesc.GetRenderTargets();
            NoBlendingBlendDescRenderTargets[0].IsBlendEnabled = false;
            NoBlendingBlendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            NoBlendingBlendDesc.SetRenderTargets(NoBlendingBlendDescRenderTargets);
            this.NoBlendingBlendState = device.CreateBlendState(NoBlendingBlendDesc);

            var AdditiveBlendingBlendDesc = D3D11BlendDesc.Default;
            AdditiveBlendingBlendDesc.IsAlphaToCoverageEnabled = false;
            var AdditiveBlendingBlendDescRenderTargets = AdditiveBlendingBlendDesc.GetRenderTargets();
            AdditiveBlendingBlendDescRenderTargets[0].IsBlendEnabled = true;
            AdditiveBlendingBlendDescRenderTargets[0].SourceBlend = D3D11BlendValue.One;
            AdditiveBlendingBlendDescRenderTargets[0].DestinationBlend = D3D11BlendValue.One;
            AdditiveBlendingBlendDescRenderTargets[0].BlendOperation = D3D11BlendOperation.Add;
            AdditiveBlendingBlendDescRenderTargets[0].SourceBlendAlpha = D3D11BlendValue.Zero;
            AdditiveBlendingBlendDescRenderTargets[0].DestinationBlendAlpha = D3D11BlendValue.Zero;
            AdditiveBlendingBlendDescRenderTargets[0].BlendOperationAlpha = D3D11BlendOperation.Add;
            AdditiveBlendingBlendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            AdditiveBlendingBlendDesc.SetRenderTargets(AdditiveBlendingBlendDescRenderTargets);
            this.AdditiveBlendingBlendState = device.CreateBlendState(AdditiveBlendingBlendDesc);

            var SrcAlphaBlendingBlendDesc = D3D11BlendDesc.Default;
            AdditiveBlendingBlendDesc.IsAlphaToCoverageEnabled = false;
            var SrcAlphaBlendingBlendDescRenderTargets = SrcAlphaBlendingBlendDesc.GetRenderTargets();
            SrcAlphaBlendingBlendDescRenderTargets[0].IsBlendEnabled = true;
            SrcAlphaBlendingBlendDescRenderTargets[0].SourceBlend = D3D11BlendValue.SourceAlpha;
            SrcAlphaBlendingBlendDescRenderTargets[0].DestinationBlend = D3D11BlendValue.InverseSourceAlpha;
            SrcAlphaBlendingBlendDescRenderTargets[0].BlendOperation = D3D11BlendOperation.Add;
            SrcAlphaBlendingBlendDescRenderTargets[0].SourceBlendAlpha = D3D11BlendValue.Zero;
            SrcAlphaBlendingBlendDescRenderTargets[0].DestinationBlendAlpha = D3D11BlendValue.Zero;
            SrcAlphaBlendingBlendDescRenderTargets[0].BlendOperationAlpha = D3D11BlendOperation.Add;
            SrcAlphaBlendingBlendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
            SrcAlphaBlendingBlendDesc.SetRenderTargets(SrcAlphaBlendingBlendDescRenderTargets);
            this.SrcAlphaBlendingBlendState = device.CreateBlendState(SrcAlphaBlendingBlendDesc);
        }

        private void CreateTextures(OptFile opt)
        {
            var device = this.deviceResources.D3DDevice;

            foreach (var textureKey in opt.Textures)
            {
                var optTextureName = textureKey.Key;
                var optTexture = textureKey.Value;

                int mipLevels = optTexture.MipmapsCount;

                if (mipLevels == 0)
                {
                    continue;
                }

                D3D11SubResourceData[] textureSubResData = new D3D11SubResourceData[mipLevels];

                int bpp = optTexture.BitsPerPixel;

                if (bpp == 32)
                {
                    for (int level = 0; level < mipLevels; level++)
                    {
                        byte[] imageData = optTexture.GetMipmapImageData(level, out int width, out int height);

                        textureSubResData[level] = new D3D11SubResourceData(imageData, (uint)width * 4);
                    }
                }
                else
                {
                    continue;
                }

                D3D11Texture2DDesc textureDesc = new(DxgiFormat.B8G8R8A8UNorm, (uint)optTexture.Width, (uint)optTexture.Height, 1, (uint)textureSubResData.Length);

                using (var texture = device.CreateTexture2D(textureDesc, textureSubResData))
                {
                    D3D11ShaderResourceViewDesc textureViewDesc = new(D3D11SrvDimension.Texture2D, textureDesc.Format, 0, textureDesc.MipLevels);
                    D3D11ShaderResourceView textureView = device.CreateShaderResourceView(texture, textureViewDesc);

                    this.textureViews.Add(optTextureName, textureView);
                }
            }
        }

        private void CreateTextures2(OptFile opt)
        {
            var device = this.deviceResources.D3DDevice;

            foreach (var textureKey in opt.Textures)
            {
                var optTextureName = textureKey.Key;
                var optTexture = textureKey.Value;

                int mipLevels = optTexture.MipmapsCount;

                if (mipLevels == 0)
                {
                    continue;
                }

                D3D11SubResourceData[] textureSubResData = new D3D11SubResourceData[mipLevels];

                int bpp = optTexture.BitsPerPixel;

                if (bpp == 32)
                {
                    for (int level = 0; level < mipLevels; level++)
                    {
                        byte[] imageData = optTexture.GetMipmapImageData(level, out int width, out int height);
                        byte[] illumData = optTexture.GetMipmapIllumData(level, out _, out _);

                        int length = width * height;

                        for (int i = 0; i < length; i++)
                        {
                            imageData[i * 4 + 3] = illumData == null ? (byte)0 : illumData[i];
                        }

                        textureSubResData[level] = new D3D11SubResourceData(imageData, (uint)width * 4);
                    }
                }
                else
                {
                    continue;
                }

                D3D11Texture2DDesc textureDesc = new(DxgiFormat.B8G8R8A8UNorm, (uint)optTexture.Width, (uint)optTexture.Height, 1, (uint)textureSubResData.Length);

                using (var texture = device.CreateTexture2D(textureDesc, textureSubResData))
                {
                    D3D11ShaderResourceViewDesc textureViewDesc = new(D3D11SrvDimension.Texture2D, textureDesc.Format, 0, textureDesc.MipLevels);
                    D3D11ShaderResourceView textureView = device.CreateShaderResourceView(texture, textureViewDesc);

                    this.textureViews2.Add(optTextureName, textureView);
                }
            }
        }

        private void CreateTexturesNormalMaps(OptFile opt)
        {
            var device = this.deviceResources.D3DDevice;
            string optName = Path.GetFileNameWithoutExtension(opt.FileName);
            string optDirectory = Path.GetDirectoryName(opt.FileName);
            string rootDirectory = Path.GetFullPath(Path.Combine(optDirectory, ".."));

            string materialsDirectory = Path.GetFullPath(Path.Combine(optDirectory, "..", "Materials"));
            string materialFilename = Path.Combine(materialsDirectory, optName + ".mat");

            if (!File.Exists(materialFilename))
            {
                return;
            }

            foreach (var textureKey in opt.Textures)
            {
                string optTextureName = textureKey.Key;

                string textureSection = optTextureName;
                int textureFgIndex = textureSection.IndexOf("_fg_");

                if (textureFgIndex != -1)
                {
                    textureSection = textureSection[..textureFgIndex];
                }

                IList<string> materialLines = XwaHooksConfig.GetFileLines(materialFilename, textureSection);
                string normalMapEntry = XwaHooksConfig.GetFileKeyValue(materialLines, "NormalMap");
                string[] normalMapEntryParts = normalMapEntry.Split('-');

                if (normalMapEntryParts.Length != 3)
                {
                    continue;
                }

                string normalMapDatFilename = Path.Combine(rootDirectory, normalMapEntryParts[0]);

                if (!File.Exists(normalMapDatFilename))
                {
                    continue;
                }

                if (!int.TryParse(normalMapEntryParts[1], out int normalMapDatGroupId) || !int.TryParse(normalMapEntryParts[2], out int normalMapDatImageId))
                {
                    continue;
                }

                DatFile normalMapDat = DatFile.FromFile(normalMapDatFilename);
                DatGroup normalMapDatGroup = normalMapDat.Groups.FirstOrDefault(t => t.GroupId == normalMapDatGroupId);

                if (normalMapDatGroup is null)
                {
                    continue;
                }

                DatImage normalMapDatImage = normalMapDatGroup.Images.FirstOrDefault(t => t.GroupId == normalMapDatGroupId && t.ImageId == normalMapDatImageId);

                if (normalMapDatImage is null)
                {
                    continue;
                }

                normalMapDatImage.ConvertToFormat25();
                normalMapDatImage.FlipUpsideDown();

                D3D11SubResourceData[] textureSubResData = new D3D11SubResourceData[1];
                byte[] imageData = normalMapDatImage.GetImageData();
                textureSubResData[0] = new D3D11SubResourceData(imageData, (uint)normalMapDatImage.Width * 4);
                D3D11Texture2DDesc textureDesc = new(DxgiFormat.B8G8R8A8UNorm, (uint)normalMapDatImage.Width, (uint)normalMapDatImage.Height, 1, (uint)textureSubResData.Length);

                using (var texture = device.CreateTexture2D(textureDesc, textureSubResData))
                {
                    D3D11ShaderResourceViewDesc textureViewDesc = new(D3D11SrvDimension.Texture2D, textureDesc.Format, 0, textureDesc.MipLevels);
                    D3D11ShaderResourceView textureView = device.CreateShaderResourceView(texture, textureViewDesc);

                    this.textureViewsNormalMaps.Add(optTextureName, textureView);
                }
            }
        }

        private void CreateMeshes(OptFile opt)
        {
            var device = this.deviceResources.D3DDevice;

            var sceneMeshes = new List<SceneMesh>();

            foreach (var optMesh in opt.Meshes)
            {
                var positions = optMesh
                    .Vertices
                    .Select(t => new XMFloat3(t.X * OptFile.ScaleFactor, t.Z * OptFile.ScaleFactor, -t.Y * OptFile.ScaleFactor))
                    .ToList();

                var normals = optMesh
                    .VertexNormals
                    .Select(t => new XMFloat3(t.X, t.Z, -t.Y))
                    .ToList();

                var textureCoordinates = optMesh
                    .TextureCoordinates
                    .Select(t => new XMFloat2(t.U, -t.V))
                    .ToList();

                var optLod = optMesh.Lods.FirstOrDefault();

                if (optLod == null)
                {
                    continue;
                }

                foreach (var optFaceGroup in optLod.FaceGroups)
                {
                    var sceneMesh = new SceneMesh();
                    sceneMeshes.Add(sceneMesh);

                    var optTextureName = optFaceGroup.Textures[0];

                    sceneMesh.Texture = optTextureName;

                    opt.Textures.TryGetValue(optTextureName, out Texture texture);
                    sceneMesh.HasAlpha = texture is null ? false : texture.HasAlpha;

                    int index = 0;

                    foreach (var optFace in optFaceGroup.Faces)
                    {
                        Indices positionsIndex = optFace.VerticesIndex;
                        Indices normalsIndex = optFace.VertexNormalsIndex;
                        Indices textureCoordinatesIndex = optFace.TextureCoordinatesIndex;

                        D3dVertex vertex = new();

                        vertex.Position = positions.ElementAtOrDefault(positionsIndex.C);
                        vertex.Normal = normals.ElementAtOrDefault(normalsIndex.C);
                        vertex.TextureCoordinates = textureCoordinates.ElementAtOrDefault(textureCoordinatesIndex.C);
                        sceneMesh.Vertices.Add(vertex);
                        sceneMesh.Indices.Add(index);
                        index++;

                        vertex.Position = positions.ElementAtOrDefault(positionsIndex.B);
                        vertex.Normal = normals.ElementAtOrDefault(normalsIndex.B);
                        vertex.TextureCoordinates = textureCoordinates.ElementAtOrDefault(textureCoordinatesIndex.B);
                        sceneMesh.Vertices.Add(vertex);
                        sceneMesh.Indices.Add(index);
                        index++;

                        vertex.Position = positions.ElementAtOrDefault(positionsIndex.A);
                        vertex.Normal = normals.ElementAtOrDefault(normalsIndex.A);
                        vertex.TextureCoordinates = textureCoordinates.ElementAtOrDefault(textureCoordinatesIndex.A);
                        sceneMesh.Vertices.Add(vertex);
                        sceneMesh.Indices.Add(index);
                        index++;

                        if (positionsIndex.D >= 0)
                        {
                            sceneMesh.Indices.Add(index - 3);
                            sceneMesh.Indices.Add(index - 1);

                            vertex.Position = positions.ElementAtOrDefault(positionsIndex.D);
                            vertex.Normal = normals.ElementAtOrDefault(normalsIndex.D);
                            vertex.TextureCoordinates = textureCoordinates.ElementAtOrDefault(textureCoordinatesIndex.D);
                            sceneMesh.Vertices.Add(vertex);
                            sceneMesh.Indices.Add(index);
                            index++;
                        }
                    }
                }
            }

            foreach (var mesh3d in sceneMeshes)
            {
                var mesh = new D3dMesh(this.deviceResources, mesh3d);
                this.meshes.Add(mesh);
            }

            int indicesCount = sceneMeshes.Where(t => t.HasAlpha).Sum(t => t.Indices.Count);

            if (indicesCount != 0)
            {
                this.vertexBuffer = device.CreateBuffer(
                    new D3D11BufferDesc(D3dVertex.Size * (uint)indicesCount, D3D11BindOptions.VertexBuffer));

                this.indexBuffer = device.CreateBuffer(
                    new D3D11BufferDesc(4 * (uint)indicesCount, D3D11BindOptions.IndexBuffer));
            }
        }

        public void ReleaseDeviceDependentResources()
        {
            foreach (var textureKey in this.textureViews)
            {
                textureKey.Value.Dispose();
            }

            this.textureViews.Clear();

            foreach (var textureKey in this.textureViews2)
            {
                textureKey.Value.Dispose();
            }

            this.textureViews2.Clear();

            foreach (var textureKey in this.textureViewsNormalMaps)
            {
                textureKey.Value.Dispose();
            }

            this.textureViewsNormalMaps.Clear();

            foreach (var mesh in this.meshes)
            {
                mesh.ReleaseDeviceDependentResources();
            }

            this.meshes.Clear();

            D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
            D3D11Utils.DisposeAndNull(ref this.indexBuffer);

            D3D11Utils.DisposeAndNull(ref this.shaderVSMain);
            D3D11Utils.DisposeAndNull(ref this.shaderPSMain);
            D3D11Utils.DisposeAndNull(ref this.shaderPSAmbient);
            D3D11Utils.DisposeAndNull(ref this.shaderPSDepth);
            D3D11Utils.DisposeAndNull(ref this.shaderVSShadow);
            D3D11Utils.DisposeAndNull(ref this.shaderGSShadow);
            D3D11Utils.DisposeAndNull(ref this.shaderPSShadow);

            D3D11Utils.DisposeAndNull(ref this.inputLayout);

            D3D11Utils.DisposeAndNull(ref this.constantBuffer);

            D3D11Utils.DisposeAndNull(ref this.sampler);
            D3D11Utils.DisposeAndNull(ref this.DisableCullingRasterizerState);
            D3D11Utils.DisposeAndNull(ref this.EnableCullingRasterizerState);
            D3D11Utils.DisposeAndNull(ref this.rasterizerStateWireframe);
            D3D11Utils.DisposeAndNull(ref this.EnableDepthDepthStencilState);
            D3D11Utils.DisposeAndNull(ref this.DisableDepthWriteDepthStencilState);
            D3D11Utils.DisposeAndNull(ref this.TwoSidedStencilDepthStencilState);
            D3D11Utils.DisposeAndNull(ref this.RenderNonShadowsDepthStencilState);
            D3D11Utils.DisposeAndNull(ref this.RenderInShadowsDepthStencilState);
            D3D11Utils.DisposeAndNull(ref this.DefaultBlendState);
            D3D11Utils.DisposeAndNull(ref this.AlphaBlendingBlendState);
            D3D11Utils.DisposeAndNull(ref this.DisableFrameBufferBlendState);
            D3D11Utils.DisposeAndNull(ref this.NoBlendingBlendState);
            D3D11Utils.DisposeAndNull(ref this.AdditiveBlendingBlendState);
            D3D11Utils.DisposeAndNull(ref this.SrcAlphaBlendingBlendState);
        }

        public void CreateWindowSizeDependentResources()
        {
        }

        public void ReleaseWindowSizeDependentResources()
        {
        }

        public void Update(ITimer timer)
        {
            if (timer is null)
            {
                return;
            }

            if (!IsPaused)
            {
                Time += timer.ElapsedSeconds;
            }
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            float minDistance = -15.0f;
            float maxDistance = 15.0f;
            XMVector lightDirection = new(1.0f, 0.5f, -2.0f, 1.0f);
            XMMatrix worldMatrix = XMMatrix.Identity;
            float totalElapsed = (float)(Time % 29);

            if (totalElapsed < 1.0f)
            {
                float elapsed = totalElapsed;
                context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);
            }
            else if (totalElapsed < 5.0f)
            {
                float elapsed = totalElapsed - 1.0f;
                float cuttingDistance = (1.0f - elapsed / 4) * (maxDistance - minDistance) + minDistance;
                RenderScene(cuttingDistance, 100, true, lightDirection, worldMatrix);
            }
            else if (totalElapsed < 9.0f)
            {
                float elapsed = totalElapsed - 5.0f;
                float cuttingDistance = (1.0f - elapsed / 4) * (maxDistance - minDistance) + minDistance;
                RenderScene(-100, cuttingDistance, true, lightDirection, worldMatrix);
                RenderScene(cuttingDistance, 100, false, lightDirection, worldMatrix);
            }
            else if (totalElapsed < 15.0f)
            {
                float elapsed = totalElapsed - 9.0f;
                float duration = 6.0f;
                lightDirection = XMVector4.Transform(lightDirection, XMMatrix.RotationY(elapsed * XMMath.TwoPI / duration));
                RenderScene(-100, 100, false, lightDirection, worldMatrix);
            }
            else if (totalElapsed < 27.0f)
            {
                float elapsed = totalElapsed - 15.0f;
                float duration = 12.0f;
                lightDirection = XMVector4.Transform(lightDirection, XMMatrix.RotationY(elapsed * 2 * XMMath.TwoPI / duration));
                worldMatrix = XMMatrix.RotationY(-elapsed * XMMath.TwoPI / duration);
                RenderScene(-100, 100, false, lightDirection, worldMatrix);
            }
            else
            {
                float elapsed = totalElapsed - 27.0f;
                RenderScene(-100, 100, false, lightDirection, worldMatrix);
            }
        }

        private void RenderScene(float cuttingDistanceFrom, float cuttingDistanceTo, bool isWireframe, in XMVector lightDirection, in XMMatrix worldMatrix)
        {
            var context = this.deviceResources.D3DContext;
            bool showShadowVolumes = false;

            var constantBufferData = new D3dConstantBufferData
            {
                World = (worldMatrix * this.WorldTransform).Transpose(),
                View = this.ViewTransform.Transpose(),
                Projection = this.ProjectionTransform.Transpose(),
                LightDirection = XMVector4.Transform(lightDirection, this.LightTransform),
                CuttingDistanceFrom = cuttingDistanceFrom,
                CuttingDistanceTo = cuttingDistanceTo,
                IsWireframe = isWireframe ? 1 : 0,
                LightBrightness = this.LightBrightness
            };

            context.UpdateSubresource(this.constantBuffer, 0, null, constantBufferData, 0, 0);

            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            if (isWireframe)
            {
                this.RenderSceneSolid(
                    this.shaderVSMain,
                    null,
                    this.shaderPSMain,
                    this.DefaultBlendState,
                    this.EnableDepthDepthStencilState,
                    0,
                    this.rasterizerStateWireframe);

                this.RenderSceneTransparent(
                    this.shaderVSMain,
                    null,
                    this.shaderPSMain,
                    this.AlphaBlendingBlendState,
                    this.DisableDepthWriteDepthStencilState,
                    0,
                    this.rasterizerStateWireframe);

                return;
            }

            List<SceneMesh> sceneTransparentMeshes = this.BuildSceneTransparentMeshes();
            this.SortSceneMeshes(sceneTransparentMeshes, worldMatrix * this.WorldTransform * this.ViewTransform);
            this.GroupSceneMeshes(sceneTransparentMeshes);

            // Render the scene ambient - renders the scene with ambient lighting
            this.RenderSceneSolid(
                this.shaderVSMain,
                null,
                null, //this.shaderPSDepth,
                this.NoBlendingBlendState,
                this.EnableDepthDepthStencilState,
                0,
                this.DisableCullingRasterizerState);

            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Stencil, 1.0f, 0);

            // Render the shadow volume - extrudes shadows from geometry
            if (showShadowVolumes)
            {
                this.RenderSceneSolid(
                    this.shaderVSShadow,
                    this.shaderGSShadow,
                    this.shaderPSShadow,
                    this.SrcAlphaBlendingBlendState,
                    this.TwoSidedStencilDepthStencilState,
                    1,
                    this.DisableCullingRasterizerState);
            }
            else
            {
                this.RenderSceneSolid(
                    this.shaderVSShadow,
                    this.shaderGSShadow,
                    this.shaderPSShadow,
                    this.DisableFrameBufferBlendState,
                    this.TwoSidedStencilDepthStencilState,
                    1,
                    this.DisableCullingRasterizerState);
            }

            // Render the lit scene
            this.RenderSceneSolid(
                this.shaderVSMain,
                null,
                this.shaderPSAmbient,
                this.DefaultBlendState,
                this.RenderInShadowsDepthStencilState,
                0,
                this.DisableCullingRasterizerState);

            this.RenderSceneSolid(
                this.shaderVSMain,
                null,
                this.shaderPSMain,
                this.DefaultBlendState,
                this.RenderNonShadowsDepthStencilState,
                0,
                this.DisableCullingRasterizerState);

            this.RenderSceneMeshes(
                this.shaderVSMain,
                null,
                this.shaderPSMain,
                this.AlphaBlendingBlendState,
                this.DisableDepthWriteDepthStencilState,
                0,
                this.DisableCullingRasterizerState,
                sceneTransparentMeshes);
        }

        private void RenderSceneSolid(
                D3D11VertexShader vs,
                D3D11GeometryShader gs,
                D3D11PixelShader ps,
                D3D11BlendState blendState,
                D3D11DepthStencilState depthStencilState,
                uint stencilReference,
                D3D11RasterizerState rasterizerState)
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(vs, null);
            context.VertexShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.GeometryShaderSetShader(gs, null);
            context.GeometryShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.PixelShaderSetShader(ps, null);
            context.PixelShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.PixelShaderSetSamplers(0, new[] { this.sampler });
            context.OutputMergerSetBlendState(blendState, new float[] { 0.0f, 0.0f, 0.0f, 0.0f }, 0xFFFFFFFF);
            context.OutputMergerSetDepthStencilState(depthStencilState, stencilReference);
            context.RasterizerStageSetState(rasterizerState);

            context.InputAssemblerSetInputLayout(this.inputLayout);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            foreach (var mesh in this.meshes.Where(t => !t.HasAlpha))
            {
                context.InputAssemblerSetVertexBuffers(
                    0,
                    new[] { mesh.VertexBuffer },
                    new uint[] { D3dVertex.Size },
                    new uint[] { 0 });

                context.InputAssemblerSetIndexBuffer(mesh.IndexBuffer, DxgiFormat.R32UInt, 0);

                this.textureViews.TryGetValue(mesh.Texture, out D3D11ShaderResourceView texture);
                this.textureViews2.TryGetValue(mesh.Texture, out D3D11ShaderResourceView texture2);
                this.textureViewsNormalMaps.TryGetValue(mesh.Texture, out D3D11ShaderResourceView textureNormapMap);

                context.PixelShaderSetShaderResources(0, new[] { texture, texture2, textureNormapMap });

                context.DrawIndexed(mesh.IndicesCount, 0, 0);
            }
        }

        private void RenderSceneTransparent(
            D3D11VertexShader vs,
            D3D11GeometryShader gs,
            D3D11PixelShader ps,
            D3D11BlendState blendState,
            D3D11DepthStencilState depthStencilState,
            uint stencilReference,
            D3D11RasterizerState rasterizerState)
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(vs, null);
            context.VertexShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.GeometryShaderSetShader(gs, null);
            context.GeometryShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.PixelShaderSetShader(ps, null);
            context.PixelShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.PixelShaderSetSamplers(0, new[] { this.sampler });
            context.OutputMergerSetBlendState(blendState, new float[] { 0.0f, 0.0f, 0.0f, 0.0f }, 0xFFFFFFFF);
            context.OutputMergerSetDepthStencilState(depthStencilState, stencilReference);
            context.RasterizerStageSetState(rasterizerState);

            context.InputAssemblerSetInputLayout(this.inputLayout);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            foreach (var mesh in this.meshes.Where(t => t.HasAlpha))
            {
                context.InputAssemblerSetVertexBuffers(
                    0,
                    new[] { mesh.VertexBuffer },
                    new uint[] { D3dVertex.Size },
                    new uint[] { 0 });

                context.InputAssemblerSetIndexBuffer(mesh.IndexBuffer, DxgiFormat.R32UInt, 0);

                this.textureViews.TryGetValue(mesh.Texture, out D3D11ShaderResourceView texture);
                this.textureViews2.TryGetValue(mesh.Texture, out D3D11ShaderResourceView texture2);
                this.textureViewsNormalMaps.TryGetValue(mesh.Texture, out D3D11ShaderResourceView textureNormapMap);

                context.PixelShaderSetShaderResources(0, new[] { texture, texture2, textureNormapMap });

                context.DrawIndexed(mesh.IndicesCount, 0, 0);
            }
        }

        private void RenderSceneMeshes(
            D3D11VertexShader vs,
            D3D11GeometryShader gs,
            D3D11PixelShader ps,
            D3D11BlendState blendState,
            D3D11DepthStencilState depthStencilState,
            uint stencilReference,
            D3D11RasterizerState rasterizerState,
            List<SceneMesh> sceneMeshes)
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(vs, null);
            context.VertexShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.GeometryShaderSetShader(gs, null);
            context.GeometryShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.PixelShaderSetShader(ps, null);
            context.PixelShaderSetConstantBuffers(0, new[] { this.constantBuffer });
            context.PixelShaderSetSamplers(0, new[] { this.sampler });
            context.OutputMergerSetBlendState(blendState, new float[] { 0.0f, 0.0f, 0.0f, 0.0f }, 0xFFFFFFFF);
            context.OutputMergerSetDepthStencilState(depthStencilState, stencilReference);
            context.RasterizerStageSetState(rasterizerState);

            context.InputAssemblerSetInputLayout(this.inputLayout);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            context.InputAssemblerSetVertexBuffers(
                0,
                new[] { this.vertexBuffer },
                new uint[] { D3dVertex.Size },
                new uint[] { 0 });

            context.InputAssemblerSetIndexBuffer(this.indexBuffer, DxgiFormat.R32UInt, 0);

            foreach (var sceneMesh in sceneMeshes)
            {
                var vertices = sceneMesh.Vertices.ToArray();
                var indices = sceneMesh.Indices.ToArray();

                var vertexBox = new D3D11Box(0, 0, 0, D3dVertex.Size * (uint)vertices.Length, 1, 1);
                context.UpdateSubresource(this.vertexBuffer, 0, vertexBox, vertices, 0, 0);
                var indexBox = new D3D11Box(0, 0, 0, 4 * (uint)indices.Length, 1, 1);
                context.UpdateSubresource(this.indexBuffer, 0, indexBox, indices, 0, 0);

                this.textureViews.TryGetValue(sceneMesh.Texture, out D3D11ShaderResourceView texture);
                this.textureViews2.TryGetValue(sceneMesh.Texture, out D3D11ShaderResourceView texture2);
                this.textureViewsNormalMaps.TryGetValue(sceneMesh.Texture, out D3D11ShaderResourceView textureNormapMap);

                context.PixelShaderSetShaderResources(0, new[] { texture, texture2, textureNormapMap });

                context.DrawIndexed((uint)sceneMesh.Indices.Count, 0, 0);
            }
        }

        private List<SceneMesh> BuildSceneTransparentMeshes()
        {
            var sceneMeshes = new List<SceneMesh>();

            foreach (var mesh in this.meshes.Where(t => t.HasAlpha))
            {
                for (int i = 0; i < mesh.Mesh.Indices.Count; i += 3)
                {
                    var sceneMesh = new SceneMesh
                    {
                        HasAlpha = true,
                        Texture = mesh.Texture,
                    };

                    sceneMesh.Vertices.Add(mesh.Mesh.Vertices[mesh.Mesh.Indices[i]]);
                    sceneMesh.Indices.Add(0);

                    sceneMesh.Vertices.Add(mesh.Mesh.Vertices[mesh.Mesh.Indices[i + 1]]);
                    sceneMesh.Indices.Add(1);

                    sceneMesh.Vertices.Add(mesh.Mesh.Vertices[mesh.Mesh.Indices[i + 2]]);
                    sceneMesh.Indices.Add(2);

                    sceneMeshes.Add(sceneMesh);
                }
            }

            return sceneMeshes;
        }

        private void SortSceneMeshes(List<SceneMesh> sceneMeshes, in XMMatrix m)
        {
            foreach (var mesh in sceneMeshes)
            {
                mesh.ComputeDepth(m);
            }

            Quicksort.Sort(sceneMeshes);
        }

        private void GroupSceneMeshes(List<SceneMesh> sceneMeshes)
        {
            var newSceneMeshes = new List<SceneMesh>();

            SceneMesh currentMesh = null;

            for (int meshIndex = 0; meshIndex < sceneMeshes.Count; meshIndex++)
            {
                var mesh = sceneMeshes[meshIndex];

                if (currentMesh is null)
                {
                    currentMesh = mesh;
                    newSceneMeshes.Add(currentMesh);
                    continue;
                }

                if (mesh.Texture != currentMesh.Texture)
                {
                    currentMesh = null;
                    meshIndex--;
                    continue;
                }

                currentMesh.Vertices.AddRange(mesh.Vertices);

                int baseIndex = currentMesh.Indices.Count;

                for (int i = 0; i < mesh.Indices.Count; i++)
                {
                    currentMesh.Indices.Add(baseIndex + mesh.Indices[i]);
                }
            }

            sceneMeshes.Clear();
            sceneMeshes.AddRange(newSceneMeshes);
        }
    }
}
