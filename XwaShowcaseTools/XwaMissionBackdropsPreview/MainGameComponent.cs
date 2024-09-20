using JeremyAnsel.DirectX.D2D1;
using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DWrite;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.Xwa.Dat;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XwaMissionBackdropsPreview;

internal class MainGameComponent : IGameComponent
{
    private DeviceResources deviceResources;

    private D2D1DrawingStateBlock stateBlock;

    private DWriteTextFormat textFormat;

    private D2D1SolidColorBrush textBrush;

    private D3D11InputLayout inputLayout;

    private D3D11VertexShader vertexShader;

    private D3D11PixelShader pixelShader;

    private D3D11SamplerState sampler;

    private D3D11BlendState blendState;

    private D3D11DepthStencilState depthStencil;

    private D3D11RasterizerState rasterizerStateWireframe;

    private D3D11Buffer constantBuffer;

    private readonly List<BackdropModel> backdrops = new();

    private ConstantBufferData constantBufferData;

    private MissionModel missionModel;

    public MainGameComponent(string workingDirectory, string missionFileName, int missionRegion)
    {
        WorkingDirectory = workingDirectory;
        MissionFileName = missionFileName;
        MissionRegion = missionRegion;
    }

    public bool CreateRenderBackdrops { get; private set; } = true;

    public string WorkingDirectory { get; private set; }

    public string MissionFileName { get; private set; }

    public int MissionRegion { get; private set; }

    public XMMatrix WorldMatrix { get; set; }

    public XMMatrix ViewMatrix { get; set; }

    public XMMatrix ProjectionMatrix { get; set; }

    public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel91;

    public void CreateDeviceDependentResources(DeviceResources resources)
    {
        this.deviceResources = resources;

        this.stateBlock = this.deviceResources.D2DFactory.CreateDrawingStateBlock();
        this.textFormat = this.deviceResources.DWriteFactory.CreateTextFormat("Verdana", null, DWriteFontWeight.Light, DWriteFontStyle.Normal, DWriteFontStretch.Normal, 30, string.Empty);
        this.textFormat.TextAlignment = DWriteTextAlignment.Center;
        this.textFormat.ParagraphAlignment = DWriteParagraphAlignment.Far;

        if (!Directory.Exists(WorkingDirectory) || !File.Exists(MissionFileName))
        {
            return;
        }

        AppSettings.WorkingDirectory = WorkingDirectory;

        var loader = new BasicLoader(this.deviceResources.D3DDevice);

        loader.LoadShader("XwaMissionBackdropsPreview_Shaders\\VertexShader.cso", null, out this.vertexShader, out this.inputLayout);

        var constantBufferDesc = new D3D11BufferDesc(ConstantBufferData.Size, D3D11BindOptions.ConstantBuffer);
        this.constantBuffer = this.deviceResources.D3DDevice.CreateBuffer(constantBufferDesc);

        loader.LoadShader("XwaMissionBackdropsPreview_Shaders\\PixelShader.cso", out this.pixelShader);

        D3D11SamplerDesc samplerDesc = new(
            D3D11Filter.Anisotropic,
            D3D11TextureAddressMode.Wrap,
            D3D11TextureAddressMode.Wrap,
            D3D11TextureAddressMode.Wrap,
            0.0f,
            this.deviceResources.D3DFeatureLevel > D3D11FeatureLevel.FeatureLevel91 ? D3D11Constants.DefaultMaxAnisotropy : D3D11Constants.FeatureLevel91DefaultMaxAnisotropy,
            D3D11ComparisonFunction.Never,
            new float[] { 0.0f, 0.0f, 0.0f, 0.0f },
            0.0f,
            float.MaxValue);

        this.sampler = this.deviceResources.D3DDevice.CreateSamplerState(samplerDesc);

        D3D11BlendDesc blendDesc = D3D11BlendDesc.Default;
        var blendDescRenderTargets = blendDesc.GetRenderTargets();
        blendDescRenderTargets[0].IsBlendEnabled = true;
        blendDescRenderTargets[0].SourceBlend = D3D11BlendValue.SourceAlpha;
        blendDescRenderTargets[0].DestinationBlend = D3D11BlendValue.InverseSourceAlpha;
        blendDescRenderTargets[0].BlendOperation = D3D11BlendOperation.Add;
        blendDescRenderTargets[0].SourceBlendAlpha = D3D11BlendValue.One;
        blendDescRenderTargets[0].DestinationBlendAlpha = D3D11BlendValue.Zero;
        blendDescRenderTargets[0].BlendOperationAlpha = D3D11BlendOperation.Add;
        blendDescRenderTargets[0].RenderTargetWriteMask = D3D11ColorWriteEnables.All;
        blendDesc.SetRenderTargets(blendDescRenderTargets);
        this.blendState = this.deviceResources.D3DDevice.CreateBlendState(blendDesc);

        D3D11DepthStencilDesc depthStencilDesc = D3D11DepthStencilDesc.Default;
        depthStencilDesc.IsDepthEnabled = false;
        depthStencilDesc.DepthWriteMask = D3D11DepthWriteMask.Zero;
        this.depthStencil = this.deviceResources.D3DDevice.CreateDepthStencilState(depthStencilDesc);

        this.rasterizerStateWireframe = this.deviceResources.D3DDevice.CreateRasterizerState(D3D11RasterizerDesc.Default with
        {
            CullMode = D3D11CullMode.Back,
            //FillMode = D3D11FillMode.WireFrame
        });

        this.missionModel = new MissionModel(MissionFileName, MissionRegion, CreateRenderBackdrops, CreateBackdrop);

        this.ViewMatrix = XMMatrix.LookAtLH(SceneConstants.VecEye, SceneConstants.VecAt, SceneConstants.VecUp);
        this.WorldMatrix = XMMatrix.Identity;
    }

