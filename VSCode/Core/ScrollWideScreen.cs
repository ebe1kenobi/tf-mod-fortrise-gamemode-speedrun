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

      // Reparation d'un defaut de Monocle, appliquee quel que soit l'appelant et
      // meme si le wide-screen de ce mod est desactive : voir Resize_postfix.
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Screen), nameof(Screen.Resize)),
          postfix: new HarmonyMethod(Resize_postfix)
      );
    }

    /// <summary>
    /// Repare Monocle.Screen.Resize, qui ecrit "this.width = width; this.width =
    /// height;" - height n'est jamais affecte et width est ecrase par la hauteur.
    ///
    /// ScaledWidth en decoule, et avec lui le centrage DrawRect.X calcule dans
    /// HandleWindowedViewport : apres tout redimensionnement, l'image est decalee
    /// horizontalement.
    ///
    /// Ce correctif etait auparavant applique dans ResizeScreen, donc seulement quand
    /// ce mod redimensionnait lui-meme. Or il se met en retrait quand WiderSet est
    /// present (Disabled), et WiderSet appelle Resize de son cote : la combinaison des
    /// deux mods laissait le defaut sans reparation, d'ou l'ecran decale. Le corriger
    /// sur Resize repare tous les appelants, y compris ceux qu'on ne connait pas.
    /// </summary>
    private static void Resize_postfix(Screen __instance, int width, int height)
    {
      var dyn = DynamicData.For(__instance);
      dyn.Set("width", width);
      dyn.Set("height", height);

      // Le centrage a ete calcule avec les mauvaises valeurs : on le refait.
      if (__instance.IsFullscreen)
        __instance.HandleFullscreenViewport();
      else
        dyn.Invoke("SetWindowSize", __instance.ScaledWidth, __instance.ScaledHeight);

      dyn.Dispose();
    }

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

    private static void RestoreScreen()
    {
      if (IsWide)
        ResizeScreen(NORMAL_WIDTH);
    }

    private static void ResizeScreen(int width)
    {
      Screen screen = Engine.Instance != null ? Engine.Instance.Screen : null;
      if (screen == null || screen.Width == width)
        return;

      // La correction des champs prives de Screen et le recalcul du centrage se font
      // maintenant dans Resize_postfix, pour tous les appelants et non seulement ici.
      screen.Resize(width, 240, screen.Scale);
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
                   && ScrollRenderPatches.IsSpeedRunMode(session.MatchSettings)
                   && TFModFortRiseScrollModule.Settings.SpeedRunWideScreen;
      if (wantWide)
        ResizeScreen(WIDE_WIDTH);
      else
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
