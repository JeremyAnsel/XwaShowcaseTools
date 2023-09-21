using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;

namespace XwaOptShowcase
{
    class SceneMesh : IComparable<SceneMesh>
    {
        public List<D3dVertex> Vertices { get; } = new();

        public List<int> Indices { get; } = new();

        public string Texture { get; set; }

        public bool HasAlpha { get; set; }

        public float Depth { get; set; }

        public int CompareTo(SceneMesh other)
        {
            int depthOrder = other.Depth.CompareTo(this.Depth);

            if (depthOrder != 0)
            {
                return depthOrder;
            }

            return this.Texture.CompareTo(other.Texture);
        }

        public void ComputeDepth(in XMMatrix m)
        {
            XMVector center = XMVector.Zero;

            for (int i = 0; i < 3; i++)
            {
                center += this.Vertices[i].Position;
            }

            center /= 3;
            float d = XMVector3.Transform(center, m).Z;
            this.Depth = d;
        }
    }
}
