using System;

namespace ResonanceTools.Utility;

public class ByteArray
{
    private readonly MemoryStream ms;
    private readonly BinaryWriter bw;
    private readonly bool isBigEndian;

    public ByteArray()
    {
        ms = new MemoryStream();
        bw = new BinaryWriter(ms); // For big-endian you'd wrap this
        isBigEndian = true;
    }

    public ByteArray(byte[] data, string endian)
    {
        ms = new MemoryStream();
        bw = new BinaryWriter(ms);
        isBigEndian = string.Equals(endian, "bigEndian", StringComparison.OrdinalIgnoreCase);

        if (data != null && data.Length > 0)
            ms.Write(data, 0, data.Length);

        ms.Position = 0;
    }

    public byte[] ToArray() => ms.ToArray();
}
