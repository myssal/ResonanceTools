using System;
using System.Net;
using System.IO;
using System.Text;
using System.Xml.XPath;
using System.Collections.Generic;
using ResonanceTools.Utility;

namespace ResonanceTools.JABParser;

public class UtilsJab
{
    /// <summary>
    /// Decodes a JAB file, extracting its contents to the specified directory.
    /// Exact reimplementation of Assembly-CSharp.HK.Core.Utils.UtilsJab
    /// </summary>
    public static bool Decode(String iJabFilePath, String iDecodeDir, Encoding iEncoding, int iBufferSize = 262144)
    {
        if (!File.Exists(iJabFilePath))
        {
            Log.Error("UtilsJab::Decode File not found: " + iJabFilePath);
            return false;
        }

        // Determining JAB file name
        string? jabName = null;
        if (!string.IsNullOrEmpty(iJabFilePath))
        {
            // Emulating manual search for '/' then '\\' as in native code
            int idx = iJabFilePath.LastIndexOf('/');
            if (idx == -1)
                idx = iJabFilePath.LastIndexOf('\\');
            jabName = (idx == -1) ? iJabFilePath : iJabFilePath.Substring(idx + 1);
        }

        // Normalizing parameters
        if (iEncoding == null)
            iEncoding = Encoding.UTF8;
        if (iBufferSize <= 0)
            iBufferSize = 262144;
        if (string.IsNullOrEmpty(iDecodeDir))
            iDecodeDir = Directory.GetCurrentDirectory();

        // Opening stream and delegating to Stream overload
        try
        {
            using var fs = new FileStream(iJabFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            bool ok = Decode(fs, jabName, iDecodeDir, iEncoding, iBufferSize);
            return ok;
        }
        catch (Exception ex)
        {
            Log.Error("UtilsJab::Decode exception during file processing", ex);
            return false;
        }
    }

    private static bool Decode(Stream iStream, string? iJabName, string iDecodeDir, Encoding iEncoding, int iBufferSize)
    {
        if (iStream == null || !iStream.CanRead)
        {
            Log.Error("UtilsJab::Decode stream null or not readable");
            return false;
        }
        if (string.IsNullOrEmpty(iDecodeDir))
        {
            Log.Error("UtilsJab::Decode destination directory empty");
            return false;
        }
        if (iEncoding == null) iEncoding = Encoding.UTF8;
        if (iBufferSize <= 0) iBufferSize = 262144;

        try
        {
            Directory.CreateDirectory(iDecodeDir);

            using var reader = new BinaryReader(iStream, Encoding.UTF8, leaveOpen: true);
            var header = LoadHeader(reader, iEncoding);
            if (header == null || !header.Valid)
            {
                Log.Error("UtilsJab::Decode header not valid");
                return false;
            }

            // Reading directory (only metadata) before data section: we assume files are listed first and then data continues.
            string? lastRelPath = string.Empty;
            for (int i = 0; i < header.ChildCount; i++)
            {
                var child = LoadJabChildFile(reader, header, iJabName ?? string.Empty, iEncoding, ref lastRelPath);
                if (child == null || !child.Valid)
                {
                    Log.Error($"UtilsJab::Decode child {i} not valid");
                    return false;
                }
                header.AddChild(child);
            }

            // Ora estraiamo i dati usando gli offset locali (DataLocalOffset relativo a dataSectionOffset presumibilmente)
            foreach (var child in header.Children)
            {
                long dataPos = (long)header.DataSectionGOffset + child.DataLocalOffset;
                if (dataPos < 0 || dataPos > iStream.Length)
                {
                    Log.Error($"Offset out of range for {child.Path}");
                    return false;
                }
                iStream.Position = dataPos;
                byte[] rawData = new byte[child.Size];
                int total = 0;
                while (total < rawData.Length)
                {
                    int n = iStream.Read(rawData, total, rawData.Length - total);
                    if (n <= 0)
                    {
                        Log.Error($"UtilsJab::Decode incomplete read for {child.Path} ({total}/{rawData.Length})");
                        return false;
                    }
                    total += n;
                }

                byte[] finalData = rawData;
                bool attemptedDecompress = false;
                if (header.Compress && child.UncSize > 0 && child.UncSize != child.Size)
                {
                    try
                    {
                        attemptedDecompress = true;
                        finalData = ZlibHelper.JabUncompressBytes(rawData);
                        if (child.UncSize != finalData.Length)
                        {
                            Log.Warn($"{child.Path} unc size expected {child.UncSize} obtained {finalData.Length}");
                        }
                    }
                    catch (Exception dex)
                    {
                        Log.Warn($"Decompression failed for {child.Path}, saving raw data: {dex.Message}");
                        finalData = rawData; // fallback
                    }
                }

                string fullOutPath = iDecodeDir.EndsWith(Path.DirectorySeparatorChar) || iDecodeDir.EndsWith('/')
                    ? iDecodeDir + child.Path
                    : Path.Combine(iDecodeDir, child.Path);
                string? parentDir = Path.GetDirectoryName(fullOutPath);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }
                File.WriteAllBytes(fullOutPath, finalData);
                Log.Debug($"Extracted: {child.Path} size={child.Size} unc={child.UncSize} {(attemptedDecompress ? "[decomp]" : string.Empty)}");
            }

            Log.Info($"UtilsJab::Decode completed. Files extracted: {header.Children.Count}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("UtilsJab::Decode exception", ex);
            return false;
        }
    }

    // ==================== ISPEZIONE SENZA ESTRARRE ====================
    public record JabChildInfo(string Path, int Size, int UncSize, double Time, uint Crc, uint DataLocalOffset, bool Compressed);
    public record JabFileInfo(string JabName, byte Version, bool Compress, uint DataSectionGOffset, uint ChildCount, string Prefix, IReadOnlyList<JabChildInfo> Children);

    public static JabFileInfo? Inspect(string jabFilePath, Encoding? encoding = null)
    {
        if (string.IsNullOrEmpty(jabFilePath) || !File.Exists(jabFilePath))
        {
            Log.Error("JabFileInfo::Inspect: file not found: " + jabFilePath);
            return null;
        }
        encoding ??= Encoding.UTF8;
        string jabName = Path.GetFileName(jabFilePath);

        try
        {
            using var fs = new FileStream(jabFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true);
            var header = LoadHeader(reader, encoding);
            if (header == null || !header.Valid)
            {
                Log.Error("JabFileInfo::Inspect: header not valid");
                return null;
            }
            var list = new List<JabChildInfo>((int)Math.Min(header.ChildCount, 100000));
            string? lastRel = string.Empty;
            for (int i = 0; i < header.ChildCount; i++)
            {
                var child = LoadJabChildFile(reader, header, jabName, encoding, ref lastRel);
                if (child == null || !child.Valid)
                {
                    Log.Warn($"JabFileInfo::Inspect: child {i} not valid, stopping");
                    break;
                }
                header.AddChild(child);
                list.Add(new JabChildInfo(child.Path, child.Size, child.UncSize, child.Time, child.Crc, child.DataLocalOffset, header.Compress));
            }
            return new JabFileInfo(jabName, header.Version, header.Compress, header.DataSectionGOffset, header.ChildCount, header.Prefix, list);
        }
        catch (Exception ex)
        {
            Log.Error("JabFileInfo::Inspect exception", ex);
            return null;
        }
    }

    // ================== SUPPORTO HEADER / CHILD ==================
    private sealed class JabHeader
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

    private sealed class JabChildFile
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

    private static JabHeader? LoadHeader(BinaryReader reader, Encoding? encoding)
    {
        if (reader == null) return null;
        try
        {
            byte version = reader.ReadByte();
            byte flags = 0;
            if (version >= 2)
                flags = reader.ReadByte();
            uint dataSectionOffset = reader.ReadUInt32();
            uint childCount = reader.ReadUInt32();
            bool compress = (flags & 0x01) != 0;
            var header = JabHeader.Create(version, compress, dataSectionOffset, childCount);

            if (version >= 3)
            {
                ushort prefixLen = reader.ReadUInt16();
                if (prefixLen > 0)
                {
                    byte[] prefixBytes = reader.ReadBytes(prefixLen);
                    if (encoding == null) encoding = Encoding.UTF8;
                    string prefix = encoding.GetString(prefixBytes);
                    if (!prefix.EndsWith("/", StringComparison.Ordinal))
                        prefix += "/"; // coerente con EndsWith(StringLiteral___4)
                    header.Prefix = prefix;
                }
            }
            return header;
        }
        catch (EndOfStreamException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Log.Error("LoadHeader exception", ex);
            return null;
        }
    }

    private static JabChildFile? LoadJabChildFile(BinaryReader reader, JabHeader header, string jabName, Encoding enc, ref string? lastChildRelativePath)
    {
        try
        {
            byte depthOrPrefixLen = reader.ReadByte();
            byte type = reader.ReadByte();
            ushort relNameLen = reader.ReadUInt16();
            string relative = string.Empty;
            if (relNameLen > 0)
            {
                var nameBytes = reader.ReadBytes(relNameLen);
                if (nameBytes.Length != relNameLen) return null;
                relative = enc.GetString(nameBytes);
            }
            // Se abbiamo un lastChildRelativePath e depthOrPrefixLen indica una lunghezza da mantenere
            if (!string.IsNullOrEmpty(lastChildRelativePath) && depthOrPrefixLen > 0 && depthOrPrefixLen <= lastChildRelativePath.Length)
            {
                string prefixKeep = lastChildRelativePath.Substring(0, depthOrPrefixLen);
                relative = prefixKeep + relative;
            }

            // Aggiunge suffisso tipo se necessario
            relative = type switch
            {
                1 => relative + ".asset",
                2 => relative + ".asset.manifest",
                3 => relative + ".prefab.asset",
                4 => relative + ".prefab.asset.manifest",
                5 => relative + ".manifest",
                _ => relative
            };

            // Metadati
            uint dataLocalOffset = reader.ReadUInt32();
            int size = reader.ReadInt32();
            int uncSize = header.Compress ? reader.ReadInt32() : size;
            double time = reader.ReadDouble();
            uint crc = 0;
            if (header.Version >= 4 && relative.EndsWith(".asset", StringComparison.Ordinal))
            {
                crc = reader.ReadUInt32();
            }

            lastChildRelativePath = relative; // update references path
            string fullPath = header.Prefix + relative;

            return new JabChildFile
            {
                JabName = jabName,
                Path = fullPath.Replace('/', Path.DirectorySeparatorChar),
                Size = size,
                UncSize = uncSize,
                Time = time,
                Crc = crc,
                DataLocalOffset = dataLocalOffset
            };
        }
        catch (EndOfStreamException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
