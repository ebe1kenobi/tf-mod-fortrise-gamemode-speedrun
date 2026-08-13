using System;
using System.Diagnostics;
using System.Linq;
using FortRise;
using Microsoft.Extensions.Logging;

namespace TFModFortRiseScroll
{
  public class TFModFortRiseScrollModule : Mod
  {
    public static TFModFortRiseScrollModule Instance;

    private static Type[] Registerables = [
        typeof(Scroll),
    ];

    internal Type[] Hookables = [
        typeof(ScrollRenderPatches),
        typeof(ScrollWrapPatches),
        typeof(MyScrollPlayer),
        typeof(MyScrollModeButton),
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
      TFModFortRiseScroll.Logger.Init(logger);

      foreach (var registerable in Registerables)
      {
        registerable.GetMethod(nameof(IRegisterable.Register))!.Invoke(null, [content, context.Registry]);
      }

      foreach (var hookable in Hookables)
      {
        hookable.GetMethod(nameof(IHookable.Load))!.Invoke(null, [context.Harmony]);
      }
    }

    public override ModuleSettings CreateSettings()
    {
      return new TFModFortRiseScrollSettings();
    }

    // NOTE : ce mod ne connait plus WiderSet du tout.
    //
    // Il redimensionnait l'ecran lui-meme pour ses manches, et devait donc s'entendre
    // avec WiderSet, qui fait la meme chose de son cote : detection par interop, mise
    // en retrait, restauration... Tout cela a disparu. Pour jouer Scroll en grand
    // ecran, on active le mode de WiderSet - il elargit le jeu ENTIER, et bien plus
    // completement que ce mod ne le faisait pour ses seuls rounds.
    //
    // DisableScrollMode reste disponible plus bas si le besoin de retirer le mode se
    // represente.

    // Retire l'entree Scroll du registre FortRise 5.
    //
    // Contrairement a FortRise 4, aucune re-indexation n'est necessaire : l'identite
    // d'un mode est sa valeur Modes (stable, obtenue via EnumPool), pas sa position
    // dans VersusGameModes. GameModeRegistry.Register alimente exactement quatre
    // collections ; on defait ces quatre entrees. (GameModeTypes / GameModesMap ne
    // sont jamais peuplees pour les modes Versus en FortRise 5.)
    private static void DisableScrollMode()
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
