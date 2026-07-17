using System.Collections.Generic;
using FortRise;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  public class TFModFortRiseSpeedRunSettings : ModuleSettings
  {
    // Vitesse de defilement du mode Speed Run, en dixiemes de pixel par frame
    // (ex: 3 => 0.3 px/frame ≈ 18 px/s a 60 fps). Valeur volontairement lente
    // au depart pour laisser le temps aux joueurs de suivre.
    [SettingsName("Speed Run speed (tenths of px/frame)")]
    [SettingsNumber(1, 30)]
    public int SpeedRunSpeed = 10;

    // Acceleration progressive du scroll : toutes les SpeedRunAccelEvery
    // secondes, la vitesse augmente de SpeedRunAccelAmount (dixiemes de
    // px/frame). 0 = desactive. S'applique a l'auto scroll et au follow leader
    // (ou SpeedRunSpeed est la vitesse plancher). La vitesse effective est
    // plafonnee a 6 px/frame (meme plafond que le suivi leader).
    [SettingsName("Speed Run acceleration (+tenths px/frame)")]
    [SettingsNumber(0, 20)]
    public int SpeedRunAccelAmount = 1;

    [SettingsName("Speed Run acceleration every (s)")]
    [SettingsNumber(1, 60)]
    public int SpeedRunAccelEvery = 10;

    // Forme du parcours : bande horizontale (scroll droite uniquement) ou anneau
    // carre (droite -> bas -> gauche -> haut, en boucle).
    public const int ShapeHorizontal = 0;
    public const int ShapeSquare = 1;
    [SettingsName("Speed Run shape")]
    [SettingsOptions("Horizontal", "Square")]
    public int SpeedRunShape = ShapeSquare;

    // Portail d'arrivee ("trou noir" facon fin de niveau coop) : le premier
    // joueur qui saute dedans gagne le round, les autres meurent. En HORIZONTAL
    // il apparait au bout du parcours ; en SQUARE au bout de SpeedRunLaps tours
    // (modes scroll uniquement — pas de notion de tour en follow players).
    [SettingsName("Speed Run goal portal")]
    public bool SpeedRunGoalPortal = true;

    [SettingsName("Speed Run laps before goal (square)")]
    [SettingsNumber(1, 10)]
    public int SpeedRunLaps = 3;

    // Nombre max de levels du monde a coller bout a bout. Si le monde en a moins,
    // on utilise ce qui est disponible.
    [SettingsName("Speed Run number of levels")]
    [SettingsNumber(2, 30)]
    public int SpeedRunMaxLevels = 10;

    // Mode camera (3 modes mutuellement exclusifs) :
    //   - AutoScroll    : scroll a vitesse fixe (SpeedRunSpeed).
    //   - FollowLeader  : scroll pilote par le joueur le plus avance
    //                     (SpeedRunSpeed = vitesse plancher).
    //   - FollowPlayers : plus de scroll automatique du tout. La camera suit les
    //     mouvements du groupe (elle vise le centre de leur boite englobante) :
    //     si tout le monde va a droite, l'ecran se decale a droite, etc. Si les
    //     joueurs sont ecartes de la taille de l'ecran, la camera ne bouge plus
    //     et des murs invisibles aux bords empechent de sortir de l'ecran.
    //     SpeedRunSpeed / SpeedRunLeaveBehind / SpeedRunOffscreenDeathDelay sont
    //     alors sans effet.
    public const int CameraAutoScroll = 0;
    //public const int CameraFollowLeader = 1;
    public const int CameraFollowPlayers = 2;
    [SettingsName("Speed Run camera")]
    [SettingsOptions("Auto scroll",  "Follow players")] ///"Follow leader",
    public int SpeedRunCamera = CameraAutoScroll;

    // Option 2 : ne plus bloquer le bord arriere -> les retardataires sortent de
    // l'ecran (au lieu d'etre pousses/ecrases) et meurent apres N secondes hors-ecran.
    [SettingsName("Speed Run leave players behind")]
    public bool SpeedRunLeaveBehind = false;

    // Delai (secondes) avant qu'un joueur hors-ecran ne meure (option 2).
    [SettingsName("Speed Run offscreen death delay (s)")]
    [SettingsNumber(1, 15)]
    public int SpeedRunOffscreenDeathDelay = 3;

    // Coffres : spawner custom qui remplace le spawner vanilla. Les positions
    // respectent les emplacements TreasureChest/BigTreasureChest definis dans
    // les XML des levels sources (Content/Levels/Versus), le contenu est pioche
    // au hasard parmi les pickups actives ci-dessous.
    [SettingsName("Speed Run treasure count")]
    [SettingsNumber(0, 20)]
    public int SpeedRunTreasureCount = 5;

    // Respawn d'un coffre N secondes apres son ouverture (0 = pas de respawn).
    // Surtout utile en SQUARE ou l'on repasse au meme endroit a chaque tour.
    [SettingsName("Speed Run treasure respawn (s)")]
    [SettingsNumber(0, 60)]
    public int SpeedRunTreasureRespawn = 20;

    // Contenu possible des coffres : un on/off par type de pickup du jeu.
    [SettingsName("SR treasure: arrows")] public bool SpeedRunPickupArrows = false;
    [SettingsName("SR treasure: bomb arrows")] public bool SpeedRunPickupBombArrows = false;
    [SettingsName("SR treasure: super bomb arrows")] public bool SpeedRunPickupSuperBombArrows = false;
    [SettingsName("SR treasure: laser arrows")] public bool SpeedRunPickupLaserArrows = false;
    [SettingsName("SR treasure: bramble arrows")] public bool SpeedRunPickupBrambleArrows = false;
    [SettingsName("SR treasure: drill arrows")] public bool SpeedRunPickupDrillArrows = false;
    [SettingsName("SR treasure: bolt arrows")] public bool SpeedRunPickupBoltArrows = false;
    [SettingsName("SR treasure: feather arrows")] public bool SpeedRunPickupFeatherArrows = false;
    [SettingsName("SR treasure: trigger arrows")] public bool SpeedRunPickupTriggerArrows = false;
    [SettingsName("SR treasure: prism arrows")] public bool SpeedRunPickupPrismArrows = true;
    [SettingsName("SR treasure: shield")] public bool SpeedRunPickupShield = false;
    [SettingsName("SR treasure: wings")] public bool SpeedRunPickupWings = true;
    [SettingsName("SR treasure: speed boots")] public bool SpeedRunPickupSpeedBoots = true;
    [SettingsName("SR treasure: mirror")] public bool SpeedRunPickupMirror = false;
    [SettingsName("SR treasure: time orb")] public bool SpeedRunPickupTimeOrb = false;
    [SettingsName("SR treasure: dark orb")] public bool SpeedRunPickupDarkOrb = true;
    [SettingsName("SR treasure: lava orb")] public bool SpeedRunPickupLavaOrb = false;
    [SettingsName("SR treasure: space orb")] public bool SpeedRunPickupSpaceOrb = false;
    [SettingsName("SR treasure: chaos orb")] public bool SpeedRunPickupChaosOrb = false;
    [SettingsName("SR treasure: bomb")] public bool SpeedRunPickupBomb = true;

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
    [SettingsName("Speed Run same spawn (race)")]
    public bool SpeedRunSameSpawn = true;

    // Desactive le tir de fleches (donc pas de kill a distance).
    [SettingsName("Speed Run disable arrows")]
    public bool SpeedRunNoArrows = true;

    // Empeche de tuer en sautant sur la tete (stomp).
    [SettingsName("Speed Run disable head stomp")]
    public bool SpeedRunNoStomp = true;

    // Intro : vue d'ensemble dezoomee du niveau puis zoom vers le depart.
    [SettingsName("Speed Run intro zoom")]
    public bool SpeedRunIntroZoom = true;

    // Fenetre visible elargie (420x240 au lieu de 320x240) pendant les rounds
    // Speed Run : supprime les bandes noires laterales.
    [SettingsName("Speed Run wide screen")]
    public bool SpeedRunWideScreen = true;
  }
}
