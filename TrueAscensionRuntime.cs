using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

internal static class TrueAscensionRuntime
{
    // Niveau d'ascension précédent, pour calculer le delta entre deux loops
    private static int _previousAscensionLevel = -1;

    // Types du jeu récupérés par reflection
    private static readonly Type RunManagerType =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Runs.RunManager")
        ?? throw new MissingMemberException("RunManager introuvable");

    private static readonly Type AscensionManagerType =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Entities.Ascension.AscensionManager")
        ?? throw new MissingMemberException("AscensionManager introuvable");

    private static readonly Type AscensionLevelType =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Entities.Ascension.AscensionLevel")
        ?? throw new MissingMemberException("AscensionLevel introuvable");

    private static readonly PropertyInfo RunManagerInstance =
        AccessTools.Property(RunManagerType, "Instance")
        ?? throw new MissingMemberException("RunManager.Instance introuvable");

    private static readonly PropertyInfo RunManagerAscensionManager =
        AccessTools.Property(RunManagerType, "AscensionManager")
        ?? throw new MissingMemberException("RunManager.AscensionManager introuvable");

    private static readonly PropertyInfo RunManagerState =
        AccessTools.Property(RunManagerType, "State")
        ?? throw new MissingMemberException("RunManager.State introuvable");

    private static readonly PropertyInfo RunStateAscensionLevel =
        AccessTools.Property(typeof(RunState), "AscensionLevel")
        ?? throw new MissingMemberException("RunState.AscensionLevel introuvable");

    private static readonly PropertyInfo RunStatePlayers =
        AccessTools.Property(typeof(RunState), "Players")
        ?? throw new MissingMemberException("RunState.Players introuvable");

    private static readonly MethodInfo ApplyAscensionEffects =
        AccessTools.Method(RunManagerType, "ApplyAscensionEffects")
        ?? throw new MissingMemberException("RunManager.ApplyAscensionEffects introuvable");

    // Niveau max = vraie fin du jeu
    public const int MaxAscension = 10;

    /// <summary>
    /// Retourne le niveau d'ascension actuel du run en cours.
    /// </summary>
    public static int GetCurrentAscensionLevel()
    {
        try
        {
            object runManager = RunManagerInstance.GetValue(null)
                ?? throw new InvalidOperationException("RunManager.Instance est null");
            object? runState = RunManagerState.GetValue(runManager);
            if (runState == null) return 0;
            return (int)(RunStateAscensionLevel.GetValue(runState) ?? 0);
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error("Impossible de lire l'AscensionLevel.", ex);
            return 0;
        }
    }

    /// <summary>
    /// Le joueur est-il à l'ascension maximale ?
    /// </summary>
    public static bool IsAtMaxAscension()
    {
        return GetCurrentAscensionLevel() >= MaxAscension;
    }

    /// <summary>
    /// Appelé après chaque loop par le patch sur InitializeSavedRun.
    /// Applique uniquement les effets d'ascension NOUVEAUX (delta entre ancien et nouveau niveau).
    /// </summary>
    public static void ApplyDeltaAscensionEffects()
    {
        try
        {
            int currentLevel = GetCurrentAscensionLevel();

            // Premier appel ou même niveau → rien à faire
            if (_previousAscensionLevel < 0)
            {
                _previousAscensionLevel = currentLevel;
                TrueAscensionLog.Info($"InitializeSavedRun : niveau d'ascension initial = {currentLevel}");
                return;
            }

            if (currentLevel <= _previousAscensionLevel)
            {
                TrueAscensionLog.Info($"InitializeSavedRun : pas de changement d'ascension ({currentLevel}), skip.");
                return;
            }

            TrueAscensionLog.Info($"Nouveau niveau d'ascension : {_previousAscensionLevel} → {currentLevel}. Application des effets delta.");

            // Récupère les joueurs du run
            object runManager = RunManagerInstance.GetValue(null)!;
            object runState = RunManagerState.GetValue(runManager)!;
            var players = RunStatePlayers.GetValue(runState) as System.Collections.IEnumerable;

            if (players == null)
            {
                TrueAscensionLog.Error("Impossible de récupérer les joueurs.");
                return;
            }

            // Pour chaque nouveau niveau gagné, applique les effets
            foreach (object player in players)
            {
                ApplyDeltaEffectsForPlayer(player, _previousAscensionLevel, currentLevel, runManager);
            }

            _previousAscensionLevel = currentLevel;
        }
        catch (Exception ex)
        {
            TrueAscensionLog.Error("Erreur lors de l'application des effets d'ascension delta.", ex);
        }
    }

    private static void ApplyDeltaEffectsForPlayer(object player, int oldLevel, int newLevel, object runManager)
    {
        // TightBelt = niveau 4 : -1 slot de potion
        // On l'applique seulement si on vient de passer le seuil 4
        int tightBeltLevel = (int)Enum.Parse(AscensionLevelType, "TightBelt");
        if (newLevel >= tightBeltLevel && oldLevel < tightBeltLevel)
        {
            try
            {
                var subtractMethod = AccessTools.Method(player.GetType(), "SubtractFromMaxPotionCount")
                    ?? AccessTools.Method(player.GetType().BaseType!, "SubtractFromMaxPotionCount");
                subtractMethod?.Invoke(player, new object[] { 1 });
                TrueAscensionLog.Info($"TightBelt appliqué au joueur {player.GetType().Name}.");
            }
            catch (Exception ex)
            {
                TrueAscensionLog.Error("Impossible d'appliquer TightBelt.", ex);
            }
        }

        // AscendersBane = niveau 5 : malédiction dans le deck
        // On l'applique seulement si on vient de passer le seuil 5
        int ascendersBaneLevel = (int)Enum.Parse(AscensionLevelType, "AscendersBane");
        if (newLevel >= ascendersBaneLevel && oldLevel < ascendersBaneLevel)
        {
            try
            {
                // On passe par ApplyAscensionEffects du RunManager avec un AscensionManager(5)
                // mais on doit éviter de réappliquer TightBelt → on crée un manager(5)
                // et on annule TightBelt si déjà appliqué (déjà géré par oldLevel >= 4)
                var tempManager = Activator.CreateInstance(AscensionManagerType, ascendersBaneLevel)!;
                RunManagerAscensionManager.SetValue(runManager, tempManager);
                ApplyAscensionEffects.Invoke(runManager, new[] { player });
                TrueAscensionLog.Info($"AscendersBane appliqué au joueur {player.GetType().Name}.");
            }
            catch (Exception ex)
            {
                TrueAscensionLog.Error("Impossible d'appliquer AscendersBane.", ex);
            }
            finally
            {
                // Remet le vrai AscensionManager avec le niveau actuel
                var realManager = Activator.CreateInstance(AscensionManagerType, newLevel)!;
                RunManagerAscensionManager.SetValue(runManager, realManager);
            }
        }

        TrueAscensionLog.Info($"Effets delta appliqués : ascension {oldLevel} → {newLevel}.");
    }

    /// <summary>
    /// Réinitialise l'état (nouveau run).
    /// </summary>
    public static void Reset()
    {
        _previousAscensionLevel = -1;
        TrueAscensionLog.Info("État TrueAscension réinitialisé.");
    }
}
