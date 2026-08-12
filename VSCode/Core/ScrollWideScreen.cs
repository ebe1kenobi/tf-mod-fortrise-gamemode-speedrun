using System;
using System.Xml;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TFModFortRiseScroll
{
  // Option "wide screen" du mode Loop Scroll : elargit la fenetre visible de
  // 320x240 a WIDE_WIDTH x 240 (supprime les bandes noires laterales) PENDANT les
  // rounds du mode uniquement, et restaure 320 au retour au menu.
  //
  // La largeur d'ecran se decide au chargement de chaque round (LevelLoaderXML) :
  // large si le round est en Loop Scroll avec l'option active, normale sinon.
  // Deux ressources creees en 320x240 en dur doivent suivre : le render target
  // du niveau (Level.foregroundRenderTarget) et le canvas d'eclairage
  // (LightingLayer.Canvas, devenu une auto-propriete en FortRise 5 mais toujours
  // accessible par DynamicData sous le meme nom).
  public class ScrollWideScreen : IHookable
  {
    public const int WIDE_WIDTH = 420;
    private const int NORMAL_WIDTH = 320;

    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredConstructor(typeof(LevelLoaderXML), [typeof(Session)]),
          prefix: new HarmonyMethod(LoaderCtor_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredConstructor(typeof(Level), [typeof(Session), typeof(XmlElement)]),
          postfix: new HarmonyMethod(LevelCtor_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Level), nameof(Level.HandleGraphicsDispose)),
          postfix: new HarmonyMethod(LevelHandleGraphicsDispose_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredConstructor(typeof(LightingLayer), [typeof(Color)]),
          postfix: new HarmonyMethod(LightingCtor_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Level), nameof(Level.Render)),
          prefix: new HarmonyMethod(LevelRender_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredConstructor(typeof(MainMenu), [typeof(MainMenu.MenuState)]),
          prefix: new HarmonyMethod(MainMenuCtor_patch)
      );
    }

    // NOTE : ce fichier reparait autrefois Monocle.Screen.Resize, qui ecrivait
    // "this.width = width; this.width = height;" - la hauteur ecrasait la largeur, et
    // le centrage DrawRect.X qui en decoule laissait l'image decalee.
    //
    // FortRise le corrige desormais LUI-MEME, dans l'IL de Resize
    // (MonoModRules.PatchScreenResize, depuis 5.3.3). Garder la reparation par-dessus
    // ne la rendait pas plus juste : elle rejouait le centrage APRES coup, sur tous
    // les appelants - y compris WiderSet - et c'est ce second passage qui laissait
    // l'ecran decale quand les deux mods redimensionnaient. On laisse donc faire
    // FortRise.

    // Le decor (Background) ne couvre que 320px de large ; au-dela le canvas
    // n'est pas nettoye (contenu residuel). On nettoie le render target d'ecran en
    // noir juste avant que le niveau ne se dessine, pour que la zone hors 320 soit
    // propre (fond noir + tuiles du niveau visibles).
    private static void LevelRender_patch(Level __instance)
    {
      if (IsWide)
      {
        Engine.Instance.GraphicsDevice.SetRenderTarget(Engine.Instance.Screen.RenderTarget);
        Engine.Instance.GraphicsDevice.Clear(Color.Black);
      }
    }

    /// <summary>
    /// Vrai des que l'ecran est plus large que la normale, qui que soit celui qui l'a
    /// elargi. C'est la bonne question pour les tampons de rendu : ils doivent suivre
    /// la largeur reelle, pas savoir d'ou elle vient.
    /// </summary>
    internal static bool IsWide
    {
      get
      {
        return Engine.Instance != null
            && Engine.Instance.Screen != null
            && Engine.Instance.Screen.Width != NORMAL_WIDTH;
      }
    }

    /// <summary>
    /// Vrai quand WiderSet tient lui-meme l'ecran en large, c'est-a-dire en mode
    /// 8 joueurs.
    ///
    /// La largeur lui appartient alors, et elle vaut deja 420 - exactement ce que ce
    /// mode veut. Y toucher reviendrait a ecraser son reglage a chaque chargement de
    /// niveau, ce qui etait la raison de neutraliser tout le wide-screen quand
    /// WiderSet etait present. Se retirer sur ce seul point suffit, et les deux modes
    /// cohabitent.
    /// </summary>
    private static bool WiderSetOwnsScreen
    {
      get
      {
        var api = TFModFortRiseScrollModule.WiderSet;
        if (api == null)
        {
          return false;
        }

        try
        {
          return api.IsWide;
        }
        catch
        {
          return false;
        }
      }
    }

    /// <summary>
    /// Vrai quand c'est CE mod qui a elargi l'ecran.
    ///
    /// Sans ce drapeau, "remettre 320" voulait dire "remettre 320 quoi qu'il arrive",
    /// y compris sur un ecran elargi par WiderSet. Les deux mods se reprenaient alors
    /// la largeur a tour de role, chacun recalculant le centrage sur une largeur que
    /// l'autre venait de changer - d'ou l'image decalee. On ne defait plus que ce
    /// qu'on a fait soi-meme.
    /// </summary>
    private static bool widenedByUs;

    private static void RestoreScreen()
    {
      if (!widenedByUs)
      {
        return;
      }

      ResizeScreen(NORMAL_WIDTH);
      widenedByUs = false;
    }

    private static void ResizeScreen(int width)
    {
      Screen screen = Engine.Instance != null ? Engine.Instance.Screen : null;
      if (screen == null || screen.Width == width)
        return;

      Report("avant", screen);

      // Le champ prive et le recentrage sont l'affaire de Resize lui-meme : FortRise
      // en corrige l'IL depuis 5.3.3, il n'y a plus rien a rattraper apres coup.
      screen.Resize(width, 240, screen.Scale);

      Report("apres", screen);
    }

    /// <summary>
    /// Etat de l'ecran de part et d'autre d'un redimensionnement.
    ///
    /// L'image revenait decalee vers la droite au retour au menu, et le centrage
    /// (DrawRect.X) se calcule a partir de trois valeurs qu'on ne peut pas deviner de
    /// l'exterieur : la largeur de fenetre, la largeur mise a l'echelle, et le
    /// viewport. On les ecrit, plutot que de raisonner a l'aveugle.
    /// </summary>
    private static void Report(string when, Screen screen)
    {
      try
      {
        using var data = DynamicData.For(screen);
        var drawRect = (Rectangle)data.Get("DrawRect");
        var viewport = data.Get("viewport");
        int viewportWidth = viewport == null ? -1 : (int)viewport.GetType().GetProperty("Width").GetValue(viewport);

        Logger.Info($"[Ecran] {when} : rendu {screen.Width}x{screen.Height}, echelle {screen.Scale}, "
            + $"mis a l'echelle {screen.ScaledWidth}x{screen.ScaledHeight}, "
            + $"DrawRect.X {drawRect.X} (l={drawRect.Width}), viewport.W {viewportWidth}, "
            + $"plein ecran {screen.IsFullscreen}");
      }
      catch (Exception e)
      {
        Logger.Error("[Ecran] etat illisible : " + e.Message);
      }
    }

    // Largeur decidee au chargement de CHAQUE round : garantit qu'un match dans
    // un autre mode repasse en 320 meme sans retour au menu.
    private static void LoaderCtor_patch(Session session)
    {
      // En mode 8 joueurs, la largeur est celle de WiderSet : on la laisse. Elle vaut
      // deja 420, le round de Speed Run est donc large de toute facon.
      if (WiderSetOwnsScreen)
      {
        return;
      }

      bool wantWide = session != null
                   && ScrollRenderPatches.IsScrollMode(session.MatchSettings)
                   && TFModFortRiseScrollModule.Settings.ScrollWideScreen;
      if (wantWide)
      {
        ResizeScreen(WIDE_WIDTH);
        widenedByUs = true;
        return;
      }

      RestoreScreen();
    }

    // Le render target du niveau est cree en 320x240 en dur : l'elargir.
    private static void LevelCtor_patch(Level __instance)
    {
      ReplaceForegroundTarget(__instance);
    }

    private static void LevelHandleGraphicsDispose_patch(Level __instance)
    {
      ReplaceForegroundTarget(__instance);
    }

    private static void ReplaceForegroundTarget(Level level)
    {
      if (!IsWide)
        return;
      int w = Engine.Instance.Screen.Width;
      var dyn = DynamicData.For(level);
      var rt = dyn.Get<RenderTarget2D>("foregroundRenderTarget");
      if (rt != null && rt.Width != w)
      {
        rt.Dispose();
        dyn.Set("foregroundRenderTarget", new RenderTarget2D(Engine.Instance.GraphicsDevice, w, 240));
      }
      dyn.Dispose();
    }

    // Le canvas d'eclairage est cree en 320x240 en dur : l'elargir (il rend avec
    // la matrice camera, donc la largeur suit toute seule).
    private static void LightingCtor_patch(LightingLayer __instance)
    {
      if (!IsWide)
        return;
      var dyn = DynamicData.For(__instance);
      var old = dyn.Get<Canvas>("Canvas");
      dyn.Set("Canvas", new Canvas(Engine.Instance.Screen.Width, 240));
      dyn.Dispose();
      try { if (old != null) old.Unload(); } catch { }
    }

    // Retour au menu principal : ecran normal.
    private static void MainMenuCtor_patch()
    {
      // WiderSet remet lui-meme 320 en quittant le mode 8 joueurs, depuis ce meme
      // constructeur : le devancer reviendrait a lui reprendre la main.
      if (WiderSetOwnsScreen)
      {
        return;
      }

      RestoreScreen();
    }
  }
}
