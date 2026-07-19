using System.Collections.Generic;
using FortRise;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  public class TFModFortRiseSpeedRunSettings : ModuleSettings
  {
    // Options du mode "shape" et "camera" (etaient des [SettingsOptions] en FR4).
    // NB : SpeedRunCamera garde ses constantes historiques (AutoScroll=0,
    // FollowPlayers=2) ; comme en FR4, l'index de l'option selectionnee (0/1) est
    // stocke tel quel, la vraie UI de reglage etant la popup en jeu (UISpeedRunPopup).
    private static readonly string[] ShapeNames = ["Horizontal", "Square"];
    private static readonly string[] CameraNames = ["Auto scroll", "Follow players"];

    private static string OptionName(string[] names, int index)
    {
      if (index < 0 || index >= names.Length)
        return names[0];
      return names[index];
    }

    public override void Create(ISettingsCreate settings)
    {
      settings.CreateNumber("Speed Run speed (tenths of px/frame)", SpeedRunSpeed, (x) => SpeedRunSpeed = x, 1, 30);
      settings.CreateNumber("Speed Run acceleration (+tenths px/frame)", SpeedRunAccelAmount, (x) => SpeedRunAccelAmount = x, 0, 20);
      settings.CreateNumber("Speed Run acceleration every (s)", SpeedRunAccelEvery, (x) => SpeedRunAccelEvery = x, 1, 60);
      settings.CreateOptions("Speed Run shape", OptionName(ShapeNames, SpeedRunShape), ShapeNames, (x) => SpeedRunShape = x.Item2);
      settings.CreateOnOff("Speed Run goal portal", SpeedRunGoalPortal, (x) => SpeedRunGoalPortal = x);
      settings.CreateNumber("Speed Run laps before goal (square)", SpeedRunLaps, (x) => SpeedRunLaps = x, 1, 10);
      settings.CreateNumber("Speed Run number of levels", SpeedRunMaxLevels, (x) => SpeedRunMaxLevels = x, 2, 30);
      settings.CreateOptions("Speed Run camera", OptionName(CameraNames, SpeedRunCamera), CameraNames, (x) => SpeedRunCamera = x.Item2);
      settings.CreateOnOff("Speed Run leave players behind", SpeedRunLeaveBehind, (x) => SpeedRunLeaveBehind = x);
      settings.CreateNumber("Speed Run offscreen death delay (s)", SpeedRunOffscreenDeathDelay, (x) => SpeedRunOffscreenDeathDelay = x, 1, 15);
      settings.CreateNumber("Speed Run treasure count", SpeedRunTreasureCount, (x) => SpeedRunTreasureCount = x, 0, 20);
      settings.CreateNumber("Speed Run treasure respawn (s)", SpeedRunTreasureRespawn, (x) => SpeedRunTreasureRespawn = x, 0, 60);

      settings.CreateOnOff("SR treasure: arrows", SpeedRunPickupArrows, (x) => SpeedRunPickupArrows = x);
      settings.CreateOnOff("SR treasure: bomb arrows", SpeedRunPickupBombArrows, (x) => SpeedRunPickupBombArrows = x);
      settings.CreateOnOff("SR treasure: super bomb arrows", SpeedRunPickupSuperBombArrows, (x) => SpeedRunPickupSuperBombArrows = x);
      settings.CreateOnOff("SR treasure: laser arrows", SpeedRunPickupLaserArrows, (x) => SpeedRunPickupLaserArrows = x);
      settings.CreateOnOff("SR treasure: bramble arrows", SpeedRunPickupBrambleArrows, (x) => SpeedRunPickupBrambleArrows = x);
      settings.CreateOnOff("SR treasure: drill arrows", SpeedRunPickupDrillArrows, (x) => SpeedRunPickupDrillArrows = x);
      settings.CreateOnOff("SR treasure: bolt arrows", SpeedRunPickupBoltArrows, (x) => SpeedRunPickupBoltArrows = x);
      settings.CreateOnOff("SR treasure: feather arrows", SpeedRunPickupFeatherArrows, (x) => SpeedRunPickupFeatherArrows = x);
      settings.CreateOnOff("SR treasure: trigger arrows", SpeedRunPickupTriggerArrows, (x) => SpeedRunPickupTriggerArrows = x);
      settings.CreateOnOff("SR treasure: prism arrows", SpeedRunPickupPrismArrows, (x) => SpeedRunPickupPrismArrows = x);
      settings.CreateOnOff("SR treasure: shield", SpeedRunPickupShield, (x) => SpeedRunPickupShield = x);
      settings.CreateOnOff("SR treasure: wings", SpeedRunPickupWings, (x) => SpeedRunPickupWings = x);
      settings.CreateOnOff("SR treasure: speed boots", SpeedRunPickupSpeedBoots, (x) => SpeedRunPickupSpeedBoots = x);
      settings.CreateOnOff("SR treasure: mirror", SpeedRunPickupMirror, (x) => SpeedRunPickupMirror = x);
      settings.CreateOnOff("SR treasure: time orb", SpeedRunPickupTimeOrb, (x) => SpeedRunPickupTimeOrb = x);
      settings.CreateOnOff("SR treasure: dark orb", SpeedRunPickupDarkOrb, (x) => SpeedRunPickupDarkOrb = x);
      settings.CreateOnOff("SR treasure: lava orb", SpeedRunPickupLavaOrb, (x) => SpeedRunPickupLavaOrb = x);
      settings.CreateOnOff("SR treasure: space orb", SpeedRunPickupSpaceOrb, (x) => SpeedRunPickupSpaceOrb = x);
      settings.CreateOnOff("SR treasure: chaos orb", SpeedRunPickupChaosOrb, (x) => SpeedRunPickupChaosOrb = x);
      settings.CreateOnOff("SR treasure: bomb", SpeedRunPickupBomb, (x) => SpeedRunPickupBomb = x);

      settings.CreateOnOff("Speed Run same spawn (race)", SpeedRunSameSpawn, (x) => SpeedRunSameSpawn = x);
      settings.CreateOnOff("Speed Run disable arrows", SpeedRunNoArrows, (x) => SpeedRunNoArrows = x);
      settings.CreateOnOff("Speed Run disable head stomp", SpeedRunNoStomp, (x) => SpeedRunNoStomp = x);
      settings.CreateOnOff("Speed Run intro zoom", SpeedRunIntroZoom, (x) => SpeedRunIntroZoom = x);
      settings.CreateOnOff("Speed Run wide screen", SpeedRunWideScreen, (x) => SpeedRunWideScreen = x);
    }

    // Vitesse de defilement du mode Speed Run, en dixiemes de pixel par frame.
    public int SpeedRunSpeed { get; set; } = 10;

    // Acceleration progressive du scroll (dixiemes de px/frame). 0 = desactive.
    public int SpeedRunAccelAmount { get; set; } = 1;
    public int SpeedRunAccelEvery { get; set; } = 10;

    // Forme du parcours : bande horizontale ou anneau carre.
    public const int ShapeHorizontal = 0;
    public const int ShapeSquare = 1;
    public int SpeedRunShape { get; set; } = ShapeSquare;

    // Portail d'arrivee ("trou noir" facon fin de niveau coop).
    public bool SpeedRunGoalPortal { get; set; } = true;
    public int SpeedRunLaps { get; set; } = 3;

    // Nombre max de levels du monde a coller bout a bout.
    public int SpeedRunMaxLevels { get; set; } = 10;

    // Mode camera. NB : FollowPlayers=2 est conserve pour compatibilite meme si
    // l'index d'option stocke ne l'atteint pas (comportement identique a FortRise 4).
    public const int CameraAutoScroll = 0;
    //public const int CameraFollowLeader = 1;
    public const int CameraFollowPlayers = 2;
    public int SpeedRunCamera { get; set; } = CameraAutoScroll;

    // Option 2 : les retardataires sortent de l'ecran et meurent apres N secondes.
    public bool SpeedRunLeaveBehind { get; set; } = false;
    public int SpeedRunOffscreenDeathDelay { get; set; } = 3;

    // Coffres : spawner custom qui remplace le spawner vanilla.
    public int SpeedRunTreasureCount { get; set; } = 5;
    public int SpeedRunTreasureRespawn { get; set; } = 20;

    // Contenu possible des coffres : un on/off par type de pickup du jeu.
    public bool SpeedRunPickupArrows { get; set; } = false;
    public bool SpeedRunPickupBombArrows { get; set; } = false;
    public bool SpeedRunPickupSuperBombArrows { get; set; } = false;
    public bool SpeedRunPickupLaserArrows { get; set; } = false;
    public bool SpeedRunPickupBrambleArrows { get; set; } = false;
    public bool SpeedRunPickupDrillArrows { get; set; } = false;
    public bool SpeedRunPickupBoltArrows { get; set; } = false;
    public bool SpeedRunPickupFeatherArrows { get; set; } = false;
    public bool SpeedRunPickupTriggerArrows { get; set; } = false;
    public bool SpeedRunPickupPrismArrows { get; set; } = true;
    public bool SpeedRunPickupShield { get; set; } = false;
    public bool SpeedRunPickupWings { get; set; } = true;
    public bool SpeedRunPickupSpeedBoots { get; set; } = true;
    public bool SpeedRunPickupMirror { get; set; } = false;
    public bool SpeedRunPickupTimeOrb { get; set; } = false;
    public bool SpeedRunPickupDarkOrb { get; set; } = true;
    public bool SpeedRunPickupLavaOrb { get; set; } = false;
    public bool SpeedRunPickupSpaceOrb { get; set; } = false;
    public bool SpeedRunPickupChaosOrb { get; set; } = false;
    public bool SpeedRunPickupBomb { get; set; } = true;

    // Liste des pickups actives pour le contenu des coffres.
    public List<Pickups> GetEnabledTreasurePickups()
    {
      List<Pickups> list = new List<Pickups>();
      if (SpeedRunPickupArrows) list.Add(Pickups.Arrows);
      if (SpeedRunPickupBombArrows) list.Add(Pickups.BombArrows);
      if (SpeedRunPickupSuperBombArrows) list.Add(Pickups.SuperBombArrows);
      if (SpeedRunPickupLaserArrows) list.Add(Pickups.LaserArrows);
      if (SpeedRunPickupBrambleArrows) list.Add(Pickups.BrambleArrows);
      if (SpeedRunPickupDrillArrows) list.Add(Pickups.DrillArrows);
      if (SpeedRunPickupBoltArrows) list.Add(Pickups.BoltArrows);
      if (SpeedRunPickupFeatherArrows) list.Add(Pickups.FeatherArrows);
      if (SpeedRunPickupTriggerArrows) list.Add(Pickups.TriggerArrows);
      if (SpeedRunPickupPrismArrows) list.Add(Pickups.PrismArrows);
      if (SpeedRunPickupShield) list.Add(Pickups.Shield);
      if (SpeedRunPickupWings) list.Add(Pickups.Wings);
      if (SpeedRunPickupSpeedBoots) list.Add(Pickups.SpeedBoots);
      if (SpeedRunPickupMirror) list.Add(Pickups.Mirror);
      if (SpeedRunPickupTimeOrb) list.Add(Pickups.TimeOrb);
      if (SpeedRunPickupDarkOrb) list.Add(Pickups.DarkOrb);
      if (SpeedRunPickupLavaOrb) list.Add(Pickups.LavaOrb);
      if (SpeedRunPickupSpaceOrb) list.Add(Pickups.SpaceOrb);
      if (SpeedRunPickupChaosOrb) list.Add(Pickups.ChaosOrb);
      if (SpeedRunPickupBomb) list.Add(Pickups.Bomb);
      //if (SpeedRunPickupGem) list.Add(Pickups.Gem);
      return list;
    }

    // Tous les joueurs apparaissent au meme endroit (gauche), facon course.
    public bool SpeedRunSameSpawn { get; set; } = true;

    // Desactive le tir de fleches (donc pas de kill a distance).
    public bool SpeedRunNoArrows { get; set; } = true;

    // Empeche de tuer en sautant sur la tete (stomp).
    public bool SpeedRunNoStomp { get; set; } = true;

    // Intro : vue d'ensemble dezoomee du niveau puis zoom vers le depart.
    public bool SpeedRunIntroZoom { get; set; } = true;

    // Fenetre visible elargie (420x240) pendant les rounds Speed Run.
    public bool SpeedRunWideScreen { get; set; } = true;
  }
}
