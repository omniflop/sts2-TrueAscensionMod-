using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

[ModInitializer("Initialize")]
internal static class TrueAscensionBootstrap
{
    private static readonly Harmony Harmony = new Harmony("truascension");

    public static void Initialize()
    {
        try
        {
            TrueAscensionConfig.Load();

            int attempted = 0;
            int success = 0;

            var runManagerType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Runs.RunManager")
                ?? throw new MissingMemberException("RunManager introuvable");

            var endlessRuntimeType = AccessTools.TypeByName("Codex.Sts2.EndlessMode.EndlessRuntime")
                ?? throw new MissingMemberException("EndlessRuntime introuvable. Le mod Endless Mode est-il installé ?");

            // PATCH 1 — loop_offset : ajuste le compteur de cycles
            TryPatch("EndlessRuntime.GetCompletedLoops",
                AccessTools.Method(endlessRuntimeType, "GetCompletedLoops"),
                prefix: null,
                postfix: new HarmonyMethod(typeof(TrueAscensionPatches), "AfterGetCompletedLoops"),
                ref attempted, ref success);

            // PATCH 2 — stop le loop à l'ascension 10 (ShouldInterceptWin)
            TryPatch("EndlessRuntime.ShouldInterceptWin",
                AccessTools.Method(endlessRuntimeType, "ShouldInterceptWin"),
                prefix: null,
                postfix: new HarmonyMethod(typeof(TrueAscensionPatches), "AfterShouldInterceptWin"),
                ref attempted, ref success);

            // PATCH 3 — capture le niveau pré-loop + stop le wrap d'acte à l'ascension 10
            TryPatch("EndlessRuntime.TryWrapActIndex",
                AccessTools.Method(endlessRuntimeType, "TryWrapActIndex"),
                prefix: new HarmonyMethod(typeof(TrueAscensionPatches), "BeforeTryWrapActIndex"),
                postfix: new HarmonyMethod(typeof(TrueAscensionPatches), "AfterTryWrapActIndex"),
                ref attempted, ref success);

            // PATCH 4 — applique les effets d'ascension delta après chaque loop
            TryPatch("RunManager.InitializeSavedRun",
                AccessTools.Method(runManagerType, "InitializeSavedRun"),
                prefix: null,
                postfix: new HarmonyMethod(typeof(TrueAscensionPatches), "AfterInitializeSavedRun"),
                ref attempted, ref success);

            // PATCH 5 — reset au démarrage d'un nouveau run
            TryPatch("RunManager.Launch",
                AccessTools.Method(runManagerType, "Launch"),
                prefix: null,
                postfix: new HarmonyMethod(typeof(TrueAscensionPatches), "AfterLaunch"),
                ref attempted, ref success);

            TrueAscensionLog.Info($"Initialisation terminée : {success}/{attempted} patches appliqués.");
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error("Échec de l'initialisation.", ex);
        }
    }

    private static void TryPatch(string label, MethodBase? original,
        HarmonyMethod? prefix, HarmonyMethod? postfix, ref int attempted, ref int success)
    {
        attempted++;
        if (original == null)
        {
            TrueAscensionLog.Error($"Patch '{label}' ignoré : méthode introuvable.");
            return;
        }
        try
        {
            Harmony.Patch(original, prefix, postfix);
            success++;
            TrueAscensionLog.Info($"Patch '{label}' appliqué.");
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error($"Échec du patch '{label}'.", ex);
        }
    }
}
