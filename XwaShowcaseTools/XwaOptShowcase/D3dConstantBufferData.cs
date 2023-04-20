using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace XwaOptShowcase
{
    [StructLayout(LayoutKind.Sequential)]
    struct D3dConstantBufferData
    {
        public XMFloat4X4 World;

        public XMFloat4X4 View;

        public XMFloat4X4 Projection;

        public XMFloat4 LightDirection;

        public float CuttingDistanceFrom;

        public float CuttingDistanceTo;

        public int IsWireframe;

        public float LightBrightness;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(D3dConstantBufferData));
    }
}
