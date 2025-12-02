using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.Xwa.Dat;
using System;

namespace XwaMissionBackdropsPreview;

internal sealed class BackdropModel
{
    public DeviceResources deviceResources;

    public D3D11Buffer vertexBuffer;

    public D3D11Buffer indexBuffer;

    public D3D11Texture2D texture;

    public D3D11ShaderResourceView textureView;

    public int vertexCount;

    public int indexCount;

    public BackdropEntry backdrop;

    public bool isWrap;

    public short groupId;

    public short imageId;

    public BackdropModel(DeviceResources resources, DatImage planetImage, BackdropEntry backdrop, bool isWrap)
    {
        this.deviceResources = resources;
        this.backdrop = backdrop;
        this.isWrap = isWrap;

        var loader = new BasicLoader(this.deviceResources.D3DDevice);
        var shapes = new BasicShapes(this.deviceResources.D3DDevice);

        double size = backdrop.Scale / 128.0;

        if (isWrap)
        {
            //size *= 0.95491;
            size *= 0.95465;
            double angleWidth = Math.PI * planetImage.Width * size / 4096.0;
            double angleHeight = Math.PI * planetImage.Height * size / 4096.0;

            shapes.CreateSphereArc((float)angleWidth, (float)angleHeight, out this.vertexBuffer, out this.indexBuffer, out this.vertexCount, out this.indexCount);
        }
        else
        {
            size *= 1.75;
            double width = planetImage.Width * size / 4096.0;
            double height = planetImage.Height * size / 4096.0;
            float z = -0.75f;
            //float z = -1.0f;
            shapes.CreatePlane((float)width, (float)height, z, out this.vertexBuffer, out this.indexBuffer, out this.vertexCount, out this.indexCount);
        }

        byte[] imageData = planetImage.GetImageData();
        loader.LoadTexture(imageData, (uint)planetImage.Width, (uint)planetImage.Height, out this.texture, out this.textureView);

        this.groupId = planetImage.GroupId;
        this.imageId = planetImage.ImageId;
    }

    public void Release()
    {
        D3D11Utils.DisposeAndNull(ref this.vertexBuffer);
        D3D11Utils.DisposeAndNull(ref this.indexBuffer);
        D3D11Utils.DisposeAndNull(ref this.textureView);
        D3D11Utils.DisposeAndNull(ref this.texture);
    }

    public XMMatrix GetTransformMatrix()
    {
        XMMatrix transform = XMMatrix.Identity;

        if (this.isWrap)
        {
            MathUtils.ComputeHeadingAngles(backdrop.WorldX, backdrop.WorldY, backdrop.WorldZ, out double headingXY, out double headingZ);

            transform *= XMMatrix.RotationAxis(new XMVector(0, 0, 1, 1), (float)headingZ);
            transform *= XMMatrix.RotationAxis(new XMVector(0, 1, 0, 1), (float)headingXY);
        }
        else
        {
            MathUtils.ComputeHeadingAngles(backdrop.WorldX, backdrop.WorldY, backdrop.WorldZ, out double headingXY, out double headingZ);

            transform *= XMMatrix.RotationAxis(new XMVector(0, 0, 1, 1), (float)headingZ);
            transform *= XMMatrix.RotationAxis(new XMVector(0, 1, 0, 1), (float)headingXY);
        }

        return transform;
    }
}
