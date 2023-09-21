using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using VideoLib;

namespace XwaSizeComparison
{
    class MainGameWindow : GameWindowBase
    {
        private MainGameComponent mainGameComponent;

        public MainGameWindow()
        {
#if DEBUG
            this.DeviceResourcesOptions.Debug = true;
#endif

            this.ExitOnEscapeKey = false;
        }

        private bool sceneFileNameChanged;
        private string sceneFileName = SceneConstants.SceneFileName;

        public string SceneFileName
        {
            get
            {
                return this.sceneFileName;
            }

            set
            {
                if (value == this.sceneFileName)
                {
                    return;
                }

                this.sceneFileName = value;
                this.sceneFileNameChanged = true;
            }
        }

        public bool IsPaused { get; set; }

        protected override void Init()
        {
            this.mainGameComponent = this.CheckMinimalFeatureLevel(new MainGameComponent());

            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);
        }

        protected override void ReleaseDeviceDependentResources()
        {
            this.mainGameComponent.ReleaseDeviceDependentResources();

            base.ReleaseDeviceDependentResources();
        }

        protected override void CreateWindowSizeDependentResources()
        {
            base.CreateWindowSizeDependentResources();

            this.mainGameComponent.CreateWindowSizeDependentResources();
        }

        protected override void ReleaseWindowSizeDependentResources()
        {
            this.mainGameComponent.ReleaseWindowSizeDependentResources();

            base.ReleaseWindowSizeDependentResources();
        }

        protected override void Update()
        {
            base.Update();

            if (!this.IsPaused)
            {
                this.mainGameComponent.Update(this.Timer);
            }

            if (this.sceneFileNameChanged)
            {
                this.sceneFileNameChanged = false;

                this.mainGameComponent.SceneFileName = this.SceneFileName;
                this.mainGameComponent.ReleaseScene();
                this.mainGameComponent.LoadScene();
                this.IsPaused = false;
            }
        }

        protected override void Render()
        {
            this.mainGameComponent.Render();
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);

            if (this.mainGameComponent is null)
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
                        {
                            bool isFullscreen = this.DeviceResources.SwapChain.GetFullscreenState();
                            this.DeviceResources.SwapChain.SetFullscreenState(false);

                            this.SceneFileName = FileDialogHelpers.GetOpenSceneFile() ?? this.SceneFileName;

                            if (isFullscreen)
                            {
                                this.DeviceResources.SwapChain.SetFullscreenState(true);
                            }

                            break;
                        }

                    case VirtualKey.Space:
                        this.IsPaused = !this.IsPaused;
                        break;

                    case VirtualKey.Return:
                        this.TakeImages();
                        break;

                    case VirtualKey.P:
                        {
                            bool fpsEnabled = this.FpsTextRenderer.IsEnabled;
                            this.FpsTextRenderer.IsEnabled = false;
                            Directory.CreateDirectory("Screenshots");
                            DateTime now = DateTime.Now;
                            string fileName = $"Screenshots\\comparison_{now:yyyyMMdd_HHmmssfff}.jpg";
                            this.Render();
                            this.DeviceResources.SaveBackBuffer(fileName);
                            this.FpsTextRenderer.IsEnabled = fpsEnabled;
                            break;
                        }
                }
            }

            if (isDown)
            {
                switch (key)
                {

                    case VirtualKey.Left:
                        this.mainGameComponent.Time = Math.Clamp(this.mainGameComponent.Time - 0.1f, 0.0f, this.mainGameComponent.AnimationTotalTime - 0.1f);
                        break;

                    case VirtualKey.Right:
                        this.mainGameComponent.Time = Math.Clamp(this.mainGameComponent.Time + 0.1f, 0.0f, this.mainGameComponent.AnimationTotalTime - 0.1f);
                        break;

                    case VirtualKey.Up:
                        this.mainGameComponent.Time = Math.Clamp(this.mainGameComponent.Time - 1.0f, 0.0f, this.mainGameComponent.AnimationTotalTime - 0.1f);
                        break;

                    case VirtualKey.Down:
                        this.mainGameComponent.Time = Math.Clamp(this.mainGameComponent.Time + 1.0f, 0.0f, this.mainGameComponent.AnimationTotalTime - 0.1f);
                        break;
                }
            }
        }

        private void TakeImages()
        {
            bool isFullscreen = this.DeviceResources.SwapChain.GetFullscreenState();
            this.DeviceResources.SwapChain.SetFullscreenState(false);

            this.ReleaseDeviceDependentResources();
            this.ReleaseWindowSizeDependentResources();

            ConsoleHelpers.OpenConsole();

            try
            {
                Directory.CreateDirectory("Screenshots");

                string baseName = "comparison";
                //Console.WriteLine("Take Screenshots");
                //TakeScreenshot($"Screenshots\\{baseName}_screenshot1.jpg", 1920, 1080);

                Console.WriteLine("Take Video");
                TakeVideo($"Screenshots\\{baseName}_low.mp4", 30, 1920, 1080, false);
                //TakeVideo($"Screenshots\\{baseName}_height.mp4", 60, 1920, 1080, true);
            }
            finally
            {
                ConsoleHelpers.CloseConsole();
            }

            this.CreateDeviceDependentResources();
            this.CreateWindowSizeDependentResources();

            if (isFullscreen)
            {
                this.DeviceResources.SwapChain.SetFullscreenState(true);
            }
        }

        private void TakeScreenshot(string fileName, int width, int height)
        {
            var device = new RenderTargetDeviceResources((uint)width, (uint)height);

            var component = new MainGameComponent
            {
                SceneFileName = this.SceneFileName
            };

            component.CreateDeviceDependentResources(device);
            component.CreateWindowSizeDependentResources();

            float animationTotalTime = component.AnimationTotalTime;
            component.Time = animationTotalTime - 10;

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

                    var component = new MainGameComponent
                    {
                        SceneFileName = this.SceneFileName
                    };

                    var timer = new FixedTimer();

                    component.CreateDeviceDependentResources(device);
                    component.CreateWindowSizeDependentResources();

                    int frameCount = (int)(fps * component.AnimationTotalTime);

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
                //Video video = Video.Open(fileName, fps, width, height, hightQuality ? 0 : 2000000);
                Video video = Video.Open(fileName, fps, width, height, hightQuality ? 0 : 1000000);

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
