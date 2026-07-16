using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Sur le bouton de selection de mode Versus, quand le mode Loop Scroll est
  // selectionne : appui sur Y (bouton "fleches") -> ouvre la popup d'options.
  internal static class MySpeedRunModeButton
  {
    internal static void Load()
    {
      On.TowerFall.VersusModeButton.Update += Update_patch;
      On.TowerFall.VersusModeButton.Render += Render_patch;
      On.TowerFall.VersusMapButton.OnConfirm += MapConfirm_patch;
    }

    internal static void Unload()
    {
      On.TowerFall.VersusModeButton.Update -= Update_patch;
      On.TowerFall.VersusModeButton.Render -= Render_patch;
      On.TowerFall.VersusMapButton.OnConfirm -= MapConfirm_patch;
    }

    // Tant que la popup est ouverte, on ne demarre pas le match (sinon la scene
    // menu est remplacee sans fermer la popup). Il faut fermer la popup d'abord.
    private static void MapConfirm_patch(On.TowerFall.VersusMapButton.orig_OnConfirm orig, global::TowerFall.VersusMapButton self)
    {
      if (UISpeedRunPopup.IsOpen)
        return;
      orig(self);
    }

    private static bool IsSpeedRunSelected()
    {
      return MainMenu.VersusMatchSettings.IsCustom
          && MainMenu.VersusMatchSettings.CurrentModeName == nameof(SpeedRun);
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
      if (IsSpeedRunSelected() && self.Selected && !UISpeedRunPopup.IsOpen && AnyPlayerArrowsPressed())
      {
        if (self.Scene != null)
        {
          Sounds.ui_click.Play(160f, 1f);
          self.Scene.Add(new UISpeedRunPopup(self));
        }
        return;
      }
      orig(self);
    }

    private static void Render_patch(On.TowerFall.VersusModeButton.orig_Render orig, global::TowerFall.VersusModeButton self)
    {
      orig(self);

      if (!self.Selected || UISpeedRunPopup.IsOpen || !IsSpeedRunSelected())
        return;

      Vector2 hintPos = self.Position + new Vector2(0f, 22f);
      Draw.OutlineTextCentered(TFGame.Font, "Y: OPTIONS", hintPos, Calc.HexToColor("FFEC5E"), 1f);
    }
  }
}
