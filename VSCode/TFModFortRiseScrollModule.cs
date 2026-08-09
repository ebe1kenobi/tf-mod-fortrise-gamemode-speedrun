using System;
using System.Diagnostics;
using System.Linq;
using FortRise;
using Microsoft.Extensions.Logging;
using Teuria.WiderSet;

namespace TFModFortRiseScroll
{
  public class TFModFortRiseScrollModule : Mod
  {
    public static TFModFortRiseScrollModule Instance;

    // Presence de WiderSet (ex-EightPlayerMod) : remplace l'ancien EigthPlayerImport
    // via MonoMod.ModInterop. Non-null => le mod grand-ecran est installe.
    public static IWiderSetModApi WiderSet;

    private static Type[] Registerables = [
        typeof(Scroll),
    ];

    internal Type[] Hookables = [
        typeof(ScrollRenderPatches),
        typeof(ScrollWrapPatches),
        typeof(MyScrollPlayer),
        typeof(MyScrollModeButton),
        typeof(ScrollWideScreen),
    ];

    public static TFModFortRiseScrollSettings Settings => Instance.GetSettings<TFModFortRiseScrollSettings>()!;

    /// <summary>
    /// Ecrit les reglages sur disque immediatement.
    ///
    /// FortRise ne les sauvegarde qu'en quittant le menu Options du jeu
    /// (MainMenu.DestroyOptions) ou lors d'une sauvegarde de partie. Une valeur
    /// changee depuis la popup, ou juste avant de fermer le jeu, restait donc en
    /// memoire et etait perdue. SaveSettings est internal cote FortRise, d'ou la
    /// reflexion.
    /// </summary>
    public static void SaveSettingsNow()
    {
      if (Instance == null)
        return;

      try
      {
        var method = typeof(Mod).GetMethod("SaveSettings",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null)
          method.Invoke(Instance, null);
      }
      catch (System.Exception ex)
      {
        TFModFortRiseScroll.Logger.Info($"[Settings] sauvegarde immediate impossible : {ex.Message}");
      }
    }

    public TFModFortRiseScrollModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
    {
      if (!Debugger.IsAttached)
      {
        //Debugger.Launch(); // Proposera d’attacher Visual Studio
      }
      Instance = this;
      //TFModFortRiseScroll.Logger.Init("TFModFortRiseScroll");

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
      return new TFModFortRiseScrollSettings();
    }

    private void OnLoadStateFinished(object sender, LoadState state)
    {
      if (state != LoadState.Ready)
        return;

      // Re-tente la liaison de l'API WiderSet maintenant que tous les mods sont charges.
      if (WiderSet == null)
        WiderSet = Context.Interop.GetApi<IWiderSetModApi>("Teuria.WiderSet");

      // Les deux mods cohabitent desormais : plus rien n'est retire ici.
      //
      // Le conflit ne portait que sur la propriete de la largeur d'ecran. Il est
      // regle dans SpeedRunWideScreen, qui se retire du redimensionnement tant que
      // WiderSet tient l'ecran en large (WiderSetOwnsScreen) - et comme sa largeur
      // vaut deja celle qu'on voulait, le round de Speed Run est large malgre tout.
      //
      // DisableSpeedRunMode reste disponible plus bas si le besoin de retirer le mode
      // se represente.
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
      var entry = GameModeRegistry.VersusGameModes.FirstOrDefault(m => m.VersusGameMode is Scroll);
      if (entry == null)
        return; // deja retire

      GameModeRegistry.VersusGameModes.Remove(entry);
      GameModeRegistry.RegistryVersusGameModes.Remove(entry.Name);
      GameModeRegistry.ModesToVersusGameMode.Remove(entry.Modes);
      GameModeRegistry.NameToModes.Remove(entry.Name);
    }
  }
}
