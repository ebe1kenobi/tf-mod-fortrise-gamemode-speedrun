namespace TFModFortRiseSpeedRun
{
  // Etat partage du scroll courant, mis a jour chaque frame par SpeedRunRoundLogic
  // et lu par les patches de wrap (SpeedRunWrapPatches) et le clamp.
  //
  // Wrap et mur invisible sont DIRECTIONNELS, derives de la velocite caméra :
  //   - Axe qui bouge (DirX/DirY != 0) -> mur invisible (clamp), pas de wrap.
  //   - Axe immobile                    -> wrap (relatif fenetre camera).
  // Dans un virage arrondi, les deux axes bougent -> clamp sur les deux, pas de wrap.
  internal static class SpeedRunState
  {
    // Coin haut-gauche de la fenetre visible (monde), pour un wrap relatif camera.
    public static float CamX = 0f;
    public static float CamY = 0f;

    // Signe de la velocite camera sur chaque axe (0 si immobile sur cet axe).
    public static int DirX = 1;
    public static int DirY = 0;

    public const float WINDOW_W = 320f;
    public const float WINDOW_H = 240f;

    // L'axe perpendiculaire wrappe seulement si la camera bouge STRICTEMENT sur
    // l'autre axe (pas en diagonale de virage).
    public static bool WrapsHorizontally => DirX == 0 && DirY != 0;
    public static bool WrapsVertically => DirY == 0 && DirX != 0;
  }
}
