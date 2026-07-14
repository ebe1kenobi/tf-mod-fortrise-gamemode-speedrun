using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Patches permettant a un level plus grand que 32x24 tuiles de se charger et se
  // rendre. Tout est scope au mode Loop Scroll (sinon on appelle orig()).
  //
  // Le loader (LevelLoaderXML.Load) et LevelTiles/LevelBGTiles codent en dur les
  // dimensions 32x24 et la taille du Tilemap (320x240 px). On les redirige vers
  // les vraies dimensions du niveau combine (LoopScrollLevelSystem.WidthTiles/HeightTiles).
  internal static class LoopScrollRenderPatches
  {
    internal static void Load()
    {
      On.Monocle.Calc.GetBitData += GetBitData_patch;
      On.Monocle.Calc.ReadCSVIntGrid += ReadCSVIntGrid_patch;
      On.Monocle.Tilemap.ctor += Tilemap_ctor_patch;
      On.Monocle.SFX.CalculatePan += CalculatePan_patch;
    }

    internal static void Unload()
    {
      On.Monocle.Calc.GetBitData -= GetBitData_patch;
      On.Monocle.Calc.ReadCSVIntGrid -= ReadCSVIntGrid_patch;
      On.Monocle.Tilemap.ctor -= Tilemap_ctor_patch;
      On.Monocle.SFX.CalculatePan -= CalculatePan_patch;
    }

    // Vrai pendant qu'un level du mode Loop Scroll est en cours de chargement/jeu.
    internal static bool IsLoopScrollActive()
    {
      Scene scene = Engine.Instance.Scene;
      Session sess = null;
      if (scene is LevelLoaderXML loader)
        sess = loader.Session;
      else if (scene is Level lvl)
        sess = lvl.Session;

      if (sess == null || sess.MatchSettings == null)
        return false;
      return sess.MatchSettings.Mode == ModRegisters.GameModeType<LoopScroll>();
    }

    private static bool[,] GetBitData_patch(On.Monocle.Calc.orig_GetBitData orig, string data, int width, int height)
    {
      if (IsLoopScrollActive())
        return orig(data, LoopScrollLevelSystem.WidthTiles, LoopScrollLevelSystem.HeightTiles);
      return orig(data, width, height);
    }

    private static int[,] ReadCSVIntGrid_patch(On.Monocle.Calc.orig_ReadCSVIntGrid orig, string data, int width, int height)
    {
      if (IsLoopScrollActive())
        return orig(data, LoopScrollLevelSystem.WidthTiles, LoopScrollLevelSystem.HeightTiles);
      return orig(data, width, height);
    }

    private static void Tilemap_ctor_patch(On.Monocle.Tilemap.orig_ctor orig, Monocle.Tilemap self, int width, int height)
    {
      // Seuls les tilemaps de niveau (320x240) doivent etre agrandis.
      if (IsLoopScrollActive() && width == 320 && height == 240)
      {
        orig(self, LoopScrollLevelSystem.WidthTiles * 10, LoopScrollLevelSystem.HeightTiles * 10);
        return;
      }
      orig(self, width, height);
    }

    // Le pan audio est calcule en supposant un ecran de 0..320px. Dans notre grand
    // niveau, une entite au-dela de x=320 produit un pan hors [-1,1] -> crash
    // (SoundEffectInstance.set_Pan). On calcule le pan RELATIF a la fenetre camera
    // et on borne a [-1,1] par securite.
    private static float CalculatePan_patch(On.Monocle.SFX.orig_CalculatePan orig, float panX)
    {
      if (IsLoopScrollActive())
      {
        float camX = 0f;
        if (Engine.Instance.Scene is Level lvl)
          camX = lvl.Camera.X;
        float winW = Engine.Instance.Screen != null ? (float)Engine.Instance.Screen.Width : 320f;
        float local = panX - camX;
        float pan = MathHelper.Lerp(-0.5f, 0.5f, local / winW);
        return MathHelper.Clamp(pan, -1f, 1f);
      }
      return orig(panX);
    }
  }
}
