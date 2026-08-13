using System.Collections.Generic;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using TowerFall;

namespace TFModFortRiseScroll
{
  // Neutralise ENTIEREMENT le wrap moteur pour le mode Loop Scroll.
  //
  // Le grand niveau combine est desormais clos (cadre solide sur tout le contour
  // + interieur de l'anneau rempli) : aucun wrap n'est necessaire. Mais le wrap
  // vanilla est cable en dur a 320/240 en coordonnees ABSOLUES (teleportation de
  // position, hitbox fantomes, tests de collision modulo, rendus fantomes,
  // lumieres fantomes) et se declencherait a tort partout au-dela du premier ecran.
  public class ScrollWrapPatches : IHookable
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

    /// <summary>
    /// Vrai quand le parcours est une BANDE : une seule rangee de blocs, haute de
    /// 240 pixels comme un ecran.
    ///
    /// C'est ce qui rend le wrap VERTICAL du jeu utilisable tel quel : il teleporte a
    /// 240, et 240 est exactement la hauteur du terrain. Le wrap horizontal, lui,
    /// reste faux dans tous les cas - il teleporte a 320 alors que la bande en fait
    /// plusieurs milliers.
    /// </summary>
    private static bool BandWrapsVertically
    {
      get
      {
        return ScrollRenderPatches.IsScrollActive()
            && !ScrollLevelSystem.IsLoop
            && ScrollLevelSystem.HeightTiles == ScrollLevelBuilder.BLOCK_H;
      }
    }

    // Position : aucune teleportation horizontale. En bande, la teleportation
    // VERTICALE est refaite ici - celle du jeu s'occuperait aussi de X.
    private static bool EnforceScreenWrap_patch(LevelEntity __instance)
    {
      if (!ScrollRenderPatches.IsScrollActive())
      {
        return true;
      }

      if (BandWrapsVertically)
      {
        WrapVertically(__instance);
      }

      return false;
    }

    /// <summary>
    /// Tomber par le bas ramene par le haut, et inversement.
    ///
    /// Reprise de la moitie verticale de LevelEntity.EnforceScreenWrap, OnWrap
    /// compris : c'est ce rappel qui compte les tours d'ecran dans les statistiques
    /// du match.
    /// </summary>
    private static void WrapVertically(LevelEntity entity)
    {
      float height = ScrollLevelSystem.TotalHeightPixels;
      if (height <= 0f)
      {
        return;
      }

      if (entity.Y >= height)
      {
        entity.Y -= height;
        entity.OnWrap?.Invoke();
      }
      else if (entity.Y < 0f)
      {
        entity.Y += height;
        entity.OnWrap?.Invoke();
      }
    }

    // Collision : une seule hitbox reelle, pas de fantomes a +/-320/240.
    private static bool BuildHitList_patch(WrapHitbox __instance, List<Rectangle> hitList)
    {
      if (!ScrollRenderPatches.IsScrollActive())
        return true;

      hitList.Clear();
      hitList.Add(__instance.Bounds);
      return false;
    }

    // Tests de collision via WrapMath.Vec : coordonnees inchangees (le modulo 320
    // ferait tester la collision dans le mauvais bloc).
    private static bool ApplyWrapX_patch(float x, ref float __result)
    {
      if (ScrollRenderPatches.IsScrollActive())
      {
        __result = x;
        return false;
      }
      return true;
    }

    // En bande, la coordonnee verticale se replie comme dans le jeu : le terrain
    // fait exactement une hauteur d'ecran, la formule vanilla est donc juste.
    private static bool ApplyWrapY_patch(float y, ref float __result)
    {
      if (ScrollRenderPatches.IsScrollActive() && !BandWrapsVertically)
      {
        __result = y;
        return false;
      }

      return true;
    }

    // Rendu : une seule copie, pas de rendus fantomes decales de +/-320/240.
    private static bool Render_patch(LevelEntity __instance)
    {
      if (!ScrollRenderPatches.IsScrollActive())
        return true;

      __instance.DoWrapRender();
      return false;
    }

    // Lumieres : une seule lumiere, pas de copies fantomes (le DrawLight vanilla
    // ajoute des halos a +/-320/240 en dur quand ScreenWrap est actif).
    private static bool DrawLight_patch(LevelEntity __instance, LightingLayer layer)
    {
      if (!ScrollRenderPatches.IsScrollActive())
        return true;

      layer.DrawLight(__instance.Position, __instance.LightRadius, layer.Sine, __instance.LightColor * __instance.LightAlpha);
      return false;
    }
  }
}
