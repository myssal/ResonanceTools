using System.Text;
using System.Text.Json.Serialization;
using ResonanceTools.Utility;

namespace ResonanceTools.HotfixParser;

public class VO_hotfixDesc
{
    /// <summary>
    /// Reimplementation of the VO_hotfixDesc class from the game.
    /// Read the hotfix archive and serialize it, to create a json file with all the informations for downloading the files.
    /// </summary>
    
    public string? date;
    public string? patchVersion;
    public string? baseVersion;
    public const int MIN_DESC_VERSION = 1;
    public const int MAX_DESC_VERSION = 4;
    public AS3DynamicObject? overrideDic;
    public AS3DynamicObject? header;
    public List<string>? compressedJabNames;
    [JsonIgnore]
    public JABProcessor? jabProcessor;
    public Dictionary<string, VO_hotfixDescFileInfo>? files;
    public Dictionary<string, JABProcessor.VO_JABFileItemInfo>? jabItemInfoDic;

    public static VO_hotfixDesc Deserialize(string name, byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);
        // Skip first byte per decompiled logic (compression flag or marker)
        if (stream.Length == 0)
        {
            Log.Error("VO_hotfixDesc: Empty hotfix description buffer");
            throw new InvalidOperationException();
        }
        stream.Position = 1;
        byte version = reader.ReadByte();
        // Decompiled code only accepts versions 3 or 4 ( (version-3) <=1 )
        if (version < 3 || version > MAX_DESC_VERSION)
        {
            Log.Error($"VO_hotfixDesc: unsupported version {version} (expected 3-{MAX_DESC_VERSION})");
            throw new InvalidOperationException();
        }

        int JABFileCount = reader.ReadInt32();
        Log.Debug($"VO_hotfixDesc: version {version}, JABFileCount {JABFileCount}");
        VO_hotfixDesc desc = new VO_hotfixDesc();
        Log.Debug($"VO_hotfixDesc: deserializing {name} with {JABFileCount} JAB files");
        // Decode GMF block
        var decoder = GMFEncoder.DecodeFromStream(reader) as AS3DynamicObject;
        if (decoder == null)
        {
            Log.Error("VO_hotfixDesc: Failed to decode GMF hotfix desc. decoder is null");
            throw new InvalidOperationException();
        }
        desc.patchVersion = decoder["patchVersion"]?.ToString();
        desc.baseVersion = decoder["baseVersion"]?.ToString();
        desc.date = decoder["date"]?.ToString();
        desc.overrideDic = decoder["overrideDic"] as AS3DynamicObject;
        
        // Files section – incremental path reconstruction
        int fileCount = reader.ReadInt32();
        Log.Debug($"VO_hotfixDesc: patchVersion {desc.patchVersion}, baseVersion {desc.baseVersion}, date {desc.date}, fileCount {fileCount}");

        desc.files = new Dictionary<string, VO_hotfixDescFileInfo>(fileCount);
        string rollingPath = string.Empty; // corresponds to v32 in decompiled code
        for (int i = 0; i < fileCount; i++)
        {
            byte prefixLen = reader.ReadByte();   // number of characters from previous path to keep
            byte suffixType = reader.ReadByte();  // suffix enum
            ushort nameLen = reader.ReadUInt16();
            string namePart = nameLen > 0 ? Encoding.UTF8.GetString(reader.ReadBytes(nameLen)) : string.Empty;

            if (prefixLen > rollingPath.Length)
            {
                Log.Warn($"VO_hotfixDesc: prefix overflow {prefixLen}>{rollingPath.Length} at file {i}; clamping");
                prefixLen = (byte)Math.Min(prefixLen, rollingPath.Length);
            }
            rollingPath = rollingPath.Substring(0, prefixLen) + namePart;
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
                rollingPath += mappedSuffix;
            else if (suffixType > 5)
                Log.Warn($"VO_hotfixDesc: unknown suffix type {suffixType} @pos {reader.BaseStream.Position}");

            // Read size (int) and crc (uint) following decompiled order (int then uint)
            int size = reader.ReadInt32();
            uint crc = reader.ReadUInt32();

            VO_hotfixDescFileInfo fi = new VO_hotfixDescFileInfo
            {
                path = rollingPath,
                size = size,
                crc = crc
            };
            desc.files[rollingPath] = fi;
        }

        // JAB section
        if (JABFileCount > 0)
        {
            int capacity = reader.ReadInt32(); // number of JAB groups/entries to follow
            desc.jabItemInfoDic = new Dictionary<string, JABProcessor.VO_JABFileItemInfo>(capacity);
            desc.compressedJabNames = new List<string>();

            for (int processed = 0; processed < capacity; processed++)
            {
                ushort keyLen = reader.ReadUInt16();
                string jabNameKey = keyLen > 0 ? Encoding.UTF8.GetString(reader.ReadBytes(keyLen)) : string.Empty;
                byte compressedFlag = reader.ReadByte();
                int unknownInt = reader.ReadInt32(); // consumed regardless

                if (compressedFlag == 1)
                {
                    // Just a compressed jab marker list (decompiled adds name to a list and continues)
                    desc.compressedJabNames.Add(jabNameKey);
                    continue;
                }

                // Actual JAB group with file entries
                desc.jabProcessor ??= new JABProcessor();
                desc.jabProcessor.LoadHeader(reader, jabNameKey);
                uint count = desc.jabProcessor.numFiles;
                for (uint j = 0; j < count; j++)
                {
                    var item = desc.jabProcessor.ReadSingleFileItemInfo(reader);
                    // Avoid duplicates
                    if (!desc.jabItemInfoDic.ContainsKey(item.path))
                        desc.jabItemInfoDic[item.path] = item;
                }
            }
        }

        return desc;
    }
}
