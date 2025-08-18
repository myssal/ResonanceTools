using System;
using ResonanceTools.JABParser;

namespace ResonanceTools.JABParser;

public sealed class JabHeader
{
    public byte Version { get; init; }
    public bool Compress { get; init; }
    public uint DataSectionGOffset { get; init; }
    public uint ChildCount { get; init; }
    public string Prefix { get; set; } = string.Empty;
    public List<JabChildFile> Children { get; } = new();
    public bool Valid => Version >= 1 && ChildCount <= 100_000; // guardrail
    public bool AddChild(JabChildFile child)
    {
        if (child == null) return false;
        Children.Add(child);
        return true;
    }
    public static JabHeader Create(byte ver, bool compress, uint dataOffset, uint childCount) => new()
    {
        Version = ver,
        Compress = compress,
        DataSectionGOffset = dataOffset,
        ChildCount = childCount
    };
}
