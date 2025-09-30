using System;
using System.Text.Json;
using ResonanceTools.Utility;

namespace ResonanceTools.HotfixParser;
public static class Program
{
    private static void PrintUsage()
    {
        Console.WriteLine("Usage: HotfixParser <input_file> <output_file>");
        Console.WriteLine("Example: HotfixParser desc.bin output.json");
    }

    public static int Main(string[] args)
    {
        if (args.Length == 0 || args.Length != 2 || args[0] == "-h" || args[0] == "--help")
        {
            PrintUsage();
            return 1;
        }
        
        HotfixWrap(args[0], args[1]);
        return 0;
    }

    public static int HotfixWrap(string inputFile, string outputFile)
    {

        byte[] data = File.ReadAllBytes(inputFile);
        if (data.Length == 0)
        {
            Log.Error("Error: Input file is empty");
            return 1;
        }
        byte[] uncompressedData = ZlibHelper.UncompressBytes(data);
        if (uncompressedData.Length == 0)
        {
            Log.Error("Error: Failed to uncompress data");
            return 1;
        }
        VO_hotfixDesc hotfixDesc = VO_hotfixDesc.Deserialize("hotfix", uncompressedData);
        var jsonOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true
        };
        File.WriteAllText(outputFile, JsonSerializer.Serialize(hotfixDesc, jsonOptions));
        Log.Info($"Successfully parse {Path.GetFileName(inputFile)} -> {Path.GetFileName(outputFile)}");
        return 0;
    }
}