using System.Collections.Generic;
using FortRise;
using TowerFall;

namespace TFModFortRiseScroll
{
  public class TFModFortRiseScrollSettings : ModuleSettings
  {
    // Options du mode "shape" et "camera" (etaient des [SettingsOptions] en FR4).
    // NB : ScrollCamera garde ses constantes historiques (AutoScroll=0,
    // FollowPlayers=2) ; comme en FR4, l'index de l'option selectionnee (0/1) est
    // stocke tel quel, la vraie UI de reglage etant la popup en jeu (UIScrollPopup).
    private static readonly string[] ShapeNames = ["Horizontal", "Square"];
    private static readonly string[] CameraNames = ["Auto scroll", "Follow players"];

    private static string OptionName(string[] names, int index)
    {
      if (index < 0 || index >= names.Length)
        return names[0];
      return names[index];
    }

    // FortRise n'ecrit les reglages qu'en SORTANT du menu Options
    // (MainMenu.DestroyOptions) : quitter le jeu depuis ce menu perdait la
    // modification. Chaque changement declenche donc une sauvegarde immediate.
    public override void Create(ISettingsCreate settings)
    {
      settings.CreateNumber("speed (tenths of px/frame)", ScrollSpeed, (x) => { ScrollSpeed = x; TFModFortRiseScrollModule.SaveSettingsNow(); }, 1, 30);
      settings.CreateNumber("acceleration (+tenths px/frame)", ScrollAccelAmount, (x) => { ScrollAccelAmount = x; TFModFortRiseScrollModule.SaveSettingsNow(); }, 0, 20);
      settings.CreateNumber("acceleration every (s)", ScrollAccelEvery, (x) => { ScrollAccelEvery = x; TFModFortRiseScrollModule.SaveSettingsNow(); }, 1, 60);
      settings.CreateOptions("shape", OptionName(ShapeNames, ScrollShape), ShapeNames, (x) => { ScrollShape = x.Item2; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("goal portal", ScrollGoalPortal, (x) => { ScrollGoalPortal = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateNumber("laps before goal (square)", ScrollLaps, (x) => { ScrollLaps = x; TFModFortRiseScrollModule.SaveSettingsNow(); }, 1, 10);
      settings.CreateNumber("number of levels", ScrollMaxLevels, (x) => { ScrollMaxLevels = x; TFModFortRiseScrollModule.SaveSettingsNow(); }, 2, 30);
      settings.CreateOptions("camera", OptionName(CameraNames, ScrollCamera), CameraNames, (x) => { ScrollCamera = x.Item2; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("leave players behind", ScrollLeaveBehind, (x) => { ScrollLeaveBehind = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateNumber("offscreen death delay (s)", ScrollOffscreenDeathDelay, (x) => { ScrollOffscreenDeathDelay = x; TFModFortRiseScrollModule.SaveSettingsNow(); }, 1, 15);
      settings.CreateNumber("treasure count", ScrollTreasureCount, (x) => { ScrollTreasureCount = x; TFModFortRiseScrollModule.SaveSettingsNow(); }, 0, 20);
      settings.CreateNumber("treasure respawn (s)", ScrollTreasureRespawn, (x) => { ScrollTreasureRespawn = x; TFModFortRiseScrollModule.SaveSettingsNow(); }, 0, 60);

      settings.CreateOnOff("SR treasure: arrows", ScrollPickupArrows, (x) => { ScrollPickupArrows = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: bomb arrows", ScrollPickupBombArrows, (x) => { ScrollPickupBombArrows = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: super bomb arrows", ScrollPickupSuperBombArrows, (x) => { ScrollPickupSuperBombArrows = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: laser arrows", ScrollPickupLaserArrows, (x) => { ScrollPickupLaserArrows = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: bramble arrows", ScrollPickupBrambleArrows, (x) => { ScrollPickupBrambleArrows = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: drill arrows", ScrollPickupDrillArrows, (x) => { ScrollPickupDrillArrows = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: bolt arrows", ScrollPickupBoltArrows, (x) => { ScrollPickupBoltArrows = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: feather arrows", ScrollPickupFeatherArrows, (x) => { ScrollPickupFeatherArrows = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: trigger arrows", ScrollPickupTriggerArrows, (x) => { ScrollPickupTriggerArrows = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: prism arrows", ScrollPickupPrismArrows, (x) => { ScrollPickupPrismArrows = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: shield", ScrollPickupShield, (x) => { ScrollPickupShield = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: wings", ScrollPickupWings, (x) => { ScrollPickupWings = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: speed boots", ScrollPickupSpeedBoots, (x) => { ScrollPickupSpeedBoots = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: mirror", ScrollPickupMirror, (x) => { ScrollPickupMirror = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: time orb", ScrollPickupTimeOrb, (x) => { ScrollPickupTimeOrb = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: dark orb", ScrollPickupDarkOrb, (x) => { ScrollPickupDarkOrb = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: lava orb", ScrollPickupLavaOrb, (x) => { ScrollPickupLavaOrb = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: space orb", ScrollPickupSpaceOrb, (x) => { ScrollPickupSpaceOrb = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: chaos orb", ScrollPickupChaosOrb, (x) => { ScrollPickupChaosOrb = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("SR treasure: bomb", ScrollPickupBomb, (x) => { ScrollPickupBomb = x; TFModFortRiseScrollModule.SaveSettingsNow(); });

      settings.CreateOnOff("same spawn (race)", ScrollSameSpawn, (x) => { ScrollSameSpawn = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("disable arrows", ScrollNoArrows, (x) => { ScrollNoArrows = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("disable head stomp", ScrollNoStomp, (x) => { ScrollNoStomp = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
      settings.CreateOnOff("intro zoom", ScrollIntroZoom, (x) => { ScrollIntroZoom = x; TFModFortRiseScrollModule.SaveSettingsNow(); });
    }

    // Vitesse de defilement du mode Scroll, en dixiemes de pixel par frame.
    public int ScrollSpeed { get; set; } = 10;

    // Acceleration progressive du scroll (dixiemes de px/frame). 0 = desactive.
    public int ScrollAccelAmount { get; set; } = 1;
    public int ScrollAccelEvery { get; set; } = 10;

    // Forme du parcours : bande horizontale ou anneau carre.
    public const int ShapeHorizontal = 0;
    public const int ShapeSquare = 1;
    public int ScrollShape { get; set; } = ShapeSquare;

    // Portail d'arrivee ("trou noir" facon fin de niveau coop).
    public bool ScrollGoalPortal { get; set; } = true;
    public int ScrollLaps { get; set; } = 3;

    // Nombre max de levels du monde a coller bout a bout.
    public int ScrollMaxLevels { get; set; } = 4;

    // Mode camera. NB : FollowPlayers=2 est conserve pour compatibilite meme si
    // l'index d'option stocke ne l'atteint pas (comportement identique a FortRise 4).
    public const int CameraAutoScroll = 0;
    //public const int CameraFollowLeader = 1;
    public const int CameraFollowPlayers = 2;
    public int ScrollCamera { get; set; } = CameraFollowPlayers;

    // Option 2 : les retardataires sortent de l'ecran et meurent apres N secondes.
    public bool ScrollLeaveBehind { get; set; } = false;
    public int ScrollOffscreenDeathDelay { get; set; } = 3;

    // Coffres : spawner custom qui remplace le spawner vanilla.
    public int ScrollTreasureCount { get; set; } = 5;
    public int ScrollTreasureRespawn { get; set; } = 20;

    // Contenu possible des coffres : un on/off par type de pickup du jeu.
    public bool ScrollPickupArrows { get; set; } = false;
    public bool ScrollPickupBombArrows { get; set; } = false;
    public bool ScrollPickupSuperBombArrows { get; set; } = false;
    public bool ScrollPickupLaserArrows { get; set; } = false;
    public bool ScrollPickupBrambleArrows { get; set; } = false;
    public bool ScrollPickupDrillArrows { get; set; } = false;
    public bool ScrollPickupBoltArrows { get; set; } = false;
    public bool ScrollPickupFeatherArrows { get; set; } = false;
    public bool ScrollPickupTriggerArrows { get; set; } = false;
    public bool ScrollPickupPrismArrows { get; set; } = true;
    public bool ScrollPickupShield { get; set; } = false;
    public bool ScrollPickupWings { get; set; } = true;
    public bool ScrollPickupSpeedBoots { get; set; } = true;
    public bool ScrollPickupMirror { get; set; } = false;
    public bool ScrollPickupTimeOrb { get; set; } = false;
    public bool ScrollPickupDarkOrb { get; set; } = true;
    public bool ScrollPickupLavaOrb { get; set; } = false;
    public bool ScrollPickupSpaceOrb { get; set; } = false;
    public bool ScrollPickupChaosOrb { get; set; } = false;
    public bool ScrollPickupBomb { get; set; } = true;

    // Liste des pickups actives pour le contenu des coffres.
    public List<Pickups> GetEnabledTreasurePickups()
    {
      List<Pickups> list = new List<Pickups>();
      if (ScrollPickupArrows) list.Add(Pickups.Arrows);
      if (ScrollPickupBombArrows) list.Add(Pickups.BombArrows);
      if (ScrollPickupSuperBombArrows) list.Add(Pickups.SuperBombArrows);
      if (ScrollPickupLaserArrows) list.Add(Pickups.LaserArrows);
      if (ScrollPickupBrambleArrows) list.Add(Pickups.BrambleArrows);
      if (ScrollPickupDrillArrows) list.Add(Pickups.DrillArrows);
      if (ScrollPickupBoltArrows) list.Add(Pickups.BoltArrows);
      if (ScrollPickupFeatherArrows) list.Add(Pickups.FeatherArrows);
      if (ScrollPickupTriggerArrows) list.Add(Pickups.TriggerArrows);
      if (ScrollPickupPrismArrows) list.Add(Pickups.PrismArrows);
      if (ScrollPickupShield) list.Add(Pickups.Shield);
      if (ScrollPickupWings) list.Add(Pickups.Wings);
      if (ScrollPickupSpeedBoots) list.Add(Pickups.SpeedBoots);
      if (ScrollPickupMirror) list.Add(Pickups.Mirror);
      if (ScrollPickupTimeOrb) list.Add(Pickups.TimeOrb);
      if (ScrollPickupDarkOrb) list.Add(Pickups.DarkOrb);
      if (ScrollPickupLavaOrb) list.Add(Pickups.LavaOrb);
      if (ScrollPickupSpaceOrb) list.Add(Pickups.SpaceOrb);
      if (ScrollPickupChaosOrb) list.Add(Pickups.ChaosOrb);
      if (ScrollPickupBomb) list.Add(Pickups.Bomb);
      //if (ScrollPickupGem) list.Add(Pickups.Gem);
      return list;
    }

    // Tous les joueurs apparaissent au meme endroit (gauche), facon course.
    public bool ScrollSameSpawn { get; set; } = true;

    // Desactive le tir de fleches (donc pas de kill a distance).
    public bool ScrollNoArrows { get; set; } = true;

    // Empeche de tuer en sautant sur la tete (stomp).
    public bool ScrollNoStomp { get; set; } = true;

    // Intro : vue d'ensemble dezoomee du niveau puis zoom vers le depart.
    public bool ScrollIntroZoom { get; set; } = true;

    // NOTE : l'option "wide screen" a ete retiree. Elargir la fenetre est le metier
    // du mod WiderSet, qui le fait pour tout le jeu et bien plus complement que ce
    // mod ne le faisait pour ses seules manches : pour jouer Scroll en large, on
    // active le mode de WiderSet. Deux mods qui se disputaient la largeur de l'ecran
    // se marchaient dessus, et le centrage de l'image en faisait les frais.
  }
}
