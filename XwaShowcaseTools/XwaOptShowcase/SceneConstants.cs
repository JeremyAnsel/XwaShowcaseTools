using JeremyAnsel.DirectX.DXMath;

namespace XwaOptShowcase
{
    static class SceneConstants
    {
        public const float ProjectionNearPlane = 0.1f;

        public const float ProjectionFarPlane = 10000.0f;

        public static readonly XMFloat3 VecEye = new(10.0f, 5.0f, 20.0f);

        public static readonly XMFloat3 VecAt = new(0.0f, 0.0f, 0.0f);

        public static readonly XMFloat3 VecUp = new(0.0f, 1.0f, 0.0f);

        //public const string OptFilename = @"C:\xwa\xwa_en_xwau\FLIGHTMODELS\XWINGEXTERIOR.OPT";
        //public const string OptFilename = @"C:\xwa\xwa_en_xwau\FLIGHTMODELS\MillenniumFalcon2Exterior.OPT";
        //public const string OptFilename = @"C:\xwa\xwa_en_xwau\FLIGHTMODELS\Corvette2Standard.OPT";
        //public const string OptFilename = @"C:\xwa\xwa_en_xwau\FLIGHTMODELS\superstardestroyer.OPT";
        //public const string OptFilename = @"C:\xwa\xwa_en_xwau\FLIGHTMODELS\imperialstardestroyer.OPT";
        public const string OptFilename = @"C:\xwa\xwa_en_vanilla\FLIGHTMODELS\ImperialStarDestroyer.opt";
    }
}
