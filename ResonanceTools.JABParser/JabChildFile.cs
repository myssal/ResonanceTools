using System;

namespace ResonanceTools.JABParser;

public sealed class JabChildFile
{
    public string JabName { get; init; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Size { get; set; }
    public int UncSize { get; set; }
    public double Time { get; set; }
    public uint Crc { get; set; }
    public uint DataLocalOffset { get; set; }
    public bool CrcValid => Crc != 0;
    public bool Valid => !string.IsNullOrEmpty(Path) && Size >= 0;
}
