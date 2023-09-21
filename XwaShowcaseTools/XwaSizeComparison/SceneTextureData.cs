using JeremyAnsel.Xwa.Opt;

namespace XwaSizeComparison
{
    class SceneTextureData
    {
        public string Name { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public Texture Texture { get; set; }

        public Texture NormalMap { get; set; }
    }
}
