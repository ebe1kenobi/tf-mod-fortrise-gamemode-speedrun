using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseScroll
{
  // Sur le bouton de selection de mode Versus, quand le mode Scroll est
  // selectionne : appui sur Y (bouton "fleches") -> ouvre la popup d'options.
  public class MyScrollModeButton : IHookable
  {
    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(VersusModeButton), nameof(VersusModeButton.Update)),
          prefix: new HarmonyMethod(Update_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(VersusModeButton), nameof(VersusModeButton.Render)),
          postfix: new HarmonyMethod(Render_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(VersusMapButton), nameof(VersusMapButton.OnConfirm)),
          prefix: new HarmonyMethod(MapConfirm_patch)
      );
    }

    // Tant que la popup est ouverte, on ne demarre pas le match (sinon la scene
    // menu est remplacee sans fermer la popup). Il faut fermer la popup d'abord.
    private static bool MapConfirm_patch()
    {
      return !UIScrollPopup.IsOpen;
    }

    private static bool IsScrollSelected()
    {
      return ScrollRenderPatches.IsScrollMode(MainMenu.VersusMatchSettings);
    }

    private static bool AnyPlayerArrowsPressed()
    {
      for (int i = 0; i < TFGame.PlayerInputs.Length; i++)
      {
        PlayerInput input = TFGame.PlayerInputs[i];
        if (input != null && input.GetState().ArrowsPressed)
          return true;
      }
      return false;
    }

    private static bool Update_patch(VersusModeButton __instance)
    {
      if (IsScrollSelected() && __instance.Selected && !UIScrollPopup.IsOpen && AnyPlayerArrowsPressed())
      {
        if (__instance.Scene != null)
        {
          Sounds.ui_click.Play(160f, 1f);
          __instance.Scene.Add(new UIScrollPopup(__instance));
        }
        return false;
      }
      return true;
    }

    private static void Render_patch(VersusModeButton __instance)
    {
      if (!__instance.Selected || UIScrollPopup.IsOpen || !IsScrollSelected())
        return;

      Vector2 hintPos = __instance.Position + new Vector2(0f, 22f);
      Draw.OutlineTextCentered(TFGame.Font, "Y: OPTIONS", hintPos, Calc.HexToColor("FFEC5E"), 1f);
    }
  }
}
