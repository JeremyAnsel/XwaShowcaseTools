using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System.IO;

namespace XwaOptShowcase
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private OptComponent optComponent;

        public MainGameComponent()
        {
            this.LightTransform = XMMatrix.Identity;
            this.LightBrightness = 1.0f;
            this.WorldTransform = XMMatrix.Identity;
            this.ViewTransform = XMMatrix.LookAtLH(SceneConstants.VecEye, SceneConstants.VecAt, SceneConstants.VecUp);
        }

        public D3D11FeatureLevel MinimalFeatureLevel => D3D11FeatureLevel.FeatureLevel100;

        public string OptFileName { get; set; }

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
                this.optComponent = new OptComponent(OptFileName);
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
                this.optComponent = new OptComponent(OptFileName);
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

            this.optComponent?.CreateWindowSizeDependentResources();
        }

        public void ReleaseWindowSizeDependentResources()
        {
            this.optComponent?.ReleaseWindowSizeDependentResources();
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

            context.OutputMergerSetRenderTargets(new[] { this.deviceResources.D3DRenderTargetView }, this.deviceResources.D3DDepthStencilView);
            context.ClearRenderTargetView(this.deviceResources.D3DRenderTargetView, new float[] { 0.0f, 0.0f, 0.0f, 1.0f });
            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

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
