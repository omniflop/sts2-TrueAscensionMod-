using System;
using Godot;

internal static class TrueAscensionLog
{
    public static void Info(string message)
    {
        GD.Print("[TrueAscension] " + message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        GD.PrintErr("[TrueAscension] ERROR: " + message);
        if (ex != null)
            GD.PrintErr("[TrueAscension] " + ex);
    }
}
