using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.GameWindow;
using System.IO;

namespace XwaSizeComparison
{
    class MainResources
    {
        private DeviceResources deviceResources;

        public D3D11VertexShader ShaderVSMain;
        public D3D11PixelShader ShaderPSMain;
        public D3D11PixelShader ShaderPSAmbient;
        public D3D11PixelShader ShaderPSDepth;
        public D3D11VertexShader ShaderVSShadow;
        public D3D11GeometryShader ShaderGSShadow;
        public D3D11PixelShader ShaderPSShadow;

        public D3D11InputLayout InputLayout;

        public D3D11Buffer ConstantBufferGlobal;

        public D3D11SamplerState Sampler;

        public D3D11RasterizerState DisableCullingRasterizerState;
        public D3D11RasterizerState EnableCullingRasterizerState;
        public D3D11RasterizerState RasterizerStateWireframe;

        public D3D11DepthStencilState EnableDepthDepthStencilState;
        public D3D11DepthStencilState DisableDepthWriteDepthStencilState;
        public D3D11DepthStencilState TwoSidedStencilDepthStencilState;
        public D3D11DepthStencilState RenderNonShadowsDepthStencilState;
        public D3D11DepthStencilState RenderInShadowsDepthStencilState;

        public D3D11BlendState DefaultBlendState;
        public D3D11BlendState AlphaBlendingBlendState;
        public D3D11BlendState DisableFrameBufferBlendState;
        public D3D11BlendState NoBlendingBlendState;
        public D3D11BlendState AdditiveBlendingBlendState;
        public D3D11BlendState SrcAlphaBlendingBlendState;

        public MainResources()
        {
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var device = this.deviceResources.D3DDevice;

            this.ShaderVSMain = device.CreateVertexShader(File.ReadAllBytes("XwaSizeComparison_Shaders\\SceneVSMain.cso"), null);
            this.ShaderPSMain = device.CreatePixelShader(File.ReadAllBytes("XwaSizeComparison_Shaders\\ScenePSMain.cso"), null);
            this.ShaderPSAmbient = device.CreatePixelShader(File.ReadAllBytes("XwaSizeComparison_Shaders\\ScenePSAmbient.cso"), null);
            this.ShaderPSDepth = device.CreatePixelShader(File.ReadAllBytes("XwaSizeComparison_Shaders\\ScenePSDepth.cso"), null);
            this.ShaderVSShadow = device.CreateVertexShader(File.ReadAllBytes("XwaSizeComparison_Shaders\\SceneVSShadow.cso"), null);
            this.ShaderGSShadow = device.CreateGeometryShader(File.ReadAllBytes("XwaSizeComparison_Shaders\\SceneGSShadow.cso"), null);
            this.ShaderPSShadow = device.CreatePixelShader(File.ReadAllBytes("XwaSizeComparison_Shaders\\ScenePSShadow.cso"), null);

            D3D11InputElementDesc[] basicVertexLayoutDesc = new[]
            {
                new D3D11InputElementDesc("POSITION", 0, DxgiFormat.R32G32B32A32UInt, 0, 0, D3D11InputClassification.PerVertexData, 0),
            };

            this.InputLayout = device.CreateInputLayout(basicVertexLayoutDesc, File.ReadAllBytes("XwaSizeComparison_Shaders\\SceneVSMain.cso"));

            this.ConstantBufferGlobal = device.CreateBuffer(new D3D11BufferDesc(D3dConstantBufferGlobalData.Size, D3D11BindOptions.ConstantBuffer));

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

            this.Sampler = device.CreateSamplerState(samplerDesc);

            this.DisableCullingRasterizerState = device.CreateRasterizerState(D3D11RasterizerDesc.Default with
            {
                CullMode = D3D11CullMode.None
            });

            this.EnableCullingRasterizerState = device.CreateRasterizerState(D3D11RasterizerDesc.Default with
            {
                CullMode = D3D11CullMode.Back
            });

            this.RasterizerStateWireframe = device.CreateRasterizerState(D3D11RasterizerDesc.Default with
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

        public void ReleaseDeviceDependentResources()
        {
            D3D11Utils.DisposeAndNull(ref this.ShaderVSMain);
            D3D11Utils.DisposeAndNull(ref this.ShaderPSMain);
            D3D11Utils.DisposeAndNull(ref this.ShaderPSAmbient);
            D3D11Utils.DisposeAndNull(ref this.ShaderPSDepth);
            D3D11Utils.DisposeAndNull(ref this.ShaderVSShadow);
            D3D11Utils.DisposeAndNull(ref this.ShaderGSShadow);
            D3D11Utils.DisposeAndNull(ref this.ShaderPSShadow);

            D3D11Utils.DisposeAndNull(ref this.InputLayout);

            D3D11Utils.DisposeAndNull(ref this.ConstantBufferGlobal);

            D3D11Utils.DisposeAndNull(ref this.Sampler);
            D3D11Utils.DisposeAndNull(ref this.DisableCullingRasterizerState);
            D3D11Utils.DisposeAndNull(ref this.EnableCullingRasterizerState);
            D3D11Utils.DisposeAndNull(ref this.RasterizerStateWireframe);
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
    }
}
