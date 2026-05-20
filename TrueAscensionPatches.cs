using System;

internal static class TrueAscensionPatches
{
    // -------------------------------------------------------------------------
    // PATCH 1 — EndlessRuntime.GetCompletedLoops [Postfix]
    // Ajoute le loop_offset du config au compteur de cycles
    // -------------------------------------------------------------------------
    public static void AfterGetCompletedLoops(ref int __result)
    {
        try
        {
            if (TrueAscensionConfig.LoopOffset == 0) return;
            int original = __result;
            __result = Math.Max(0, __result + TrueAscensionConfig.LoopOffset);
            if (original != __result)
                TrueAscensionLog.Info($"Loop count : {original} → {__result} (loop_offset={TrueAscensionConfig.LoopOffset})");
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error("Erreur dans AfterGetCompletedLoops.", ex);
        }
    }

    // -------------------------------------------------------------------------
    // PATCH 2 — EndlessRuntime.ShouldInterceptWin [Postfix]
    // À l'ascension max → ne pas boucler → laisser la vraie fin s'afficher
    // -------------------------------------------------------------------------
    public static void AfterShouldInterceptWin(ref bool __result)
    {
        try
        {
            if (__result && TrueAscensionRuntime.IsAtMaxAscension())
            {
                TrueAscensionLog.Info($"Ascension {TrueAscensionRuntime.MaxAscension} atteinte → vraie fin du jeu !");
                __result = false;
            }
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error("Erreur dans AfterShouldInterceptWin.", ex);
        }
    }

    // -------------------------------------------------------------------------
    // PATCH 6 — EndlessRuntime.GetDeterministicSeed [Postfix]
    // Injecte le nouveau niveau d'ascension dans la seed de loop
    // → chaque ascension produit une map différente pour le même numéro de loop
    // -------------------------------------------------------------------------
    public static void AfterGetDeterministicSeed(ref uint __result)
    {
        try
        {
            int preLoopLevel = TrueAscensionRuntime.GetPreLoopLevel();
            if (preLoopLevel >= 0)
            {
                int newLevel = preLoopLevel + 1;
                __result = __result * 31 + (uint)newLevel;
                TrueAscensionLog.Info($"Seed déterministe modifiée pour A{newLevel} : {__result}");
            }
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error("Erreur dans AfterGetDeterministicSeed.", ex);
        }
    }

    // -------------------------------------------------------------------------
    // PATCH 3a — EndlessRuntime.TryWrapActIndex [Prefix]
    // Capture le niveau d'ascension AVANT que EndlessMode le lève
    // -------------------------------------------------------------------------
    public static void BeforeTryWrapActIndex()
    {
        try
        {
            TrueAscensionRuntime.CapturePreLoopLevel();
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error("Erreur dans BeforeTryWrapActIndex.", ex);
        }
    }

    // -------------------------------------------------------------------------
    // PATCH 3b — EndlessRuntime.TryWrapActIndex [Postfix]
    // À l'ascension max → ne pas boucler l'index d'acte
    // -------------------------------------------------------------------------
    public static void AfterTryWrapActIndex(ref bool __result)
    {
        try
        {
            if (__result && TrueAscensionRuntime.IsAtMaxAscension())
            {
                TrueAscensionLog.Info("Ascension max : annulation du wrap d'acte.");
                __result = false;
            }
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error("Erreur dans AfterTryWrapActIndex.", ex);
        }
    }

    // -------------------------------------------------------------------------
    // PATCH 4 — RunManager.InitializeSavedRun [Postfix]
    // Après chaque loop → applique les nouveaux effets d'ascension (delta)
    // -------------------------------------------------------------------------
    public static void AfterInitializeSavedRun()
    {
        try
        {
            TrueAscensionRuntime.ApplyDeltaAscensionEffects();
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error("Erreur dans AfterInitializeSavedRun.", ex);
        }
    }

    // -------------------------------------------------------------------------
    // PATCH 5 — RunManager.Launch [Postfix]
    // Nouveau run → reset l'état interne du mod
    // -------------------------------------------------------------------------
    public static void AfterLaunch()
    {
        try
        {
            if (!IsLaunchingFromEndlessGameOver())
                TrueAscensionRuntime.Reset();
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error("Erreur dans AfterLaunch.", ex);
        }
    }

    private static bool IsLaunchingFromEndlessGameOver()
    {
        try
        {
            var type = HarmonyLib.AccessTools.TypeByName("Codex.Sts2.EndlessMode.EndlessRuntime");
            var prop = HarmonyLib.AccessTools.Property(type, "IsLaunchingFromGameOverScreen");
            return (bool)(prop?.GetValue(null) ?? false);
        }
        catch { return false; }
    }
}
