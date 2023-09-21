using JeremyAnsel.DirectX.DXMath;

namespace XwaSizeComparison
{
    class SceneMeshData
    {
        public SceneMeshData()
        {
        }

        public SceneMeshData(uint indexStart, uint indexCount, XMFloat3 center, string textureName, bool hasAlpha)
        {
            this.IndexStart = indexStart;
            this.IndexCount = indexCount;
            this.Center = center;
            this.TextureName = textureName;
            this.HasAlpha = hasAlpha;
        }

        public uint IndexStart { get; set; }

        public uint IndexCount { get; set; }

        public XMFloat3 Center { get; set; }

        public string TextureName { get; set; }

        public bool HasAlpha { get; set; }
    }
}
