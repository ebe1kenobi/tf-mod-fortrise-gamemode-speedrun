using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Sur le bouton de selection de mode Versus, quand le mode Loop Scroll est
  // selectionne : appui sur Y (bouton "fleches") -> ouvre la popup d'options.
  internal static class MyLoopScrollModeButton
  {
    internal static void Load()
    {
      On.TowerFall.VersusModeButton.Update += Update_patch;
      On.TowerFall.VersusModeButton.Render += Render_patch;
    }

    internal static void Unload()
    {
      On.TowerFall.VersusModeButton.Update -= Update_patch;
      On.TowerFall.VersusModeButton.Render -= Render_patch;
    }

    private static bool IsLoopScrollSelected()
    {
      return MainMenu.VersusMatchSettings.IsCustom
          && MainMenu.VersusMatchSettings.CurrentModeName == nameof(LoopScroll);
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

    private static void Update_patch(On.TowerFall.VersusModeButton.orig_Update orig, global::TowerFall.VersusModeButton self)
    {
      if (IsLoopScrollSelected() && self.Selected && !UILoopScrollPopup.IsOpen && AnyPlayerArrowsPressed())
      {
        if (self.Scene != null)
        {
          Sounds.ui_click.Play(160f, 1f);
          self.Scene.Add(new UILoopScrollPopup(self));
        }
        return;
      }
      orig(self);
    }

    private static void Render_patch(On.TowerFall.VersusModeButton.orig_Render orig, global::TowerFall.VersusModeButton self)
    {
      orig(self);

      if (!self.Selected || UILoopScrollPopup.IsOpen || !IsLoopScrollSelected())
        return;

      Vector2 hintPos = self.Position + new Vector2(0f, 22f);
      Draw.OutlineTextCentered(TFGame.Font, "Y: OPTIONS", hintPos, Calc.HexToColor("FFEC5E"), 1f);
    }
  }
}
