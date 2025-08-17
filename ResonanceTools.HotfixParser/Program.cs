using System;
using System.Text.Json;
using ResonanceTools.HotfixParser;
using ResonanceTools.Utility;

public class Program
{
    private static void PrintUsage()
    {
        Console.WriteLine("Usage: HotfixParser <input_file> <output_file>");
        Console.WriteLine("No need for file extensions (will be added automatically and saved in .json) for <output_file>");
    }

    public static int Main(string[] args)
    {
        if (args.Length == 0 || args.Length != 2 || args[0] == "-h" || args[0] == "--help")
        {
            PrintUsage();
            return 1;
        }

        string inputFile = args[0];
        string outputFile = args[1];

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
        outputFile = Path.ChangeExtension(outputFile, ".json");
        var jsonOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true
        };
        File.WriteAllText(outputFile, JsonSerializer.Serialize(hotfixDesc, jsonOptions));
        return 0;
    }
}