    public void ReleaseDeviceDependentResources()
    {
        this.missionModel = null;

        foreach (var backdrop in this.backdrops)
        {
            backdrop.Release();
        }

        this.backdrops.Clear();

        D3D11Utils.DisposeAndNull(ref this.vertexShader);
        D3D11Utils.DisposeAndNull(ref this.inputLayout);
        D3D11Utils.DisposeAndNull(ref this.constantBuffer);
        D3D11Utils.DisposeAndNull(ref this.pixelShader);
        D3D11Utils.DisposeAndNull(ref this.sampler);
        D3D11Utils.DisposeAndNull(ref this.blendState);
        D3D11Utils.DisposeAndNull(ref this.depthStencil);
        D3D11Utils.DisposeAndNull(ref this.rasterizerStateWireframe);
        D2D1Utils.DisposeAndNull(ref this.stateBlock);
        DWriteUtils.DisposeAndNull(ref this.textFormat);
    }

    private void CreateBackdrop(DatImage planetImage, BackdropEntry backdrop, bool isWrap)
    {
        var model = new BackdropModel(this.deviceResources, planetImage, backdrop, isWrap);
        this.backdrops.Add(model);
    }

    public void CreateWindowSizeDependentResources()
    {
        this.textBrush = this.deviceResources.D2DRenderTarget.CreateSolidColorBrush(new D2D1ColorF(D2D1KnownColor.White));

        float fAspectRatio = (float)this.deviceResources.BackBufferWidth / this.deviceResources.BackBufferHeight;
        this.ProjectionMatrix = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, SceneConstants.ProjectionNearPlane, SceneConstants.ProjectionFarPlane);
    }

    public void ReleaseWindowSizeDependentResources()
    {
        D2D1Utils.DisposeAndNull(ref this.textBrush);
    }

    public void Update(ITimer timer)
    {
    }

    public void Render()
    {
        var context = this.deviceResources.D3DContext;

        context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
        context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, XMKnownColor.Black);
        context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

        context.InputAssemblerSetInputLayout(this.inputLayout);
        context.RasterizerStageSetState(this.rasterizerStateWireframe);

        this.constantBufferData.View = this.ViewMatrix.Transpose();
        this.constantBufferData.Projection = this.ProjectionMatrix.Transpose();

        foreach (var backdrop in this.backdrops)
        {
            if (backdrop.vertexBuffer is null)
            {
                continue;
            }

            this.constantBufferData.World = (
                backdrop.GetTransformMatrix()
                * this.WorldMatrix
                * XMMatrix.Scaling(SceneConstants.WorldScale, SceneConstants.WorldScale, SceneConstants.WorldScale)
                ).Transpose();

            this.deviceResources.D3DContext.UpdateSubresource(this.constantBuffer, 0, null, this.constantBufferData, 0, 0);

            context.InputAssemblerSetVertexBuffers(
                0,
                new[] { backdrop.vertexBuffer },
                new uint[] { BasicVertex.Size },
                new uint[] { 0 });

            context.InputAssemblerSetIndexBuffer(backdrop.indexBuffer, DxgiFormat.R16UInt, 0);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            context.VertexShaderSetShader(this.vertexShader, null);
            context.VertexShaderSetConstantBuffers(0, new[] { this.constantBuffer });

            context.PixelShaderSetShader(this.pixelShader, null);
            context.PixelShaderSetShaderResources(0, new[] { backdrop.textureView });
            context.PixelShaderSetSamplers(0, new[] { this.sampler });

            context.OutputMergerSetBlendState(this.blendState, new float[] { 0.0f, 0.0f, 0.0f, 0.0f }, 0xFFFFFFFF);
            context.OutputMergerSetDepthStencilState(this.depthStencil, 0);

            context.DrawIndexed((uint)backdrop.indexCount, 0, 0);
        }

        var d2dContext = this.deviceResources.D2DRenderTarget;

        d2dContext.SaveDrawingState(this.stateBlock);
        d2dContext.BeginDraw();

        var textRect = new D2D1RectF(
            0,
            0,
            this.deviceResources.ConvertPixelsToDipsX(this.deviceResources.BackBufferWidth),
            this.deviceResources.ConvertPixelsToDipsX(this.deviceResources.BackBufferHeight));


        if (!string.IsNullOrEmpty(MissionFileName))
        {
            var sb = new StringBuilder();

            string name = Path.GetFileNameWithoutExtension(MissionFileName);
            sb.AppendLine(name);

            sb.AppendLine($"Region {MissionRegion + 1}");

            d2dContext.DrawText(sb.ToString(), this.textFormat, textRect, this.textBrush);
        }

        d2dContext.EndDrawIgnoringRecreateTargetError();
        d2dContext.RestoreDrawingState(this.stateBlock);
    }
}
