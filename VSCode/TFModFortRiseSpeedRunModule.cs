//no goal 
// can't push Y again when quit
//ligne vertical noire sur widescreen ON : ok
//desactiver le sbouton pour arriere plan dans la popup Y

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FortRise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Monocle;
using MonoMod.ModInterop;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  [Fort("com.ebe1.kenobi.TFModFortRiseSpeedRun", "TFModFortRiseSpeedRun")]
  public class TFModFortRiseSpeedRunModule : FortModule
  {
    public static TFModFortRiseSpeedRunModule Instance;

    public override Type SettingsType => typeof(TFModFortRiseSpeedRunSettings);
    public static TFModFortRiseSpeedRunSettings Settings => (TFModFortRiseSpeedRunSettings)Instance.InternalSettings;
    public TFModFortRiseSpeedRunModule()
    {
      if (!Debugger.IsAttached)
      {
        //Debugger.Launch(); // Proposera d’attacher Visual Studio
      }
      Instance = this;
      //Logger.Init("TFModFortRiseSpeedRun");
    }

    public override void LoadContent()
    {
    }

    public override void Load()
    {

      SpeedRunRenderPatches.Load();
      SpeedRunWrapPatches.Load();
      MySpeedRunPlayer.Load();
      MySpeedRunModeButton.Load();
      SpeedRunWideScreen.Load();
      typeof(EigthPlayerImport).ModInterop();

    }

    // Appele une fois TOUS les mods charges (RiseCore.ModsAfterLoad). C'est le seul
    // moment fiable pour detecter WiderSetMod (EightPlayerMod), qui peut se charger
    // APRES nous : a ce stade son export ModInterop est enregistre.
    public override void AfterLoad()
    {
      base.AfterLoad();
      // Re-lie l'import maintenant que l'export de WiderSetMod (charge tardivement)
      // est disponible.
      typeof(EigthPlayerImport).ModInterop();

      // Incompatibilite wide-screen avec WiderSetMod : s'il est present, on retire
      // le mode Speed Run de la selection Versus ET on neutralise notre wide-screen
      // (sinon nos hooks de redimensionnement ecraseraient celui de WiderSetMod a
      // chaque chargement de niveau). Idempotent : AfterLoad peut etre appele plusieurs fois.
      if (EigthPlayerImport.IsEightPlayer != null)
      {
        SpeedRunWideScreen.Disabled = true;
        DisableSpeedRunMode();
      }
    }

    private static void DisableSpeedRunMode()
    {
      var list = GameModeRegistry.VersusGameModes;
      int idx = list.FindIndex(m => m is SpeedRun);
      if (idx < 0)
        return; // deja retire

      var removed = list[idx];
      list.RemoveAt(idx);
      GameModeRegistry.GameModeTypes.Remove(removed.GetType());
      GameModeRegistry.GameModesMap.Remove(removed.ID);

      // Les indices stockes pointent dans VersusGameModes : decrementer ceux > idx
      // pour garder les mappings des autres modes coherents apres le retrait.
      foreach (var key in GameModeRegistry.GameModeTypes.Keys.ToList())
        if (GameModeRegistry.GameModeTypes[key] > idx)
          GameModeRegistry.GameModeTypes[key]--;
      foreach (var key in GameModeRegistry.LegacyGameModesMap.Keys.ToList())
        if (GameModeRegistry.LegacyGameModesMap[key] > idx)
          GameModeRegistry.LegacyGameModesMap[key]--;
    }


    public override void Unload()
    {
      //if (EigthPlayerImport.IsEightPlayer == null)
      SpeedRunRenderPatches.Unload();
      SpeedRunWrapPatches.Unload();
      MySpeedRunPlayer.Unload();
      MySpeedRunModeButton.Unload();
      SpeedRunWideScreen.Unload();
    }
  }
}
