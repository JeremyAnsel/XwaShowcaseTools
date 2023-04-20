using System.Collections.Generic;

namespace XwaOptShowcase
{
    class SceneMesh
    {
        public List<D3dVertex> Vertices { get; } = new();

        public List<ushort> Indices { get; } = new();

        public string Texture { get; set; }

        public bool HasAlpha { get; set; }
    }
}
