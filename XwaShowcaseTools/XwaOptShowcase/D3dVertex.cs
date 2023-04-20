using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace XwaOptShowcase
{
    [StructLayout(LayoutKind.Sequential)]
    struct D3dVertex
    {
        public D3dVertex(XMFloat3 position, XMFloat3 normal, XMFloat2 textureCoordinates)
        {
            this.Position = position;
            this.Normal = normal;
            this.TextureCoordinates = textureCoordinates;
        }

        public XMFloat3 Position;

        public XMFloat3 Normal;

        public XMFloat2 TextureCoordinates;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(D3dVertex));
    }
}
