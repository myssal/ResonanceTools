using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace ResonanceTools.Utility;

public static class ZlibHelper
{
    /// <summary>
    /// Decompress the bytes with zlib inflate.
    /// (The hotfix archive have the first byte inverted.)
    /// (For compressed JAB files, we use standard zlib inflate.)
    /// Does not modify the original array: works on a copy.
    /// </summary>
    public static byte[] UncompressBytes(byte[] data)
    {
        if (data == null || data.Length == 0)
            return Array.Empty<byte>();

        // Copying to avoid external side effects.
        var working = new byte[data.Length];
        Buffer.BlockCopy(data, 0, working, 0, data.Length);

        // Flipping the first byte (inverting bits); logic based on original code.
        working[0] = (byte)(~working[0] & 0xFF);

        try
        {
            Log.Debug($"ZlibHelper try zlib inflate...");
            return InflateZlib(working);
        }
        catch (Exception ex)
        {
            Log.Debug($"ZlibHelper zlib inflate failed: {ex.Message}");
            throw new InvalidOperationException();
        }
    }

    public static byte[] JabUncompressBytes(byte[] data)
    {
        if (data == null || data.Length == 0)
            return Array.Empty<byte>();

        // Copying to avoid external side effects.
        var working = new byte[data.Length];
        Buffer.BlockCopy(data, 0, working, 0, data.Length);

        try
        {
            Log.Debug("ZlibHelper try zlib inflate...");
            return InflateZlib(working);
        }
        catch (Exception ex)
        {
            Log.Debug($"ZlibHelper zlib inflate failed: {ex.Message}");
            throw new InvalidOperationException();
        }
    }

    private static byte[] InflateZlib(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var inflater = new InflaterInputStream(ms, new Inflater(false))) // 'false' = zlib header
        using (var outStream = new MemoryStream())
        {
            inflater.CopyTo(outStream);
            return outStream.ToArray();
        }
    }
}
