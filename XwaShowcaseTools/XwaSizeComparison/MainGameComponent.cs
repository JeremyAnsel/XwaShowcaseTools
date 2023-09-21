using JeremyAnsel.DirectX.D2D1;
using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DWrite;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.IO;

namespace XwaSizeComparison
{
    class MainGameComponent : IGameComponent
    {
        private const float initTime = 3;
        private const float modelTimeLength = 5;

        public float AnimationTotalTime
        {
            get
            {
                float totalTimeLength = modelTimeLength * Math.Max(1, this.SceneOpts.Count);

                return initTime + totalTimeLength;
            }
        }

        private DeviceResources deviceResources;

        private readonly MainResources mainResources = new();

        private readonly GroundResources groundResources = new();

        private readonly OptResources optResources = new();

        private D2D1DrawingStateBlock stateBlock;

        private DWriteTextFormat textFormat;
        private D2D1SolidColorBrush textBrush;

        public MainGameComponent()
        {
            this.ViewTransform = XMMatrix.LookAtLH(SceneConstants.VecEye, SceneConstants.VecAt, SceneConstants.VecUp);
            this.CameraAngle = 0;
            this.CameraElevation = 0;
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel100;

        public XMMatrix ViewTransform { get; set; }

        public int CameraAngle { get; set; }

        public int CameraElevation { get; set; }

        public XMMatrix ProjectionTransform { get; set; }

        public List<SceneOpt> SceneOpts { get; } = new();

        public string SceneFileName { get; set; } = SceneConstants.SceneFileName;

        public float Time { get; set; }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            this.mainResources.CreateDeviceDependentResources(resources);
            this.groundResources.CreateDeviceDependentResources(resources);

            this.LoadScene();

            this.Time = 0;
        }

        public void ReleaseDeviceDependentResources()
        {
            this.ReleaseScene();

            this.mainResources.ReleaseDeviceDependentResources();
            this.groundResources.ReleaseDeviceDependentResources();
        }

        public void LoadScene()
        {
            this.Time = 0;

            if (!File.Exists(this.SceneFileName))
            {
                return;
            }

            Scene scene = Scene.FromFile(this.SceneFileName);

            if (!Directory.Exists(scene.OptDirectory))
            {
                return;
            }

            scene.FillSize();
            scene.OrderBySize(scene.OrderScene);

            float position = 0;
            var sceneData = new SceneData();

            foreach (var sceneOpt in scene.SceneOpts)
            {
                float scale = 1.5f;
                position -= sceneOpt.OptSpanSize.X / 2 * scale;
                sceneOpt.WorldTransform = XMMatrix.Translation(position, 0, 0);
                position -= sceneOpt.OptSpanSize.X / 2 * scale;

                this.SceneOpts.Add(sceneOpt);
                sceneData.AddOpt(sceneOpt);
            }

            this.optResources.CreateDeviceDependentResources(this.deviceResources, sceneData);

            this.stateBlock = this.deviceResources.D2DFactory.CreateDrawingStateBlock();
            this.textFormat = this.deviceResources.DWriteFactory.CreateTextFormat(scene.TextFontName, null, DWriteFontWeight.Light, DWriteFontStyle.Normal, DWriteFontStretch.Normal, scene.TextFontSize, string.Empty);
            this.textFormat.TextAlignment = DWriteTextAlignment.Center;
            this.textFormat.ParagraphAlignment = DWriteParagraphAlignment.Far;

            this.CameraAngle = scene.CameraAngle;
            this.CameraElevation = scene.CameraElevation;
        }

        public void ReleaseScene()
        {
            D2D1Utils.DisposeAndNull(ref this.stateBlock);
            DWriteUtils.DisposeAndNull(ref this.textFormat);

            this.optResources.ReleaseDeviceDependentResources();
            this.SceneOpts.Clear();
        }

        public void CreateWindowSizeDependentResources()
        {
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            this.ProjectionTransform = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, SceneConstants.ProjectionNearPlane, SceneConstants.ProjectionFarPlane);

            this.textBrush = this.deviceResources.D2DRenderTarget.CreateSolidColorBrush(new D2D1ColorF(D2D1KnownColor.White));
        }

        public void ReleaseWindowSizeDependentResources()
        {
            D2D1Utils.DisposeAndNull(ref this.textBrush);
        }

        public void Update(ITimer timer)
        {
            if (timer is null)
            {
                return;
            }

            this.Time += (float)timer.ElapsedSeconds;
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, SceneConstants.BackgroundColor);
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            if (this.SceneOpts.Count == 0)
            {
                return;
            }

