using Microsoft.Xna.Framework;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Mur solide INVISIBLE place aux bords de la fenetre camera en mode
  // "follow players". Contrairement au simple clamp de position, une vraie
  // entite taggee Solid donne au joueur toute la physique normale contre le
  // bord de l'ecran : wall-jump / rebond sur les cotes, etat "au sol" (donc
  // saut possible) quand le bord bas le retient au-dessus d'un trou.
  // Repositionne chaque frame par SpeedRunRoundLogic ; le haut de l'ecran
  // reste ouvert (un saut au-dessus retombe tout seul).
  internal class SpeedRunEdgeWall : Solid
  {
    public SpeedRunEdgeWall(int width, int height) : base(Vector2.Zero, width, height, false)
    {
      Visible = false;
    }
  }
}
