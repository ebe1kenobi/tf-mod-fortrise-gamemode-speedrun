using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Options gameplay du mode Loop Scroll appliquees au joueur :
  //  - meme spawn (course) : tous les joueurs apparaissent au point le plus a gauche.
  //  - pas de fleches : ShootLock force pendant l'update (aucun tir).
  //  - pas de stomp : HurtBouncedOn ignore (sauter sur la tete ne tue pas).
  internal static class MySpeedRunPlayer
  {
    internal static void Load()
    {
      On.TowerFall.Player.Added += Added_patch;
      On.TowerFall.Player.Update += Update_patch;
      On.TowerFall.Player.HurtBouncedOn += HurtBouncedOn_patch;
      On.TowerFall.Player.HUDRender += HUDRender_patch;
    }

    internal static void Unload()
    {
      On.TowerFall.Player.Added -= Added_patch;
      On.TowerFall.Player.Update -= Update_patch;
      On.TowerFall.Player.HurtBouncedOn -= HurtBouncedOn_patch;
      On.TowerFall.Player.HUDRender -= HUDRender_patch;
    }

    // Supprime la copie HUD "wrappee" (dessinee decalee de +/-320/240 par
    // GameplayLayer.BatchedRender) : sans wrap horizontal, un joueur sorti a gauche
    // faisait reapparaitre son HUD (fleches) a droite de l'ecran.
    private static void HUDRender_patch(On.TowerFall.Player.orig_HUDRender orig, global::TowerFall.Player self, bool wrapped)
    {
      if (wrapped && IsSpeedRun(self))
        return;
      orig(self, wrapped);
    }

    private static bool IsSpeedRun(global::TowerFall.Player self)
    {
      MatchSettings ms = self?.Level?.Session?.MatchSettings;
      return ms != null && ms.IsCustom && ms.CurrentModeName == nameof(SpeedRun);
    }

    private static void Added_patch(On.TowerFall.Player.orig_Added orig, global::TowerFall.Player self)
    {
      orig(self);

      if (!IsSpeedRun(self) || !TFModFortRiseSpeedRunModule.Settings.SpeedRunSameSpawn)
        return;

      var spawns = self.Level.GetXMLPositions("PlayerSpawn");
      if (spawns == null || spawns.Count == 0)
        return;

      Vector2 target = spawns[0];
      foreach (Vector2 sp in spawns)
        if (sp.X < target.X)
          target = sp;
      self.Position = target;
    }

    private static void Update_patch(On.TowerFall.Player.orig_Update orig, global::TowerFall.Player self)
    {
      if (IsSpeedRun(self) && TFModFortRiseSpeedRunModule.Settings.SpeedRunNoArrows)
      {
        bool prev = global::TowerFall.Player.ShootLock;
        global::TowerFall.Player.ShootLock = true;
        orig(self);
        global::TowerFall.Player.ShootLock = prev;
        return;
      }
      orig(self);
    }

    private static void HurtBouncedOn_patch(On.TowerFall.Player.orig_HurtBouncedOn orig, global::TowerFall.Player self, int bouncerIndex)
    {
      if (IsSpeedRun(self) && TFModFortRiseSpeedRunModule.Settings.SpeedRunNoStomp)
        return; // pas de degat quand on saute sur la tete
      orig(self, bouncerIndex);
    }
  }
}
