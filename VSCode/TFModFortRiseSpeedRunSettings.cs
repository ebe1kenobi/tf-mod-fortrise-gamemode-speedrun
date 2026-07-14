using FortRise;

namespace TFModFortRiseSpeedRun
{
  public class TFModFortRiseSpeedRunSettings : ModuleSettings
  {
    // Vitesse de defilement du mode Loop Scroll, en dixiemes de pixel par frame
    // (ex: 3 => 0.3 px/frame ≈ 18 px/s a 60 fps). Valeur volontairement lente
    // au depart pour laisser le temps aux joueurs de suivre.
    [SettingsName("Loop Scroll speed (tenths of px/frame)")]
    [SettingsNumber(1, 30)]
    public int loopScrollSpeed = 3;

    // Forme du parcours : bande horizontale (scroll droite uniquement) ou anneau
    // carre (droite -> bas -> gauche -> haut, en boucle).
    public const int ShapeHorizontal = 0;
    public const int ShapeSquare = 1;
    [SettingsName("Loop Scroll shape")]
    [SettingsOptions("Horizontal", "Square")]
    public int loopScrollShape = ShapeHorizontal;

    // Nombre max de levels du monde a coller bout a bout. Si le monde en a moins,
    // on utilise ce qui est disponible.
    [SettingsName("Loop Scroll number of levels")]
    [SettingsNumber(2, 30)]
    public int loopScrollMaxLevels = 10;

    // Option 1 : la camera suit le joueur le plus avance (le "premier" controle
    // la vitesse), au lieu d'un scroll a vitesse fixe.
    [SettingsName("Loop Scroll camera follows leader")]
    public bool loopScrollFollowLeader = true;

    // Option 2 : ne plus bloquer le bord arriere -> les retardataires sortent de
    // l'ecran (au lieu d'etre pousses/ecrases) et meurent apres N secondes hors-ecran.
    [SettingsName("Loop Scroll leave players behind")]
    public bool loopScrollLeaveBehind = true;

    // Delai (secondes) avant qu'un joueur hors-ecran ne meure (option 2).
    [SettingsName("Loop Scroll offscreen death delay (s)")]
    [SettingsNumber(1, 15)]
    public int loopScrollOffscreenDeathDelay = 3;

    // Tous les joueurs apparaissent au meme endroit (gauche), facon course.
    [SettingsName("Loop Scroll same spawn (race)")]
    public bool loopScrollSameSpawn = false;

    // Desactive le tir de fleches (donc pas de kill a distance).
    [SettingsName("Loop Scroll disable arrows")]
    public bool loopScrollNoArrows = false;

    // Empeche de tuer en sautant sur la tete (stomp).
    [SettingsName("Loop Scroll disable head stomp")]
    public bool loopScrollNoStomp = false;

    // Intro : vue d'ensemble dezoomee du niveau puis zoom vers le depart.
    [SettingsName("Loop Scroll intro zoom")]
    public bool loopScrollIntroZoom = true;

    // Fenetre visible elargie (420x240 au lieu de 320x240) pendant les rounds
    // Loop Scroll : supprime les bandes noires laterales.
    [SettingsName("Loop Scroll wide screen")]
    public bool loopScrollWideScreen = false;
  }
}
