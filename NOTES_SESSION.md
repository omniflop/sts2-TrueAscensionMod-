# Session d'apprentissage — Modding STS2

## Ce qu'on a appris

### Architecture de STS2
- Le jeu tourne sur **Godot Engine** avec **C#**
- Les mods sont des `.dll` C# chargés depuis `Slay the Spire 2/Mods/<nom>/`
- Chaque mod a un `.json` (métadonnées) et optionnellement un `.config`

### Les outils
- **ILSpy** : décompile les `.dll` pour lire le code source
- **0Harmony** : bibliothèque de patching — permet d'intercepter les méthodes du jeu
- **AccessTools** : utilitaire Harmony pour trouver des types/méthodes par leur nom
- **[ModInitializer("Initialize")]** : attribut qui désigne le point d'entrée du mod

### Harmony — les concepts clés
```csharp
// Prefix  = s'exécute AVANT la méthode originale
// Postfix = s'exécute APRÈS la méthode originale

Harmony.Patch(methodeOriginale,
    prefix:  new HarmonyMethod(typeof(MaClasse), "AvantLaMethode"),
    postfix: new HarmonyMethod(typeof(MaClasse), "AprèsLaMethode")
);

// ref __result = modifier la valeur de retour dans un Postfix
public static void MonPostfix(ref int __result) {
    __result += 10; // modifie ce que la méthode retourne
}

// ref parametre = modifier un paramètre dans un Prefix
public static void MonPrefix(ref int actIndex) {
    actIndex = 0; // modifie le paramètre avant que la méthode s'exécute
}
```

---

## Ce qu'on a découvert sur le mod Endless Mode

### Fichiers analysés
| Fichier | Rôle |
|---|---|
| `EndlessBootstrap` | Point d'entrée, enregistre tous les patches |
| `EndlessConfig` | Lit `hp_scale` et `dmg_scale` depuis le `.config` |
| `EndlessRuntime` | Cerveau : compteur de cycles, multiplicateurs, transitions |
| `CombatScalingPatches` | Applique le scaling HP/DMG aux ennemis |
| `RunManagerPatches` | Intercepte le changement d'acte pour boucler |
| `GameOverScreenPatches` | Injecte le bouton "Endless" sur l'écran de fin |
| `EndlessReflection` | Accès par reflection au code privé du jeu |

### Découvertes importantes
- `AscensionLevel { get; init; }` dans `RunState` → immutable par design
- `AscensionManager` créé **une seule fois** dans `RunManager.InitializeShared`
- `AscensionManager.ApplyEffectsTo(player)` = seulement 2 effets implémentés : TightBelt et AscendersBane
- Les autres niveaux (SwarmingElites, ToughEnemies...) **ne sont pas encore implémentés** dans le jeu (Early Access)
- `DoubleBoss` est implémenté dans `RunManager.GenerateRooms`
- `RunManager.ApplyAscensionEffects(player)` = la vraie méthode publique

---

## Ce qu'on a construit : True Ascension Mod

### Repo GitHub
https://github.com/omniflop/sts2-TrueAscensionMod-

### Fonctionnalités
1. **loop_offset** : corrige le compteur de cycles pour les saves custom
2. **Effets d'ascension progressifs** : TightBelt (A4) et AscendersBane (A5) appliqués au bon moment
3. **Vraie fin à l'Ascension 10** : le jeu affiche sa victoire naturelle au lieu de boucler

### Les 5 patches
| Patch | Type | Rôle |
|---|---|---|
| `EndlessRuntime.GetCompletedLoops` | Postfix | Ajoute `loop_offset` au compteur |
| `EndlessRuntime.ShouldInterceptWin` | Postfix | Stop à A10 → vraie fin |
| `EndlessRuntime.TryWrapActIndex` | Postfix | Stop wrap d'acte à A10 |
| `RunManager.InitializeSavedRun` | Postfix | Applique effets d'ascension delta |
| `RunManager.Launch` | Postfix | Reset l'état au nouveau run |

### Config (`truascension.config`)
```
# Offset de cycles pour les saves custom
loop_offset 0
```

---

## Workflow de développement

```bash
# Compiler
cd "C:\Users\taman\OneDrive\Documents\Programmation\TrueAscensionMod"
dotnet build

# Copier le DLL dans le jeu
cp bin/Debug/truascension.dll "C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/Mods/truascension/"

# Commit et push
git add .
git commit -m "description"
git push
```

## Dossiers importants
| Chemin | Contenu |
|---|---|
| `C:\Users\taman\OneDrive\Documents\Programmation\TrueAscensionMod\` | Code source du mod |
| `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\Mods\` | Mods installés |
| `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\` | DLLs du jeu (références) |
