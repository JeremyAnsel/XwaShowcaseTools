using JeremyAnsel.DirectX.D2D1;
using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System.Drawing.Imaging;
using System.Drawing;
using System;
using System.IO;
using JeremyAnsel.DirectX.Dxgi;
using System.Collections.Generic;

namespace XwaOptShowcase
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private D2D1Bitmap backgroundBitmap;

        private OptComponent optComponent;

        public MainGameComponent()
        {
            this.LightTransform = XMMatrix.Identity;
            this.LightBrightness = 1.0f;
            this.WorldTransform = XMMatrix.Identity;
            this.ViewTransform = XMMatrix.LookAtLH(SceneConstants.VecEye, SceneConstants.VecAt, SceneConstants.VecUp);
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel100;

        public string BackgroundBitmapFileName { get; set; }

        public string OptFileName { get; set; }

        public int OptVersion { get; set; }

        public string OptObjectProfile { get; set; }

        public List<string> OptObjectSkins { get; } = new();

        public XMMatrix LightTransform { get; set; }

        public float LightBrightness { get; set; }

        public XMMatrix WorldTransform { get; set; }

        public XMMatrix ViewTransform { get; set; }

        public XMMatrix ProjectionTransform { get; set; }

        public bool IsPaused { get; set; }

        public double Time
        {
            get
            {
                return this.optComponent?.Time ?? 0.0;
            }

            set
            {
                if (this.optComponent is not null)
                {
                    this.optComponent.Time = value;
                }
            }
        }

        public void ReloadBackground()
        {
            if (this.deviceResources is null)
            {
                return;
            }

            if (this.backgroundBitmap is not null)
            {
                D2D1Utils.DisposeAndNull(ref this.backgroundBitmap);
            }

            if (!File.Exists(BackgroundBitmapFileName))
            {
                return;
            }

            var d2d1RenderTarget = this.deviceResources.D2DRenderTarget;

            string ext = Path.GetExtension(BackgroundBitmapFileName).ToUpperInvariant();

            switch (ext)
            {
                case ".BMP":
                case ".PNG":
                case ".JPG":
                case ".GIF":
                    {
                        using var file = new Bitmap(BackgroundBitmapFileName);
                        var rect = new Rectangle(0, 0, file.Width, file.Height);
                        int length = file.Width * file.Height;
                        using var bitmap = file.Clone(rect, PixelFormat.Format32bppArgb);
                        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

                        try
                        {
                            this.backgroundBitmap = d2d1RenderTarget.CreateBitmap(
                                new D2D1SizeU((uint)data.Width, (uint)data.Height),
                                data.Scan0,
                                (uint)data.Stride,
                                new D2D1BitmapProperties(
                                    new D2D1PixelFormat(DxgiFormat.B8G8R8A8UNorm, D2D1AlphaMode.Ignore),
                                    96.0f,
                                    96.0f));
                        }
                        finally
                        {
                            bitmap.UnlockBits(data);
                        }
                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(BackgroundBitmapFileName));
            }
        }

        public void ReloadOpt()
        {
            if (this.deviceResources is null)
            {
                return;
            }

            if (this.optComponent is not null)
            {
                this.optComponent.ReleaseWindowSizeDependentResources();
                this.optComponent.ReleaseDeviceDependentResources();
                this.optComponent = null;
            }

            if (File.Exists(OptFileName))
            {
                this.optComponent = new OptComponent(OptFileName, OptVersion, OptObjectProfile, OptObjectSkins);
                this.optComponent.CreateDeviceDependentResources(this.deviceResources);
                this.optComponent.CreateWindowSizeDependentResources();
                this.IsPaused = false;
            }
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            if (File.Exists(OptFileName))
            {
                this.optComponent = new OptComponent(OptFileName, OptVersion, OptObjectProfile, OptObjectSkins);
                this.optComponent.CreateDeviceDependentResources(this.deviceResources);
            }
        }

        public void ReleaseDeviceDependentResources()
        {
            this.optComponent?.ReleaseDeviceDependentResources();
        }

        public void CreateWindowSizeDependentResources()
        {
            float fAspectRatio = (float)this.deviceResources.BackBufferWidth / (float)this.deviceResources.BackBufferHeight;
            this.ProjectionTransform = XMMatrix.PerspectiveFovLH(XMMath.PIDivFour, fAspectRatio, SceneConstants.ProjectionNearPlane, SceneConstants.ProjectionFarPlane);

            this.ReloadBackground();

            this.optComponent?.CreateWindowSizeDependentResources();
        }

        public void ReleaseWindowSizeDependentResources()
        {
            this.optComponent?.ReleaseWindowSizeDependentResources();
            D2D1Utils.DisposeAndNull(ref this.backgroundBitmap);
        }

        public void Update(ITimer timer)
        {
            if (timer is null)
            {
                return;
            }

            this.optComponent?.Update(timer);
        }

        public void Render()
        {
            var context = this.deviceResources.D3DContext;
            var d2dRenderTarget = this.deviceResources.D2DRenderTarget;

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.0f, 0.0f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            d2dRenderTarget.BeginDraw();

            if (this.backgroundBitmap is not null)
            {
                D2D1SizeF renderTargetSize = d2dRenderTarget.Size;
                D2D1SizeF bitmapSize = this.backgroundBitmap.Size;

                float w;
                float h;

                if (renderTargetSize.Height * bitmapSize.Width <= renderTargetSize.Width * bitmapSize.Height)
                {
                    w = renderTargetSize.Height * bitmapSize.Width / bitmapSize.Height;
                    h = renderTargetSize.Height;
                }
                else
                {
                    w = renderTargetSize.Width;
                    h = renderTargetSize.Width * bitmapSize.Height / bitmapSize.Width;
                }

                float left = (renderTargetSize.Width - w) / 2;
                float top = (renderTargetSize.Height - h) / 2;

                d2dRenderTarget.DrawBitmap(this.backgroundBitmap, new D2D1RectF(left, top, left + w, top + h));
            }

            d2dRenderTarget.EndDraw();

            if (this.optComponent is not null)
            {
                this.optComponent.LightTransform = this.LightTransform;
                this.optComponent.LightBrightness = this.LightBrightness;
                this.optComponent.WorldTransform = this.WorldTransform;
                this.optComponent.ViewTransform = this.ViewTransform;
                this.optComponent.ProjectionTransform = this.ProjectionTransform;
                this.optComponent.IsPaused = this.IsPaused;
                this.optComponent.Render();
            }
        }
    }
}
