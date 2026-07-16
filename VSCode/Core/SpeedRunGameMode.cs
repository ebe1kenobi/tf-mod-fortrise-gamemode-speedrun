using FortRise;
using Microsoft.Xna.Framework;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Mode de jeu "boucle scrollante" : le round se joue sur les levels d'un monde
  // Versus concaténés, avec une caméra qui avance automatiquement le long d'une
  // boucle (droite -> bas -> gauche -> haut).
  //
  // Toute classe publique héritant de CustomGameMode est enregistrée
  // automatiquement par FortRise (scan de l'assembly, cf. RiseCore.Register ->
  // GameModeRegistry.Register). Aucun appel d'enregistrement manuel n'est requis.
  //
  // Etape actuelle (fondation) : le mode se comporte comme un Last Man Standing
  // classique sur un level normal. La concaténation des levels et le scrolling
  // caméra seront branchés dans les etapes suivantes (GetLevelSystem +
  // SpeedRunRoundLogic.OnUpdate).
  public class SpeedRun : CustomGameMode
  {
    public override void Initialize()
    {
      Name = "Speed Run";
      Icon = TFGame.MenuAtlas["gameModes/warlord"];
      NameColor = Color.Orange;
      CoinOffset = 12;
    }

    public override void InitializeSounds() { }

    public override RoundLogic CreateRoundLogic(Session session)
    {
      return new SpeedRunRoundLogic(session);
    }

    // IMPORTANT : dans le flux Versus de FortRise, MatchSettings.LevelSystem est
    // deja fixe (VersusLevelSystem standard, depuis la selection de la tour) AVANT
    // que le mode ne demarre, et mode.GetLevelSystem() n'est PAS appele. Le seul
    // point d'accroche fiable est StartGame(), invoque juste avant la creation du
    // LevelLoaderXML. On y remplace le LevelSystem par le notre.
    public override void StartGame(Session session)
    {
      base.StartGame(session);
      if (session.MatchSettings.LevelSystem is VersusLevelSystem vls)
      {
        session.MatchSettings.LevelSystem = new SpeedRunLevelSystem(vls.VersusTowerData);
      }
    }

    // Conserve par coherence (utilise par certains flux FortRise), mais le flux
    // Versus classique passe par StartGame ci-dessus.
    public override LevelSystem GetLevelSystem(LevelData levelData)
    {
      return new SpeedRunLevelSystem(levelData as VersusTowerData);
    }
  }
}
