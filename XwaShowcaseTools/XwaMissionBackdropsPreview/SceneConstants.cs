using JeremyAnsel.DirectX.DXMath;

namespace XwaMissionBackdropsPreview;

internal static class SceneConstants
{
    public const float ProjectionNearPlane = 0.01f;

    public const float ProjectionFarPlane = 1000.0f;

    public static readonly XMFloat3 VecEye = new(0.0f, 0.0f, 0.0f);

    public static readonly XMFloat3 VecAt = new(0.0f, 0.0f, 1.0f);

    public static readonly XMFloat3 VecUp = new(0.0f, 1.0f, 0.0f);

    public const float CameraMinRadius = 0.01f;

    public const float CameraMaxRadius = 1.0f;

    public const float CameraDefaultRadius = 0.5f;

    public const float WorldScale = 0.52f;
}
