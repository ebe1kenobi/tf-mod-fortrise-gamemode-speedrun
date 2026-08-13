using FortRise;
using Microsoft.Xna.Framework;
using TowerFall;

namespace TFModFortRiseScroll
{
  // Mode de jeu "boucle scrollante" : le round se joue sur les levels d'un monde
  // Versus concaténés, avec une caméra qui avance automatiquement le long d'une
  // boucle (droite -> bas -> gauche -> haut).
  //
  // FortRise 4 : classe publique héritant de CustomGameMode, enregistrée
  // automatiquement par scan d'assembly.
  // FortRise 5 : implémentation de IVersusGameMode + enregistrement explicite via
  // registry.GameModes.RegisterVersusGameMode. L'entrée retournée porte la valeur
  // Modes qui sert à reconnaître le mode (ScrollEntry.Modes).
  //
  // Etape actuelle (fondation) : le mode se comporte comme un Last Man Standing
  // classique sur un level normal. La concaténation des levels et le scrolling
  // caméra sont branchés par ScrollLevelSystem + ScrollRoundLogic.
  public class Scroll : IVersusGameMode, IRegisterable
  {
    private static ISubtextureEntry ScrollIcon { get; set; } = null!;
    public static IVersusGameModeEntry ScrollEntry { get; private set; } = null!;

    public string Name => "Scroll";
    public Color NameColor => Color.Orange;
    public ISubtextureEntry Icon => ScrollIcon;
    public bool IsTeamMode => false;

    public static void Register(IModContent content, IModRegistry registry)
    {
      // Icone propre au mode, aux dimensions des quatre du jeu (184x82) et dans leur
      // style : une tour de quatre salles et l'ecran qui s'y promene. Elle remplace
      // l'emprunt a "warlord", qui montrait une tete a cornes sans rapport - deux
      // modes avec la meme image ne se distinguent pas dans la liste.
      ScrollIcon = registry.Subtextures.RegisterTexture(
          content.Root.GetRelativePath("Content/Atlas/gamemode.png")
      );

      ScrollEntry = registry.GameModes.RegisterVersusGameMode(new Scroll());
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
        session.MatchSettings.LevelSystem = new ScrollLevelSystem(vls.VersusTowerData);
      }
    }

    public RoundLogic OnCreateRoundLogic(Session session)
    {
      return new ScrollRoundLogic(session);
    }
  }
}
