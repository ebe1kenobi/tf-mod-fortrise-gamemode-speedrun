using System.Xml;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TFModFortRiseSpeedRun
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
  public class SpeedRunWideScreen : IHookable
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

    // Neutralise entierement le wide-screen (mis a true si WiderSet est present,
    // pour ne pas entrer en conflit avec son propre redimensionnement d'ecran).
    // Comme tous les patches sont gardes par IsWide, ce flag les rend tous inertes.
    internal static bool Disabled;

    internal static bool IsWide
    {
      get
      {
        return !Disabled
            && Engine.Instance != null
            && Engine.Instance.Screen != null
            && Engine.Instance.Screen.Width != NORMAL_WIDTH;
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

      screen.Resize(width, 240, screen.Scale);
      // Le Resize vanilla ne met pas correctement a jour ses champs prives
      // width/height (this.width est ecrase deux fois) : on les corrige puis on
      // refait le calcul de centrage qui en depend (ScaledWidth).
      var dyn = DynamicData.For(screen);
      dyn.Set("width", width);
      dyn.Set("height", 240);
      if (screen.IsFullscreen)
        screen.HandleFullscreenViewport();
      else
        dyn.Invoke("SetWindowSize", screen.ScaledWidth, screen.ScaledHeight);
      dyn.Dispose();
    }

    // Largeur decidee au chargement de CHAQUE round : garantit qu'un match dans
    // un autre mode repasse en 320 meme sans retour au menu.
    private static void LoaderCtor_patch(Session session)
    {
      bool wantWide = session != null
                   && SpeedRunRenderPatches.IsSpeedRunMode(session.MatchSettings)
                   && TFModFortRiseSpeedRunModule.Settings.SpeedRunWideScreen;
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
      RestoreScreen();
    }
  }
}
