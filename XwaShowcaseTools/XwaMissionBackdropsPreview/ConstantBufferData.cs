using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace XwaMissionBackdropsPreview;

[StructLayout(LayoutKind.Sequential)]
internal struct ConstantBufferData
{
    public XMFloat4X4 World;

    public XMFloat4X4 View;

    public XMFloat4X4 Projection;

    public static readonly uint Size = (uint)Marshal.SizeOf<ConstantBufferData>();
}
