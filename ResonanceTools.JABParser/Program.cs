using System;
using System.IO;
using System.Text;

namespace ResonanceTools.JABParser;

public class Program
{
    private static void PrintUsage()
    {
        Console.WriteLine("Usage: JABParser <file.jab>/<directory> [--extract <outDir>] [--buffer <size>] [--json <meta.json>]");
        Console.WriteLine("Without --extract, the tool generates only the JSON metadata.");
        Console.WriteLine("With --extract, won't generate JSON metadata.");
    }

    // Helper to avoid duplicating top-level path segment (e.g., Asset) in outDir and internal paths.
    public static string NormalizeDecodeDir(string outDir, string? prefix)
    {
        if (string.IsNullOrEmpty(outDir))
            return Directory.GetCurrentDirectory();
        if (string.IsNullOrEmpty(prefix))
            return outDir;

        // Get first segment of prefix (normalize separators)
        string p = prefix.Replace('\\', '/').TrimStart('/');
        int slash = p.IndexOf('/');
        string firstSeg = slash >= 0 ? p.Substring(0, slash) : p;
        if (string.IsNullOrEmpty(firstSeg))
            return outDir;

        // Compare with last segment of outDir
        string trimmedOut = outDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string outName = Path.GetFileName(trimmedOut);
        if (!string.IsNullOrEmpty(outName) && outName.Equals(firstSeg, StringComparison.OrdinalIgnoreCase))
        {
            string? parent = Path.GetDirectoryName(trimmedOut);
            return string.IsNullOrEmpty(parent) ? Directory.GetCurrentDirectory() : parent;
        }
        return outDir;
    }

    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
        {
            PrintUsage();
            return 1;
        }

        string inputPath = args[0];
        bool isDirectory = Directory.Exists(inputPath);
        bool isFile = File.Exists(inputPath);
        if (!isDirectory && !isFile)
        {
            Console.Error.WriteLine("File or directory not found: " + inputPath);
            return 2;
        }

        // Simple arg parsing
        string? outDir = null;
        string? jsonOut = null;
        int bufferSize = 262144;
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--extract":
                    if (i + 1 < args.Length) outDir = args[++i];
                    break;
                case "--buffer":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var b) && b > 0) bufferSize = b;
                    break;
                case "--json":
                    if (i + 1 < args.Length) jsonOut = args[++i];
                    break;
            }
        }

        try
        {

            if (isDirectory)
            {
                var jabFiles = Directory.GetFiles(inputPath, "*.jab", SearchOption.AllDirectories);
                if (jabFiles.Length == 0)
                {
                    Console.Error.WriteLine("No .jab files found in the directory.");
                    return 6;
                }
                bool doExtract = !string.IsNullOrEmpty(outDir);
                foreach (var jabFile in jabFiles)
                {
                    Console.WriteLine($"Processing: {jabFile}");
                    var info = UtilsJab.Inspect(jabFile, Encoding.UTF8);
                    if (info == null)
                    {
                        Console.Error.WriteLine($"Parsing failed for {jabFile}");
                        continue;
                    }
                    // Save metadata only when NOT extracting; if extracting and no explicit --json for dir mode, skip metas.
                    if (!doExtract)
                    {
                        string metaPath = Path.ChangeExtension(jabFile, ".jabmeta.json");
                        var json = System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(metaPath, json, Encoding.UTF8);
                        Console.WriteLine("Metadata saved in: " + metaPath);
                    }

                    if (doExtract)
                    {
                        // Extract directly into the provided outDir (single shared folder),
                        // but normalize to avoid duplicating top-level prefix (e.g., Asset\\Asset\\...).
                        string decodeRoot = NormalizeDecodeDir(outDir!, info.Prefix);
                        if (!UtilsJab.Decode(jabFile, decodeRoot, Encoding.UTF8, bufferSize))
                        {
                            Console.Error.WriteLine($"Extraction failed for {jabFile}");
                            continue;
                        }
                        Console.WriteLine("Extraction completed in: " + outDir);
                    }
                }
                return 0;
            }
            else if (isFile)
            {
                var info = UtilsJab.Inspect(inputPath, Encoding.UTF8);
                if (info == null)
                {
                    Console.Error.WriteLine("Parsing failed");
                    return 3;
                }
                bool doExtract = !string.IsNullOrEmpty(outDir);

                // Saving JSON Metadata: only if not extracting OR user explicitly requested --json
                if (!doExtract || !string.IsNullOrEmpty(jsonOut))
                {
                    jsonOut ??= Path.ChangeExtension(inputPath, ".jabmeta.json");
                    var json = System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(jsonOut, json, Encoding.UTF8);
                    Console.WriteLine("Metadata saved to: " + jsonOut);
                }

                // Optional extraction
                if (doExtract)
                {
                    string decodeRoot = NormalizeDecodeDir(outDir!, info.Prefix);
                    if (!UtilsJab.Decode(inputPath, decodeRoot, Encoding.UTF8, bufferSize))
                    {
                        Console.Error.WriteLine("Extraction failed");
                        return 4;
                    }
                    Console.WriteLine("Extraction completed in: " + outDir);
                }
                return 0;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
            return 5;
        }
    }
}
