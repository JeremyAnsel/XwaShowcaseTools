using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using VideoLib;

namespace XwaOptShowcase
{
    class MainGameWindow : GameWindowBase
    {
        private const float DefaultCameraFov = XMMath.PIDivFour;
        private const float DefaultCameraScale = 1.0f;
        private const float DefaultLightBrightness = 1.0f;

        private MainGameComponent mainGameComponent;

        private float cameraFov;
        private float cameraScale;
        private float lightBrightness;
        private SdkModelViewerCamera camera;
        private SdkModelViewerCamera lightCamera;

        public MainGameWindow()
        {
#if DEBUG
            this.DeviceResourcesOptions.Debug = true;
#endif

            this.ExitOnEscapeKey = false;
        }

        private bool optFileNameChanged;
        private string optFileName;

        public string OptFileName
        {
            get
            {
                return this.optFileName;
            }

            set
            {
                if (value == this.optFileName)
                {
                    return;
                }

                this.optFileName = value;
                this.optFileNameChanged = true;
            }
        }

        protected override void Init()
        {
            this.mainGameComponent = this.CheckMinimalFeatureLevel(new MainGameComponent());
            this.mainGameComponent.OptFileName = OptFileName;

            this.cameraFov = DefaultCameraFov;
            this.cameraScale = DefaultCameraScale;
            this.lightBrightness = DefaultLightBrightness;
            this.camera = new SdkModelViewerCamera();
            this.lightCamera = new SdkModelViewerCamera();

            this.OptFileName = FileDialogHelpers.GetOpenFile();

            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);

            this.cameraFov = DefaultCameraFov;
            this.cameraScale = DefaultCameraScale;
            this.lightBrightness = DefaultLightBrightness;
            this.camera.SetViewParams(SceneConstants.VecEye, SceneConstants.VecAt);
            this.lightCamera.SetViewParams(SceneConstants.VecEye, SceneConstants.VecAt);
        }

        protected override void ReleaseDeviceDependentResources()
        {
            base.ReleaseDeviceDependentResources();

            this.mainGameComponent.ReleaseDeviceDependentResources();
        }

        protected override void CreateWindowSizeDependentResources()
        {
            base.CreateWindowSizeDependentResources();

            this.mainGameComponent.CreateWindowSizeDependentResources();

            float fAspectRatio = (float)this.DeviceResources.BackBufferWidth / (float)this.DeviceResources.BackBufferHeight;
            this.camera.SetProjParams(XMMath.PIDivFour, fAspectRatio, SceneConstants.ProjectionNearPlane, SceneConstants.ProjectionFarPlane);
            this.camera.SetWindow((int)this.DeviceResources.BackBufferWidth, (int)this.DeviceResources.BackBufferHeight);
            this.camera.SetButtonMasks(0, 0, SdkCameraMouseKeys.LeftButton);
            this.lightCamera.SetWindow((int)this.DeviceResources.BackBufferWidth, (int)this.DeviceResources.BackBufferHeight);
            this.lightCamera.SetButtonMasks(SdkCameraMouseKeys.RightButton, 0, 0);
        }

        protected override void ReleaseWindowSizeDependentResources()
        {
            base.ReleaseWindowSizeDependentResources();

            this.mainGameComponent.ReleaseWindowSizeDependentResources();
        }

        protected override void Update()
        {
            base.Update();

            this.mainGameComponent.Update(this.Timer);

            this.camera.FrameMove(this.Timer.ElapsedSeconds);
            this.lightCamera.FrameMove(this.Timer.ElapsedSeconds);

            if (this.optFileNameChanged)
            {
                this.optFileNameChanged = false;
                this.mainGameComponent.OptFileName = this.OptFileName;
                this.mainGameComponent.ReloadOpt();
            }
        }

        protected override void Render()
        {
            this.mainGameComponent.LightTransform = this.lightCamera.GetWorldMatrix();
            this.mainGameComponent.LightBrightness = this.lightBrightness;
            this.mainGameComponent.WorldTransform = this.camera.GetWorldMatrix();
            this.mainGameComponent.ViewTransform = XMMatrix.Scaling(this.cameraScale, this.cameraScale, this.cameraScale) * this.camera.GetViewMatrix();
            this.mainGameComponent.ProjectionTransform = this.camera.GetProjMatrix();

            this.mainGameComponent.Render();
        }

        protected override void OnEvent(WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            base.OnEvent(msg, wParam, lParam);

            if (msg == WindowMessageType.MouseWheel)
            {
                ushort keys = (ushort)((ulong)wParam & 0xffffU);
                bool isShiftKey = (keys & 0x0004) != 0;
                bool isControlKey = (keys & 0x0008) != 0;
                int wheelDelta = (short)((ulong)wParam >> 16);

                if (isShiftKey)
                {
                    this.cameraFov = Math.Clamp(XMMath.ConvertToRadians(XMMath.ConvertToDegrees(this.cameraFov) + wheelDelta * 0.3f / 120.0f), XMMath.ConvertToRadians(1), XMMath.ConvertToRadians(180));
                    float fAspectRatio = (float)this.DeviceResources.BackBufferWidth / (float)this.DeviceResources.BackBufferHeight;
                    this.camera.SetProjParams(this.cameraFov, fAspectRatio, SceneConstants.ProjectionNearPlane, SceneConstants.ProjectionFarPlane);
                }
                else if (isControlKey)
                {
                    this.lightBrightness = Math.Clamp(this.lightBrightness + wheelDelta * 0.01f / 120.0f, 0.0f, 3.0f);
                }
                else
                {
                    this.cameraScale = Math.Clamp(this.cameraScale + wheelDelta * 0.03f / 120.0f, 0.1f, 10.0f);
                }

                return;
            }

            this.camera?.HandleMessages(this.Handle, msg, wParam, lParam);
            this.lightCamera?.HandleMessages(this.Handle, msg, wParam, lParam);
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);

