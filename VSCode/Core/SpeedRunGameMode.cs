using FortRise;
using Microsoft.Xna.Framework;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Mode de jeu "boucle scrollante" : le round se joue sur les levels d'un monde
  // Versus concaténés, avec une caméra qui avance automatiquement le long d'une
  // boucle (droite -> bas -> gauche -> haut).
  //
  // FortRise 4 : classe publique héritant de CustomGameMode, enregistrée
  // automatiquement par scan d'assembly.
  // FortRise 5 : implémentation de IVersusGameMode + enregistrement explicite via
  // registry.GameModes.RegisterVersusGameMode. L'entrée retournée porte la valeur
  // Modes qui sert à reconnaître le mode (SpeedRunEntry.Modes).
  //
  // Etape actuelle (fondation) : le mode se comporte comme un Last Man Standing
  // classique sur un level normal. La concaténation des levels et le scrolling
  // caméra sont branchés par SpeedRunLevelSystem + SpeedRunRoundLogic.
  public class SpeedRun : IVersusGameMode, IRegisterable
  {
    private static ISubtextureEntry SpeedRunIcon { get; set; } = null!;
    public static IVersusGameModeEntry SpeedRunEntry { get; private set; } = null!;

    public string Name => "Speed Run";
    public Color NameColor => Color.Orange;
    public ISubtextureEntry Icon => SpeedRunIcon;
    public bool IsTeamMode => false;

    public static void Register(IModContent content, IModRegistry registry)
    {
      // Pas de texture embarquée : on réutilise l'icône vanilla (comme en FR4).
      // Callback résolu paresseusement, une fois les atlas chargés.
      SpeedRunIcon = registry.Subtextures.RegisterTexture(
          "gameModes/speedRun",
          () => TFGame.MenuAtlas["gameModes/warlord"],
          SubtextureAtlasDestination.MenuAtlas
      );

      SpeedRunEntry = registry.GameModes.RegisterVersusGameMode(new SpeedRun());
    }

    public int OverrideCoinOffset(Session session)
    {
      return 12;
    }

    // FortRise 4 surchargeait StartGame() pour remplacer le LevelSystem. En FR5 le
    // point d'accroche est OnStartGame, invoqué juste avant la création du
    // LevelLoaderXML : on y remplace le VersusLevelSystem par le nôtre. (Il n'y a
    // plus de GetLevelSystem override dans IVersusGameMode, et le flux Versus ne
    // l'appelait de toute façon pas.)
    public void OnStartGame(Session session)
    {
      if (session.MatchSettings.LevelSystem is VersusLevelSystem vls)
      {
        session.MatchSettings.LevelSystem = new SpeedRunLevelSystem(vls.VersusTowerData);
      }
    }

    public RoundLogic OnCreateRoundLogic(Session session)
    {
      return new SpeedRunRoundLogic(session);
    }
  }
}
