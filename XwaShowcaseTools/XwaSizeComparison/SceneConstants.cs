using JeremyAnsel.DirectX.DXMath;

namespace XwaSizeComparison
{
    static class SceneConstants
    {
        public const float ProjectionNearPlane = 1.0f;

        public const float ProjectionFarPlane = 1000000.0f;

        public static readonly XMFloat3 LightDirection = new(0.05f, 1.0f, 1.1f);

        public static readonly float LightBrightness = 1.0f;

        public static readonly XMFloat3 VecEye = new(0.0f, 2.0f, 8.0f);

        public static readonly XMFloat3 VecAt = new(0.0f, 0.0f, 0.0f);

        public static readonly XMFloat3 VecUp = new(0.0f, 1.0f, 0.0f);

        public static readonly XMVector BackgroundColor = XMVector.FromFloat(0.1f, 0.1f, 0.1f, 1.0f);

        public static readonly string SceneFileName = "Scene.txt";
    }
}
