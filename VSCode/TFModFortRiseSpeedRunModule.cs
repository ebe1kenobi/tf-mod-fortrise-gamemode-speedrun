using System;
using System.Diagnostics;
using System.IO;
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
      LoopScrollRenderPatches.Load();
      LoopScrollWrapPatches.Load();
      MyLoopScrollPlayer.Load();
      MyLoopScrollModeButton.Load();
      LoopScrollWideScreen.Load();
    }


    public override void Unload()
    {
      LoopScrollRenderPatches.Unload();
      LoopScrollWrapPatches.Unload();
      MyLoopScrollPlayer.Unload();
      MyLoopScrollModeButton.Unload();
      LoopScrollWideScreen.Unload();
    }
  }
}