            float totalTimeLength = modelTimeLength * Math.Max(1, this.SceneOpts.Count);
            float currentTime = this.Time < initTime ? 0 : (this.Time - initTime) % totalTimeLength;
            int currentIndex = (int)(currentTime / modelTimeLength);
            float t = (currentTime - currentIndex * modelTimeLength) / modelTimeLength;

            SceneOpt opt0 = this.SceneOpts[currentIndex];
            SceneOpt opt1 = this.SceneOpts[Math.Min(currentIndex + 1, this.SceneOpts.Count - 1)];

            this.RenderScene(opt0, opt1, t);
            this.RenderTitle(opt0, opt1, t);
        }

        private void RenderScene(SceneOpt opt0, SceneOpt opt1, float t)
        {
            XMMatrix m0 = opt0.WorldTransform;
            XMMatrix m1 = opt1.WorldTransform;

            m0.Decompose(out XMVector s0, out XMVector r0, out XMVector t0);
            m1.Decompose(out XMVector s1, out XMVector r1, out XMVector t1);

            XMMatrix world = XMMatrix.TranslationFromVector(XMVector.Lerp(-t0, -t1, t));

            float optSize = opt0.OptSize * (1 - t) + opt1.OptSize * t;
            float optSizeDelta1 = 30.0f;
            float optSizeScale1 = 3.0f;
            float optSizeScale2 = 1.0f;
            optSize = Math.Min(optSize, optSizeDelta1) * optSizeScale1 + Math.Max(0, optSize - optSizeDelta1) * optSizeScale2;

            float optSpanSizeZ = opt0.OptSpanSize.Z * (1 - t) + opt1.OptSpanSize.Z * t;
            float optSpanSizeY = opt0.OptSpanSize.Y * (1 - t) + opt1.OptSpanSize.Y * t;

            //XMVector vecEye = new(0.0f, optSpanSizeZ * 0.8f, optSize * 0.6f, 0.0f);
            XMVector vecEye = new(0.0f, optSpanSizeZ * 0.5f, optSize * 0.35f, 0.0f);
            //XMVector vecAt = new(0.0f, optSpanSizeZ * 0.7f, -optSpanSizeY / 2, 0.0f);
            XMVector vecAt = new(0.0f, optSpanSizeZ * 0.5f, -optSpanSizeY / 2, 0.0f);
            XMVector vecUp = new(0.0f, 1.0f, 0.0f, 0.0f);
            XMMatrix viewTransform = XMMatrix.LookAtLH(vecEye, vecAt, vecUp);

            viewTransform = XMMatrix.RotationX(XMMath.ConvertToRadians(this.CameraElevation)) * viewTransform;
            viewTransform = XMMatrix.RotationY(XMMath.ConvertToRadians(this.CameraAngle)) * viewTransform;

            float farPlane = SceneConstants.ProjectionFarPlane;
            float nearPlane = SceneConstants.ProjectionNearPlane;

            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            XMMatrix projectionTransform = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, nearPlane, farPlane);

            var constantBufferData = new D3dConstantBufferGlobalData
            {
                World = world.Transpose(),
                View = viewTransform.Transpose(),
                Projection = projectionTransform.Transpose(),
                LightDirection = XMVector3.Normalize(SceneConstants.LightDirection),
                LightBrightness = SceneConstants.LightBrightness
            };

            var optRenderer = new OptRenderer(this.deviceResources, this.mainResources, this.groundResources, this.optResources);
            optRenderer.Render(constantBufferData);
        }

        private void RenderTitle(SceneOpt opt0, SceneOpt opt1, float t)
        {
            var d2dContext = this.deviceResources.D2DRenderTarget;

            d2dContext.SaveDrawingState(this.stateBlock);
            d2dContext.BeginDraw();

            var textRect = new D2D1RectF(
                0,
                0,
                this.deviceResources.ConvertPixelsToDipsX(this.deviceResources.BackBufferWidth),
                this.deviceResources.ConvertPixelsToDipsX(this.deviceResources.BackBufferHeight));

            SceneOpt opt = t < 0.4f ? opt0 : t > 0.6 ? opt1 : null;

            if (opt is not null)
            {
                string title = opt.Title;

                if (!string.IsNullOrEmpty(title))
                {
                    title += "\n" + opt.OptSize.ToString("F0") + " m";
                }

                d2dContext.DrawText(title, this.textFormat, textRect, this.textBrush);
            }

            d2dContext.EndDrawIgnoringRecreateTargetError();
            d2dContext.RestoreDrawingState(this.stateBlock);
        }
    }
}
