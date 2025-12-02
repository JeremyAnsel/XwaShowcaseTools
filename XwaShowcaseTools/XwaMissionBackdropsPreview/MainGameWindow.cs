using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;
using System.IO;

namespace XwaMissionBackdropsPreview;

internal class MainGameWindow : GameWindowBase
{
    private MainGameComponent mainGameComponent;

    private CustomCamera camera;

    private string workingDirectory;

    private string missionFileName;

    private int missionRegion;

    private bool forceUpdate;

    public MainGameWindow()
    {
#if DEBUG
        this.DeviceResourcesOptions.Debug = true;
#endif

        this.ExitOnEscapeKey = false;
    }

    protected override void Init()
    {
        this.mainGameComponent = this.CheckMinimalFeatureLevel(new MainGameComponent(null, null, -1));

        this.camera = new CustomCamera();

        this.SelectTieFileName();

        base.Init();
    }

    private void ResetCamera()
    {
        this.camera.Reset();
        this.camera.SetViewParams(SceneConstants.VecEye, SceneConstants.VecAt);
    }

    protected override void CreateDeviceDependentResources()
    {
        base.CreateDeviceDependentResources();

        this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);

        this.ResetCamera();
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

        float fAspectRatio = (float)this.DeviceResources.BackBufferWidth / this.DeviceResources.BackBufferHeight;
        this.camera.SetProjParams(XMMath.PIDivFour, fAspectRatio, SceneConstants.ProjectionNearPlane, SceneConstants.ProjectionFarPlane);
        this.camera.SetWindow((int)this.DeviceResources.BackBufferWidth, (int)this.DeviceResources.BackBufferHeight, 0.9f);
    }

    protected override void ReleaseWindowSizeDependentResources()
    {
        base.ReleaseWindowSizeDependentResources();

        this.mainGameComponent.ReleaseWindowSizeDependentResources();
    }

    protected override void Update()
    {
        base.Update();

        if (this.forceUpdate
            || this.workingDirectory != this.mainGameComponent.WorkingDirectory
            || this.missionFileName != this.mainGameComponent.MissionFileName
            || this.missionRegion != this.mainGameComponent.MissionRegion)
        {
            bool resetCamera =
                this.forceUpdate
                || this.workingDirectory != this.mainGameComponent.WorkingDirectory
                || this.missionFileName != this.mainGameComponent.MissionFileName;

            this.forceUpdate = false;

            this.mainGameComponent.ReleaseWindowSizeDependentResources();
            this.mainGameComponent.ReleaseDeviceDependentResources();
            this.mainGameComponent = null;

            this.mainGameComponent = new MainGameComponent(this.workingDirectory, this.missionFileName, this.missionRegion);
            this.mainGameComponent.CreateDeviceDependentResources(this.DeviceResources);
            this.mainGameComponent.CreateWindowSizeDependentResources();

            if (resetCamera)
            {
                this.ResetCamera();
            }
        }

        this.mainGameComponent.Update(this.Timer);
    }

    protected override void Render()
    {
        this.mainGameComponent.WorldMatrix = XMMatrix.Identity;
        this.mainGameComponent.ViewMatrix = this.camera.GetTransformMatrix();
        this.mainGameComponent.ProjectionMatrix = this.camera.GetProjMatrix();

        this.mainGameComponent.Render();
    }

    protected override void OnEvent(WindowMessageType msg, IntPtr wParam, IntPtr lParam)
    {
        base.OnEvent(msg, wParam, lParam);

        this.camera?.HandleMessages(this.Handle, msg, wParam, lParam);
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

                case VirtualKey.Add:
                    this.missionRegion = Math.Min(3, this.missionRegion + 1);
                    break;

                case VirtualKey.Subtract:
                    this.missionRegion = Math.Max(0, this.missionRegion - 1);
                    break;

                case VirtualKey.NumPad2:
                    // Bottom
                    this.ResetCamera();
                    this.camera.SetQuat(XMQuaternion.RotationRollPitchYaw(-XMMath.PIDivTwo, 0, 0));
                    break;

                case VirtualKey.NumPad3:
                    // Rear
                    this.ResetCamera();
                    this.camera.SetQuat(XMQuaternion.RotationRollPitchYaw(0, XMMath.PI, 0));
                    break;

                case VirtualKey.NumPad4:
                    // Left
                    this.ResetCamera();
                    this.camera.SetQuat(XMQuaternion.RotationRollPitchYaw(0, XMMath.PIDivTwo, 0));
                    break;

                case VirtualKey.NumPad5:
                    // Front
                    this.ResetCamera();
                    this.camera.SetQuat(XMQuaternion.RotationRollPitchYaw(0, 0, 0));
                    break;

                case VirtualKey.NumPad6:
                    // Right
                    this.ResetCamera();
                    this.camera.SetQuat(XMQuaternion.RotationRollPitchYaw(0, -XMMath.PIDivTwo, 0));
                    break;

                case VirtualKey.NumPad8:
                    // Top
                    this.ResetCamera();
                    this.camera.SetQuat(XMQuaternion.RotationRollPitchYaw(XMMath.PIDivTwo, 0, 0));
                    break;

                case VirtualKey.O:
                    {
                        bool isFullscreen = this.DeviceResources.SwapChain.GetFullscreenState();
                        this.DeviceResources.SwapChain.SetFullscreenState(false);

                        this.SelectTieFileName();

                        if (isFullscreen)
                        {
                            this.DeviceResources.SwapChain.SetFullscreenState(true);
                        }

                        break;
                    }
            }
        }
    }

    private void SelectTieFileName()
    {
        string missionFileName = FileDialogHelpers.GetOpenTieFile();

        if (!File.Exists(missionFileName))
        {
            return;
        }

        this.workingDirectory = Path.GetDirectoryName(Path.GetDirectoryName(missionFileName));
        this.missionFileName = missionFileName;
        this.missionRegion = 0;
        this.forceUpdate = true;
    }
}
