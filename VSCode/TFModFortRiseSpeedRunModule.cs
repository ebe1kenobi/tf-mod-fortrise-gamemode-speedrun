using System;
using System.Diagnostics;
using System.Linq;
using FortRise;
using Microsoft.Extensions.Logging;
using Teuria.WiderSet;

namespace TFModFortRiseSpeedRun
{
  public class TFModFortRiseSpeedRunModule : Mod
  {
    public static TFModFortRiseSpeedRunModule Instance;

    // Presence de WiderSet (ex-EightPlayerMod) : remplace l'ancien EigthPlayerImport
    // via MonoMod.ModInterop. Non-null => le mod grand-ecran est installe.
    public static IWiderSetModApi WiderSet;

    private static Type[] Registerables = [
        typeof(SpeedRun),
    ];

    internal Type[] Hookables = [
        typeof(SpeedRunRenderPatches),
        typeof(SpeedRunWrapPatches),
        typeof(MySpeedRunPlayer),
        typeof(MySpeedRunModeButton),
        typeof(SpeedRunWideScreen),
    ];

    public static TFModFortRiseSpeedRunSettings Settings => Instance.GetSettings<TFModFortRiseSpeedRunSettings>()!;

    public TFModFortRiseSpeedRunModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
    {
      if (!Debugger.IsAttached)
      {
        //Debugger.Launch(); // Proposera d’attacher Visual Studio
      }
      Instance = this;
      //TFModFortRiseSpeedRun.Logger.Init("TFModFortRiseSpeedRun");

      foreach (var registerable in Registerables)
      {
        registerable.GetMethod(nameof(IRegisterable.Register))!.Invoke(null, [content, context.Registry]);
      }

      foreach (var hookable in Hookables)
      {
        hookable.GetMethod(nameof(IHookable.Load))!.Invoke(null, [context.Harmony]);
      }

      // FortRise 4 utilisait AfterLoad (RiseCore.ModsAfterLoad). FortRise 5 :
      // OnModLoadStateFinished se declenche quand la phase de chargement de TOUS les
      // mods est terminee -> seul moment fiable pour detecter WiderSet, qui peut se
      // charger apres nous.
      context.Events.OnModLoadStateFinished += OnLoadStateFinished;
    }

    public override ModuleSettings CreateSettings()
    {
      return new TFModFortRiseSpeedRunSettings();
    }

    private void OnLoadStateFinished(object sender, LoadState state)
    {
      if (state != LoadState.Ready)
        return;

      // Re-tente la liaison de l'API WiderSet maintenant que tous les mods sont charges.
      if (WiderSet == null)
        WiderSet = Context.Interop.GetApi<IWiderSetModApi>("Teuria.WiderSet");

      // Incompatibilite grand-ecran avec WiderSet : s'il est present, on retire le
      // mode Speed Run de la selection Versus ET on neutralise notre wide-screen
      // (sinon nos hooks de redimensionnement ecraseraient celui de WiderSet a chaque
      // chargement de niveau). Idempotent : peut etre appele plusieurs fois.
      if (WiderSet != null)
      {
        SpeedRunWideScreen.Disabled = true;
        DisableSpeedRunMode();
      }
    }

    // Retire l'entree SpeedRun du registre FortRise 5.
    //
    // Contrairement a FortRise 4, aucune re-indexation n'est necessaire : l'identite
    // d'un mode est sa valeur Modes (stable, obtenue via EnumPool), pas sa position
    // dans VersusGameModes. GameModeRegistry.Register alimente exactement quatre
    // collections ; on defait ces quatre entrees. (GameModeTypes / GameModesMap ne
    // sont jamais peuplees pour les modes Versus en FortRise 5.)
    private static void DisableSpeedRunMode()
    {
      var entry = GameModeRegistry.VersusGameModes.FirstOrDefault(m => m.VersusGameMode is SpeedRun);
      if (entry == null)
        return; // deja retire

      GameModeRegistry.VersusGameModes.Remove(entry);
      GameModeRegistry.RegistryVersusGameModes.Remove(entry.Name);
      GameModeRegistry.ModesToVersusGameMode.Remove(entry.Modes);
      GameModeRegistry.NameToModes.Remove(entry.Name);
    }
  }
}
