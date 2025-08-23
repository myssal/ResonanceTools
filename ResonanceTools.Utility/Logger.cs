using System;

namespace ResonanceTools.Utility;


public static class Log
{
    /// <summary>
    /// Simple logger (console)
    /// Enable by setting the environment variable HOTFIXPARSER_DEBUG=1 or via Log.Enabled=true.
    /// </summary>
    private static bool _enabled = true;
    public static bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public static void Debug(string message)
    {
        if (!_enabled) return;
        Console.WriteLine($"[DBG] {message}");
    }

    public static void Info(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }

    public static void Warn(string message)
    {
        Console.WriteLine($"[WARN] {message}");
    }

    public static void Error(string message, Exception? ex = null)
    {
        Console.Error.WriteLine($"[ERR] {message}{(ex != null ? " :: " + ex.Message : "")}");

    }
}
