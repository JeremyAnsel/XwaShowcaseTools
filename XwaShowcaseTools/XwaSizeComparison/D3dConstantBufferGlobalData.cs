using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace XwaSizeComparison
{
    [StructLayout(LayoutKind.Sequential)]
    struct D3dConstantBufferGlobalData
    {
        public XMFloat4X4 World;

        public XMFloat4X4 View;

        public XMFloat4X4 Projection;

        public XMFloat4 LightDirection;

        public float LightBrightness;

        private XMFloat3 _dummmy;

        public static readonly uint Size = (uint)Marshal.SizeOf<D3dConstantBufferGlobalData>();
    }
}
