using System.Xml;
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
  // (LightingLayer.Canvas).
  internal static class LoopScrollWideScreen
  {
    public const int WIDE_WIDTH = 420;
    private const int NORMAL_WIDTH = 320;

    internal static void Load()
    {
      On.TowerFall.LevelLoaderXML.ctor += LoaderCtor_patch;
      On.TowerFall.Level.ctor += LevelCtor_patch;
      On.TowerFall.Level.HandleGraphicsDispose += LevelHandleGraphicsDispose_patch;
      On.TowerFall.LightingLayer.ctor += LightingCtor_patch;
      On.TowerFall.MainMenu.ctor += MainMenuCtor_patch;
    }

    internal static void Unload()
    {
      On.TowerFall.LevelLoaderXML.ctor -= LoaderCtor_patch;
      On.TowerFall.Level.ctor -= LevelCtor_patch;
      On.TowerFall.Level.HandleGraphicsDispose -= LevelHandleGraphicsDispose_patch;
      On.TowerFall.LightingLayer.ctor -= LightingCtor_patch;
      On.TowerFall.MainMenu.ctor -= MainMenuCtor_patch;
      RestoreScreen();
    }

    internal static bool IsWide
    {
      get
      {
        return Engine.Instance != null
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
    private static void LoaderCtor_patch(On.TowerFall.LevelLoaderXML.orig_ctor orig, LevelLoaderXML self, Session session)
    {
      bool wantWide = session != null
                   && session.MatchSettings != null
                   && session.MatchSettings.IsCustom
                   && session.MatchSettings.CurrentModeName == nameof(LoopScroll)
                   && TFModFortRiseSpeedRunModule.Settings.loopScrollWideScreen;
      if (wantWide)
        ResizeScreen(WIDE_WIDTH);
      else
        RestoreScreen();
      orig(self, session);
    }

    // Le render target du niveau est cree en 320x240 en dur : l'elargir.
    private static void LevelCtor_patch(On.TowerFall.Level.orig_ctor orig, Level self, Session session, XmlElement xml)
    {
      orig(self, session, xml);
      ReplaceForegroundTarget(self);
    }

    private static void LevelHandleGraphicsDispose_patch(On.TowerFall.Level.orig_HandleGraphicsDispose orig, Level self)
    {
      orig(self);
      ReplaceForegroundTarget(self);
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
    private static void LightingCtor_patch(On.TowerFall.LightingLayer.orig_ctor orig, LightingLayer self, Color clearColor)
    {
      orig(self, clearColor);
      if (!IsWide)
        return;
      var dyn = DynamicData.For(self);
      var old = dyn.Get<Canvas>("Canvas");
      dyn.Set("Canvas", new Canvas(Engine.Instance.Screen.Width, 240));
      dyn.Dispose();
      try { if (old != null) old.Unload(); } catch { }
    }

    // Retour au menu principal : ecran normal.
    private static void MainMenuCtor_patch(On.TowerFall.MainMenu.orig_ctor orig, MainMenu self, MainMenu.MenuState state)
    {
      RestoreScreen();
      orig(self, state);
    }
  }
}
