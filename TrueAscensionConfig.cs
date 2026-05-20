using System;
using System.Globalization;
using System.IO;
using System.Reflection;

internal static class TrueAscensionConfig
{
    // Offset de loops (pour corriger les saves custom)
    public static int LoopOffset { get; private set; } = 0;

    private static string ConfigPath => Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory,
        "truascension.config"
    );

    public static void Load()
    {
        LoopOffset = 0;
        try
        {
            EnsureConfigExists();
            foreach (string line in File.ReadAllLines(ConfigPath))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

                string[] parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                if (parts[0].ToLowerInvariant() == "loop_offset")
                {
                    if (int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int val))
                        LoopOffset = val;
                }
            }
            TrueAscensionLog.Info($"Config chargée : loop_offset={LoopOffset}");
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error("Impossible de charger la config, valeurs par défaut utilisées.", ex);
            LoopOffset = 0;
        }
    }

    private static void EnsureConfigExists()
    {
        string? dir = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(ConfigPath))
        {
            File.WriteAllText(ConfigPath,
                "# True Ascension - Configuration\n" +
                "\n" +
                "# Nombre de cycles à ajouter au compteur Endless Mode\n" +
                "# Utile si ta save custom est à un acte avancé mais que le mod croit que c'est le cycle 1\n" +
                "# Positif = plus difficile | Négatif = plus facile | 0 = désactivé\n" +
                "loop_offset 0\n"
            );
        }
    }
}
