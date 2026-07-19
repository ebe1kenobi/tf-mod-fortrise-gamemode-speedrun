using System.Collections.Generic;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Neutralise ENTIEREMENT le wrap moteur pour le mode Loop Scroll.
  //
  // Le grand niveau combine est desormais clos (cadre solide sur tout le contour
  // + interieur de l'anneau rempli) : aucun wrap n'est necessaire. Mais le wrap
  // vanilla est cable en dur a 320/240 en coordonnees ABSOLUES (teleportation de
  // position, hitbox fantomes, tests de collision modulo, rendus fantomes,
  // lumieres fantomes) et se declencherait a tort partout au-dela du premier ecran.
  public class SpeedRunWrapPatches : IHookable
  {
    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(LevelEntity), "EnforceScreenWrap"),
          prefix: new HarmonyMethod(EnforceScreenWrap_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(WrapHitbox), "BuildHitList"),
          prefix: new HarmonyMethod(BuildHitList_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(WrapMath), nameof(WrapMath.ApplyWrapX)),
          prefix: new HarmonyMethod(ApplyWrapX_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(WrapMath), nameof(WrapMath.ApplyWrapY)),
          prefix: new HarmonyMethod(ApplyWrapY_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(LevelEntity), nameof(LevelEntity.Render)),
          prefix: new HarmonyMethod(Render_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(LevelEntity), nameof(LevelEntity.DrawLight)),
          prefix: new HarmonyMethod(DrawLight_patch)
      );
    }

    // Position : aucune teleportation aux frontieres 320/240.
    private static bool EnforceScreenWrap_patch()
    {
      return !SpeedRunRenderPatches.IsSpeedRunActive();
    }

    // Collision : une seule hitbox reelle, pas de fantomes a +/-320/240.
    private static bool BuildHitList_patch(WrapHitbox __instance, List<Rectangle> hitList)
    {
      if (!SpeedRunRenderPatches.IsSpeedRunActive())
        return true;

      hitList.Clear();
      hitList.Add(__instance.Bounds);
      return false;
    }

    // Tests de collision via WrapMath.Vec : coordonnees inchangees (le modulo 320
    // ferait tester la collision dans le mauvais bloc).
    private static bool ApplyWrapX_patch(float x, ref float __result)
    {
      if (SpeedRunRenderPatches.IsSpeedRunActive())
      {
        __result = x;
        return false;
      }
      return true;
    }

    private static bool ApplyWrapY_patch(float y, ref float __result)
    {
      if (SpeedRunRenderPatches.IsSpeedRunActive())
      {
        __result = y;
        return false;
      }
      return true;
    }

    // Rendu : une seule copie, pas de rendus fantomes decales de +/-320/240.
    private static bool Render_patch(LevelEntity __instance)
    {
      if (!SpeedRunRenderPatches.IsSpeedRunActive())
        return true;

      __instance.DoWrapRender();
      return false;
    }

    // Lumieres : une seule lumiere, pas de copies fantomes (le DrawLight vanilla
    // ajoute des halos a +/-320/240 en dur quand ScreenWrap est actif).
    private static bool DrawLight_patch(LevelEntity __instance, LightingLayer layer)
    {
      if (!SpeedRunRenderPatches.IsSpeedRunActive())
        return true;

      layer.DrawLight(__instance.Position, __instance.LightRadius, layer.Sine, __instance.LightColor * __instance.LightAlpha);
      return false;
    }
  }
}
