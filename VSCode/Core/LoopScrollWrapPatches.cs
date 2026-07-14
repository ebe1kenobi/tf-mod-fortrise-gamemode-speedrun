using System.Collections.Generic;
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
  // lumieres fantomes) et se declencherait a tort partout au-dela du premier
  // ecran. Historique important : une version precedente wrappait sur l'axe
  // perpendiculaire au scroll RELATIF a la fenetre camera — mauvaise idee, car la
  // teleportation ramenait dans la fenetre TOUTES les entites laissees derriere
  // (torches des autres blocs empilees dans la colonne visible, joueurs
  // abandonnes ramenes a l'ecran au lieu de mourir hors-champ).
  internal static class LoopScrollWrapPatches
  {
    internal static void Load()
    {
      On.TowerFall.LevelEntity.EnforceScreenWrap += EnforceScreenWrap_patch;
      On.TowerFall.WrapHitbox.BuildHitList += BuildHitList_patch;
      On.TowerFall.WrapMath.ApplyWrapX += ApplyWrapX_patch;
      On.TowerFall.WrapMath.ApplyWrapY += ApplyWrapY_patch;
      On.TowerFall.LevelEntity.Render += Render_patch;
      On.TowerFall.LevelEntity.DrawLight += DrawLight_patch;
    }

    internal static void Unload()
    {
      On.TowerFall.LevelEntity.EnforceScreenWrap -= EnforceScreenWrap_patch;
      On.TowerFall.WrapHitbox.BuildHitList -= BuildHitList_patch;
      On.TowerFall.WrapMath.ApplyWrapX -= ApplyWrapX_patch;
      On.TowerFall.WrapMath.ApplyWrapY -= ApplyWrapY_patch;
      On.TowerFall.LevelEntity.Render -= Render_patch;
      On.TowerFall.LevelEntity.DrawLight -= DrawLight_patch;
    }

    // Position : aucune teleportation aux frontieres 320/240.
    private static void EnforceScreenWrap_patch(On.TowerFall.LevelEntity.orig_EnforceScreenWrap orig, LevelEntity self)
    {
      if (LoopScrollRenderPatches.IsLoopScrollActive())
        return;
      orig(self);
    }

    // Collision : une seule hitbox reelle, pas de fantomes a +/-320/240.
    private static void BuildHitList_patch(On.TowerFall.WrapHitbox.orig_BuildHitList orig, WrapHitbox self, List<Rectangle> hitList)
    {
      if (!LoopScrollRenderPatches.IsLoopScrollActive())
      {
        orig(self, hitList);
        return;
      }
      hitList.Clear();
      hitList.Add(self.Bounds);
    }

    // Tests de collision via WrapMath.Vec : coordonnees inchangees (le modulo 320
    // ferait tester la collision dans le mauvais bloc).
    private static float ApplyWrapX_patch(On.TowerFall.WrapMath.orig_ApplyWrapX orig, float x)
    {
      if (LoopScrollRenderPatches.IsLoopScrollActive())
        return x;
      return orig(x);
    }

    private static float ApplyWrapY_patch(On.TowerFall.WrapMath.orig_ApplyWrapY orig, float y)
    {
      if (LoopScrollRenderPatches.IsLoopScrollActive())
        return y;
      return orig(y);
    }

    // Rendu : une seule copie, pas de rendus fantomes decales de +/-320/240.
    private static void Render_patch(On.TowerFall.LevelEntity.orig_Render orig, LevelEntity self)
    {
      if (!LoopScrollRenderPatches.IsLoopScrollActive())
      {
        orig(self);
        return;
      }
      self.DoWrapRender();
    }

    // Lumieres : une seule lumiere, pas de copies fantomes (le DrawLight vanilla
    // ajoute des halos a +/-320/240 en dur quand ScreenWrap est actif).
    private static void DrawLight_patch(On.TowerFall.LevelEntity.orig_DrawLight orig, LevelEntity self, LightingLayer layer)
    {
      if (!LoopScrollRenderPatches.IsLoopScrollActive())
      {
        orig(self, layer);
        return;
      }
      layer.DrawLight(self.Position, self.LightRadius, layer.Sine, self.LightColor * self.LightAlpha);
    }
  }
}
