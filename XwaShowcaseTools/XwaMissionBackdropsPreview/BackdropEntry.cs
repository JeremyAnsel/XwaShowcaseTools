namespace XwaMissionBackdropsPreview;

public sealed class BackdropEntry
{
    public short DataIndex1 { get; set; }

    public short DataIndex2 { get; set; }

    public int WorldX { get; set; }

    public int WorldY { get; set; }

    public int WorldZ { get; set; }

    public float ColorR { get; set; }

    public float ColorG { get; set; }

    public float ColorB { get; set; }

    public float ColorIntensity { get; set; }

    public int Side { get; set; }

    public float Scale { get; set; }

    public bool IsEnvironment { get; set; }

    public byte Flags { get; set; }

    public byte ImageNumber { get; set; }
}