            if (this.mainGameComponent is null || this.camera is null)
            {
                return;
            }

            if (isDown && !wasDown)
            {
                switch (key)
                {
                    case VirtualKey.Q:
                        this.Exit();
                        break;

                    case VirtualKey.O:
                        this.OptFileName = FileDialogHelpers.GetOpenFile();
                        this.camera.SetViewParams(SceneConstants.VecEye, SceneConstants.VecAt);
                        this.lightCamera.SetViewParams(SceneConstants.VecEye, SceneConstants.VecAt);
                        break;

                    case VirtualKey.Space:
                        this.mainGameComponent.IsPaused = !this.mainGameComponent.IsPaused;

                        if (!this.mainGameComponent.IsPaused)
                        {
                            this.camera.SetViewParams(SceneConstants.VecEye, SceneConstants.VecAt);
                            this.lightCamera.SetViewParams(SceneConstants.VecEye, SceneConstants.VecAt);
                        }

                        break;

                    case VirtualKey.Return:
                        this.TakeImages();
                        break;
                }
            }
        }

        private void TakeImages()
        {
            if (!File.Exists(OptFileName))
            {
                return;
            }

            bool isFullscreen = this.DeviceResources.SwapChain.GetFullscreenState();
            this.DeviceResources.SwapChain.SetFullscreenState(false);

            ConsoleHelpers.OpenConsole();

            try
            {
                Directory.CreateDirectory("Screenshots");

                string baseName = Path.GetFileNameWithoutExtension(OptFileName);
                Console.WriteLine("Take Screenshots");
                TakeScreenshot($"Screenshots\\{baseName}_screenshot1.jpg", 1920, 1080);
                TakeScreenshot($"Screenshots\\{baseName}_screenshot2.jpg", 3840, 2160);

                Console.WriteLine("Take Video");
                TakeVideo($"Screenshots\\{baseName}_low.mp4", 30, 1920, 1080, false);
            }
            finally
            {
                ConsoleHelpers.CloseConsole();
            }

            if (isFullscreen)
            {
                this.DeviceResources.SwapChain.SetFullscreenState(true);
            }
        }

        private void TakeScreenshot(string fileName, int width, int height)
        {
            var device = new RenderTargetDeviceResources((uint)width, (uint)height);
            var component = new MainGameComponent()
            {
                OptFileName = OptFileName
            };

            component.CreateDeviceDependentResources(device);
            component.CreateWindowSizeDependentResources();
            component.Time = 6.5;

            component.Render();

            device.SaveBackBuffer(fileName);

            component.ReleaseWindowSizeDependentResources();
            component.ReleaseDeviceDependentResources();
            device.Release();
        }

        private void TakeVideo(string fileName, int fps, int width, int height, bool hightQuality)
        {
            var inputFrames = new BlockingCollection<byte[]>();

            var renderFrames = Task.Factory.StartNew(() =>
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    Console.WriteLine($"Open {fileName}");

                    var device = new RenderTargetDeviceResources((uint)width, (uint)height);

                    var component = new MainGameComponent()
                    {
                        OptFileName = OptFileName
                    };

                    var timer = new FixedTimer();

                    component.CreateDeviceDependentResources(device);
                    component.CreateWindowSizeDependentResources();

                    int frameCount = fps * OptComponent.AnimationTotalTime;

                    var (Left, Top) = Console.GetCursorPosition();

                    for (int frame = 0; frame < frameCount; frame++)
                    {
                        if (frame % fps == 0)
                        {
                            Console.SetCursorPosition(Left, Top);
                            Console.WriteLine($"{100 * frame / frameCount}% in {sw.Elapsed}");
                        }

                        component.Update(timer);
                        timer.Tick(1.0 / fps);

                        component.Render();

                        byte[] buffer = device.GetBackBufferContent();
                        inputFrames.Add(buffer);
                    }

                    sw.Stop();
                    Console.SetCursorPosition(Left, Top);
                    Console.WriteLine($"100% in {sw.Elapsed}");

                    component.ReleaseWindowSizeDependentResources();
                    component.ReleaseDeviceDependentResources();
                    device.Release();
                }
                finally
                {
                    inputFrames.CompleteAdding();
                }
            });

            var writeVideo = Task.Factory.StartNew(() =>
            {
                Video video = Video.Open(fileName, fps, width, height, hightQuality ? 0 : 2000000);

                try
                {
                    foreach (byte[] buffer in inputFrames.GetConsumingEnumerable())
                    {
                        video.AppendFrame(buffer);
                    }
                }
                finally
                {
                    video.Close();
                }
            });

            Task.WaitAll(renderFrames, writeVideo);
            Console.WriteLine("End");
        }
    }
}
