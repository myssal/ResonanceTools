using ResonanceTools.Utility;
using System;
using System.IO.Enumeration;
using System.Text;

namespace ResonanceTools.HotfixParser;

public class JABProcessor
{
    /// <summary>
    /// Reimplementation of the JAB files processor.
    /// Processes JAB files, reading their headers and file item information.
    /// From the hotfix archive.
    /// </summary>

    public const int MIN_VERSION = 1;
    public const int MAX_VERSION = 4;
    public string? jabName;
    public string? lastPath;
    public byte version;
    public byte compress;
    public uint dataSectionGOffset;
    public uint numFiles;
    public float executeTime;
    public string? expendPrefix_l;

    public struct VO_JABFileItemInfo
    {
        public string jabName;
        public string path;
        public bool compress;
        public int size;
        public int uncSize;
        public double time;
        public uint crc;
        public uint dataLocalOffset;
    }

    public void LoadHeader(BinaryReader reader, string name)
    {
        if (reader == null)
        {
            Log.Error($"JAB {name}: reader null");
            return;
        }

        // Set base metadata on this instance (not on a temporary one)
        jabName = name;
        lastPath = string.Empty; // decompiled sets to empty string

        version = reader.ReadByte();
        if (version < MIN_VERSION || version > MAX_VERSION)
        {
            Log.Error($"JAB {jabName}: versionn {version} out of range [{MIN_VERSION},{MAX_VERSION}]");
            return;
        }
            

        if (version >= 2)
            compress = reader.ReadByte();

        dataSectionGOffset = reader.ReadUInt32();
        numFiles = reader.ReadUInt32();

        if (version >= 3)
        {
            ushort prefixLength = reader.ReadUInt16();
            if (prefixLength == 0)
            {
                expendPrefix_l = string.Empty;
            }
            else
            {
                byte[] prefixBytes = reader.ReadBytes(prefixLength);
                expendPrefix_l = Encoding.UTF8.GetString(prefixBytes);
            }
            if (!string.IsNullOrEmpty(expendPrefix_l) && !expendPrefix_l.EndsWith('/'))
                expendPrefix_l += "/"; // ensure trailing slash
        }
        else
        {
            expendPrefix_l = string.Empty;
        }
    }

    public VO_JABFileItemInfo ReadSingleFileItemInfo(BinaryReader reader)
    {
        VO_JABFileItemInfo fileItem = new VO_JABFileItemInfo();

        // Initialize: jabName here refers to the container (this JAB file) per decompiled logic
        fileItem.jabName = jabName ?? string.Empty;

        // Read header for this file entry
        byte prefixLen = reader.ReadByte();          // number of chars to keep from previous lastPath
        byte suffixType = reader.ReadByte();         // determines a known textual suffix
        ushort namePartLen = reader.ReadUInt16();    // length of the UTF8 name part that replaces tail

        string namePart = namePartLen > 0 ? Encoding.UTF8.GetString(reader.ReadBytes(namePartLen)) : string.Empty;

        string previous = lastPath ?? string.Empty;
        if (prefixLen > previous.Length)
        {
            // Defensive: clamp (corrupted data). Report via onError if available.
            Log.Error($"JAB {jabName} prefix overflow: {prefixLen} > {previous.Length}");
            prefixLen = (byte)Math.Min(previous.Length, prefixLen);
        }

        // Build new relative path core (v20 in decompiled code)
        string core = previous.Substring(0, prefixLen) + namePart;

        // Map suffix types (1..5) to known suffix strings; unknown -> report but continue.
        string? mappedSuffix = suffixType switch
        {
            1 => ".asset",
            2 => ".asset_manifest",
            3 => ".prefab_asset",
            4 => ".prefab_asset_manifest",
            5 => ".manifest",
            _ => null
        };
        if (mappedSuffix != null)
            core += mappedSuffix;
        else if (suffixType > 5)
            Log.Error($"JAB {jabName} Parsing error: Unknown suffix {suffixType} @pos {reader.BaseStream.Position}");

        // Read remaining metadata
        fileItem.dataLocalOffset = reader.ReadUInt32();
        fileItem.size = reader.ReadInt32();
        if (compress == 1)
            fileItem.uncSize = reader.ReadInt32();
        fileItem.time = reader.ReadDouble();

        // Version >=4: if ends with .asset read CRC (only when suffix is exactly .asset, per decompiled check for last 6 chars == ".asset")
        if (version >= 4 && core.Length > 6 && core.EndsWith(".asset", StringComparison.Ordinal))
        {
            fileItem.crc = reader.ReadUInt32();
        }

        // Update rolling lastPath and final path with optional prefix (expendPrefix_l)
        lastPath = core;
        fileItem.path = (expendPrefix_l ?? string.Empty) + core;
        fileItem.compress = compress == 1;

        return fileItem;
    }

}
