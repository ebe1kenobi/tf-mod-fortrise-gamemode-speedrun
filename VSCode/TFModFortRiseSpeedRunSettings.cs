using FortRise;

namespace TFModFortRiseSpeedRun
{
  public class TFModFortRiseSpeedRunSettings : ModuleSettings
  {
    // Vitesse de defilement du mode Speed Run, en dixiemes de pixel par frame
    // (ex: 3 => 0.3 px/frame ≈ 18 px/s a 60 fps). Valeur volontairement lente
    // au depart pour laisser le temps aux joueurs de suivre.
    [SettingsName("Speed Run speed (tenths of px/frame)")]
    [SettingsNumber(1, 30)]
    public int SpeedRunSpeed = 3;

    // Forme du parcours : bande horizontale (scroll droite uniquement) ou anneau
    // carre (droite -> bas -> gauche -> haut, en boucle).
    public const int ShapeHorizontal = 0;
    public const int ShapeSquare = 1;
    [SettingsName("Speed Run shape")]
    [SettingsOptions("Horizontal", "Square")]
    public int SpeedRunShape = ShapeHorizontal;

    // Nombre max de levels du monde a coller bout a bout. Si le monde en a moins,
    // on utilise ce qui est disponible.
    [SettingsName("Speed Run number of levels")]
    [SettingsNumber(2, 30)]
    public int SpeedRunMaxLevels = 10;

    // Option 1 : la camera suit le joueur le plus avance (le "premier" controle
    // la vitesse), au lieu d'un scroll a vitesse fixe.
    [SettingsName("Speed Run camera follows leader")]
    public bool SpeedRunFollowLeader = true;

    // Option 2 : ne plus bloquer le bord arriere -> les retardataires sortent de
    // l'ecran (au lieu d'etre pousses/ecrases) et meurent apres N secondes hors-ecran.
    [SettingsName("Speed Run leave players behind")]
    public bool SpeedRunLeaveBehind = true;

    // Delai (secondes) avant qu'un joueur hors-ecran ne meure (option 2).
    [SettingsName("Speed Run offscreen death delay (s)")]
    [SettingsNumber(1, 15)]
    public int SpeedRunOffscreenDeathDelay = 3;

    // Tous les joueurs apparaissent au meme endroit (gauche), facon course.
    [SettingsName("Speed Run same spawn (race)")]
    public bool SpeedRunSameSpawn = false;

    // Desactive le tir de fleches (donc pas de kill a distance).
    [SettingsName("Speed Run disable arrows")]
    public bool SpeedRunNoArrows = false;

    // Empeche de tuer en sautant sur la tete (stomp).
    [SettingsName("Speed Run disable head stomp")]
    public bool SpeedRunNoStomp = false;

    // Intro : vue d'ensemble dezoomee du niveau puis zoom vers le depart.
    [SettingsName("Speed Run intro zoom")]
    public bool SpeedRunIntroZoom = true;

    // Fenetre visible elargie (420x240 au lieu de 320x240) pendant les rounds
    // Speed Run : supprime les bandes noires laterales.
    [SettingsName("Speed Run wide screen")]
    public bool SpeedRunWideScreen = false;
  }
}
