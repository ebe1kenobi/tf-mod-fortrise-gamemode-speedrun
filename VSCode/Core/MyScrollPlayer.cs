using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseScroll
{
  // Options gameplay du mode Loop Scroll appliquees au joueur :
  //  - meme spawn (course) : tous les joueurs apparaissent au point le plus a gauche.
  //  - pas de fleches : ShootLock force pendant l'update (aucun tir).
  //  - pas de stomp : HurtBouncedOn ignore (sauter sur la tete ne tue pas).
  public class MyScrollPlayer : IHookable
  {
    // Etat transmis entre prefix et postfix de Update (pour restaurer ShootLock).
    private struct UpdateState
    {
      public bool Active;
      public bool PreviousShootLock;
    }

    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Added)),
          postfix: new HarmonyMethod(Added_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Update)),
          prefix: new HarmonyMethod(Update_prefix_patch),
          postfix: new HarmonyMethod(Update_postfix_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.HurtBouncedOn)),
          prefix: new HarmonyMethod(HurtBouncedOn_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.HUDRender)),
          prefix: new HarmonyMethod(HUDRender_patch)
      );
    }

    // Supprime la copie HUD "wrappee" (dessinee decalee de +/-320/240 par
    // GameplayLayer.BatchedRender) : sans wrap horizontal, un joueur sorti a gauche
    // faisait reapparaitre son HUD (fleches) a droite de l'ecran.
    private static bool HUDRender_patch(Player __instance, bool wrapped)
    {
      // false => on saute l'original (pas de rendu HUD wrappe en Scroll).
      return !(wrapped && IsScroll(__instance));
    }

    private static bool IsScroll(Player self)
    {
      MatchSettings ms = self?.Level?.Session?.MatchSettings;
      return ScrollRenderPatches.IsScrollMode(ms);
    }

    private static void Added_patch(Player __instance)
    {
      if (!IsScroll(__instance) || !TFModFortRiseScrollModule.Settings.ScrollSameSpawn)
        return;

      var spawns = __instance.Level.GetXMLPositions("PlayerSpawn");
      if (spawns == null || spawns.Count == 0)
        return;

      Vector2 target = spawns[0];
      foreach (Vector2 sp in spawns)
        if (sp.X < target.X)
          target = sp;
      __instance.Position = target;
    }

    private static void Update_prefix_patch(Player __instance, ref UpdateState __state)
    {
      __state.Active = IsScroll(__instance) && TFModFortRiseScrollModule.Settings.ScrollNoArrows;
      if (__state.Active)
      {
        __state.PreviousShootLock = Player.ShootLock;
        Player.ShootLock = true;
      }
    }

    private static void Update_postfix_patch(ref UpdateState __state)
    {
      if (__state.Active)
        Player.ShootLock = __state.PreviousShootLock;
    }

    private static bool HurtBouncedOn_patch(Player __instance, int bouncerIndex)
    {
      if (IsScroll(__instance) && TFModFortRiseScrollModule.Settings.ScrollNoStomp)
        return false; // pas de degat quand on saute sur la tete
      return true;
    }
  }
}
