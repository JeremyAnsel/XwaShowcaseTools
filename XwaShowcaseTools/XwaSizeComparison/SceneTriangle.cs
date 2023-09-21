using JeremyAnsel.DirectX.DXMath;
using System;

namespace XwaSizeComparison
{
    class SceneTriangle : IComparable<SceneTriangle>
    {
        public XMUInt4 Index0;

        public XMUInt4 Index1;

        public XMUInt4 Index2;

        public XMVector Center;

        public float Depth;

        public int CompareTo(SceneTriangle other)
        {
            return other.Depth.CompareTo(this.Depth);
        }

        public void ComputeDepth(in XMMatrix m)
        {
            float d = XMVector3.Transform(this.Center, m).Z;
            this.Depth = d;
        }
    }
}
