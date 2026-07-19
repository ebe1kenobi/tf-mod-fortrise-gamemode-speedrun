using System;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Patches permettant a un level plus grand que 32x24 tuiles de se charger et se
  // rendre. Tout est scope au mode Loop Scroll (sinon on laisse l'original).
  //
  // Le loader (LevelLoaderXML.Load) et LevelTiles/LevelBGTiles codent en dur les
  // dimensions 32x24 et la taille du Tilemap (320x240 px). On les redirige vers
  // les vraies dimensions du niveau combine (SpeedRunLevelSystem.WidthTiles/HeightTiles).
  public class SpeedRunRenderPatches : IHookable
  {
    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Calc), nameof(Calc.GetBitData)),
          prefix: new HarmonyMethod(GetBitData_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Calc), nameof(Calc.ReadCSVIntGrid)),
          prefix: new HarmonyMethod(ReadCSVIntGrid_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredConstructor(typeof(Tilemap), [typeof(int), typeof(int)]),
          prefix: new HarmonyMethod(Tilemap_ctor_patch)
      );
      // On patche les Play(panX,volume) plutot que SFX.CalculatePan : cette derniere
      // est un simple MathHelper.Lerp que le JIT inline dans ses appelants, si bien
      // qu'un patch sur la methode autonome ne toucherait pas les copies inlinees
      // (d'ou un crash set_Pan hors [-1,1] persistant). En corrigeant panX en amont,
      // le resultat est garanti quel que soit l'inlining.
      //
      // Il y a QUATRE Play indépendants dans la hierarchie SFX (SFX, SFXInstanced,
      // SFXLooped, SFXVaried) : chacun calcule CalculatePan(panX) pour son compte,
      // aucun ne delegue a un autre -> on les patche tous les quatre, sans risque de
      // double transformation. Le coffre (TreasureChest) utilise SFXInstanced.
      Type[] sfxTypes = [typeof(SFX), typeof(SFXInstanced), typeof(SFXLooped), typeof(SFXVaried)];
      foreach (var sfxType in sfxTypes)
      {
        harmony.Patch(
            AccessTools.DeclaredMethod(sfxType, "Play", [typeof(float), typeof(float)]),
            prefix: new HarmonyMethod(Play_patch)
        );
      }
    }

    // Vrai pendant qu'un level du mode Loop Scroll est en cours de chargement/jeu.
    internal static bool IsSpeedRunActive()
    {
      Scene scene = Engine.Instance != null ? Engine.Instance.Scene : null;
      Session sess = null;
      if (scene is LevelLoaderXML loader)
        sess = loader.Session;
      else if (scene is Level lvl)
        sess = lvl.Session;

      return IsSpeedRunMode(sess != null ? sess.MatchSettings : null);
    }

    // Source unique de verite pour "sommes-nous en mode Speed Run", partagee par
    // tous les hooks du mod.
    //
    // FortRise 4 : IsCustom && CurrentModeName == nameof(SpeedRun).
    // FortRise 5 : comparaison sur la valeur Modes stable de l'entree enregistree.
    internal static bool IsSpeedRunMode(MatchSettings ms)
    {
      return ms != null
          && SpeedRun.SpeedRunEntry != null
          && ms.Mode == SpeedRun.SpeedRunEntry.Modes;
    }

    private static bool GetBitData_patch(ref string data, ref int width, ref int height)
    {
      if (IsSpeedRunActive())
      {
        width = SpeedRunLevelSystem.WidthTiles;
        height = SpeedRunLevelSystem.HeightTiles;
      }
      return true;
    }

    // NB : le 1er parametre de ReadCSVIntGrid s'appelle "csv" (et non "data" comme
    // GetBitData) ; Harmony lie les parametres par nom, il doit donc correspondre.
    private static bool ReadCSVIntGrid_patch(ref string csv, ref int width, ref int height)
    {
      if (IsSpeedRunActive())
      {
        width = SpeedRunLevelSystem.WidthTiles;
        height = SpeedRunLevelSystem.HeightTiles;
      }
      return true;
    }

    private static bool Tilemap_ctor_patch(ref int width, ref int height)
    {
      // Seuls les tilemaps de niveau (320x240) doivent etre agrandis.
      if (IsSpeedRunActive() && width == 320 && height == 240)
      {
        width = SpeedRunLevelSystem.WidthTiles * 10;
        height = SpeedRunLevelSystem.HeightTiles * 10;
      }
      return true;
    }

    // Le pan audio est calcule en supposant un ecran de 0..320px. Dans notre grand
    // niveau, une entite au-dela de x=320 produit un pan hors [-1,1] -> crash
    // (SoundEffectInstance.set_Pan). On calcule le pan RELATIF a la fenetre camera,
    // borne a [-1,1], puis on re-encode ce pan dans panX pour que le
    // CalculatePan(panX) interne a Play (= Lerp(-0.5,0.5,panX/320)) le reproduise
    // exactement. Prefix void : Play s'execute ensuite normalement avec un panX sur.
    private static void Play_patch(ref float panX)
    {
      if (!IsSpeedRunActive())
        return;

      float camX = 0f;
      if (Engine.Instance.Scene is Level lvl)
        camX = lvl.Camera.X;
      float winW = Engine.Instance.Screen != null ? (float)Engine.Instance.Screen.Width : 320f;
      float local = panX - camX;
      float pan = MathHelper.Lerp(-0.5f, 0.5f, local / winW);
      pan = MathHelper.Clamp(pan, -1f, 1f);

      // Inverse de CalculatePan : newPanX tel que Lerp(-0.5,0.5,newPanX/320) == pan.
      panX = (pan + 0.5f) * 320f;
    }
  }
}
