using System;

namespace ResonanceTools.Utility;


public static class Log
{
    /// <summary>
    /// Semplice logger interno (console) con flag abilitazione.
    /// Abilita impostando la variabile ambiente HOTFIXPARSER_DEBUG=1 oppure via Log.Enabled=true.
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
        Console.WriteLine($"[JABParser][DBG] {message}");
    }

    public static void Info(string message)
    {
        Console.WriteLine($"[JABParser][INFO] {message}");
    }

    public static void Warn(string message)
    {
        Console.WriteLine($"[JABParser][WARN] {message}");
    }

    public static void Error(string message, Exception? ex = null)
    {
        Console.Error.WriteLine($"[JABParser][ERR] {message}{(ex != null ? " :: " + ex.Message : "")}");

    }
}
