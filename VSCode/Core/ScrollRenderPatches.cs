using System;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TFModFortRiseScroll
{
  // Patches permettant a un level plus grand que 32x24 tuiles de se charger et se
  // rendre. Tout est scope au mode Loop Scroll (sinon on laisse l'original).
  //
  // Le loader (LevelLoaderXML.Load) et LevelTiles/LevelBGTiles codent en dur les
  // dimensions 32x24 et la taille du Tilemap (320x240 px). On les redirige vers
  // les vraies dimensions du niveau combine (ScrollLevelSystem.WidthTiles/HeightTiles).
  public class ScrollRenderPatches : IHookable
  {
    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Calc), nameof(Calc.GetBitData)),
          prefix: new HarmonyMethod(GetBitData_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Calc), nameof(Calc.ReadCSVIntGrid)),
          prefix: new HarmonyMethod(ReadCSVIntGrid_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredConstructor(typeof(Tilemap), [typeof(int), typeof(int)]),
          prefix: new HarmonyMethod(Tilemap_ctor_patch)
      );

      // Les deux porteurs de tuiles creent leur tilemap en 320x240 dans Added().
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(LevelTiles), nameof(LevelTiles.Added)),
          postfix: new HarmonyMethod(TilesAdded_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(LevelBGTiles), nameof(LevelBGTiles.Added)),
          postfix: new HarmonyMethod(TilesAdded_patch)
      );

      // Le rechargement a chaud de FortRise relit le fichier .oel et rebatit les
      // tuiles en 32x24 EN DUR (patch_Level.OnFileChanged). Sur un niveau combine
      // cela ne peut etre que faux : aucun fichier ne le decrit, et ses dimensions ne
      // sont pas celles d'un bloc. On neutralise donc le rechargement pendant une
      // manche de ce mode.
      //
      // Methode privee, d'ou le nom en dur ; absente d'une version future, on ne
      // patche rien et le mod tourne comme avant.
      var onFileChanged = AccessTools.DeclaredMethod(typeof(Level), "OnFileChanged");
      if (onFileChanged != null)
      {
        harmony.Patch(onFileChanged, prefix: new HarmonyMethod(SkipInScroll_patch));
      }

      // On patche les Play(panX,volume) plutot que SFX.CalculatePan : cette derniere
      // est un simple MathHelper.Lerp que le JIT inline dans ses appelants, si bien
      // qu'un patch sur la methode autonome ne toucherait pas les copies inlinees
      // (d'ou un crash set_Pan hors [-1,1] persistant). En corrigeant panX en amont,
      // le resultat est garanti quel que soit l'inlining.
      //
      // Il y a QUATRE Play indépendants dans la hierarchie SFX (SFX, SFXInstanced,
      // SFXLooped, SFXVaried) : chacun calcule CalculatePan(panX) pour son compte,
      // aucun ne delegue a un autre -> on les patche tous les quatre, sans risque de
      // double transformation. Le coffre (TreasureChest) utilise SFXInstanced.
      Type[] sfxTypes = [typeof(SFX), typeof(SFXInstanced), typeof(SFXLooped), typeof(SFXVaried)];
      foreach (var sfxType in sfxTypes)
      {
        harmony.Patch(
            AccessTools.DeclaredMethod(sfxType, "Play", [typeof(float), typeof(float)]),
            prefix: new HarmonyMethod(Play_patch)
        );
      }
    }

    /// <summary>Saute la methode patchee tant qu'une manche de ce mode est en cours.</summary>
    private static bool SkipInScroll_patch()
    {
      return !IsScrollActive();
    }

    // Vrai pendant qu'un level du mode Loop Scroll est en cours de chargement/jeu.
    internal static bool IsScrollActive()
    {
      Scene scene = Engine.Instance != null ? Engine.Instance.Scene : null;
      Session sess = null;
      if (scene is LevelLoaderXML loader)
        sess = loader.Session;
      else if (scene is Level lvl)
        sess = lvl.Session;

      return IsScrollMode(sess != null ? sess.MatchSettings : null);
    }

    // Source unique de verite pour "sommes-nous en mode Scroll", partagee par
    // tous les hooks du mod.
    //
    // FortRise 4 : IsCustom && CurrentModeName == nameof(Scroll).
    // FortRise 5 : comparaison sur la valeur Modes stable de l'entree enregistree.
    internal static bool IsScrollMode(MatchSettings ms)
    {
      return ms != null
          && Scroll.ScrollEntry != null
          && ms.Mode == Scroll.ScrollEntry.Modes;
    }

    private static bool GetBitData_patch(ref string data, ref int width, ref int height)
    {
      bool scroll = IsScrollActive();
      if (scroll)
      {
        width = ScrollLevelSystem.WidthTiles;
        height = ScrollLevelSystem.HeightTiles;
      }

      Diag("GetBitData", scroll, width, height, data);
      return true;
    }

    /// <summary>
    /// Journal de diagnostic du chargement des tuiles.
    ///
    /// Les murs des blocs au-dela du premier ne s'affichaient pas, et tout le chemin
    /// - assemblage du CSV, patch des dimensions, taille du tilemap - est juste sur
    /// le papier. On mesure donc au lieu de deduire : ce que valent reellement les
    /// dimensions forcees, et la forme de la chaine qu'on donne a lire.
    ///
    /// Quelques lignes par chargement de niveau, uniquement en mode Scroll.
    /// </summary>
    private static void Diag(string who, bool scroll, int width, int height, string data)
    {
      if (!scroll)
      {
        return;
      }

      int length = data == null ? -1 : data.Length;
      int firstRow = -1;
      if (data != null)
      {
        int nl = data.IndexOf('\n');
        firstRow = nl < 0 ? data.Length : nl;
      }

      Logger.Info($"[Tiles] {who} force {width}x{height} (attendu {ScrollLevelSystem.WidthTiles}x{ScrollLevelSystem.HeightTiles}) "
          + $"- chaine {length} car, 1re ligne {firstRow}");
    }

    // NB : le 1er parametre de ReadCSVIntGrid s'appelle "csv" (et non "data" comme
    // GetBitData) ; Harmony lie les parametres par nom, il doit donc correspondre.
    private static bool ReadCSVIntGrid_patch(ref string csv, ref int width, ref int height)
    {
      bool scroll = IsScrollActive();
      if (scroll)
      {
        width = ScrollLevelSystem.WidthTiles;
        height = ScrollLevelSystem.HeightTiles;
      }

      Diag("ReadCSVIntGrid", scroll, width, height, csv);
      return true;
    }

    private static bool Tilemap_ctor_patch(ref int width, ref int height)
    {
      bool scroll = IsScrollActive();

      // Seuls les tilemaps de niveau (320x240) doivent etre agrandis.
      if (scroll && width == 320 && height == 240)
      {
        width = ScrollLevelSystem.WidthTiles * 10;
        height = ScrollLevelSystem.HeightTiles * 10;
      }

      // Journalise TOUJOURS, y compris hors mode : c'est ce qui a permis de voir que
      // ce patch ne passait pas la ou on le croyait.
      Logger.Info($"[Tiles] Tilemap {width}x{height} px (scroll={scroll})");
      return true;
    }

    /// <summary>
    /// Redimensionne le tilemap APRES coup, une fois les tuiles posees.
    ///
    /// Le patch du constructeur ne suffit pas : le journal montre qu'il ne passe pas
    /// au chargement d'une manche de ce mode, alors que les dimensions de la GRILLE,
    /// elles, sont bien forcees - d'ou des collisions justes et des briques
    /// invisibles au-dela du premier bloc, le canevas restant large de 320 pixels.
    ///
    /// Ici la scene est celle du niveau, sans ambiguite : on remplace le tilemap par
    /// un autre a la taille du niveau combine et on redessine. Le cout est celui d'un
    /// retuilage, une fois par manche.
    /// </summary>
    private static void TilesAdded_patch(Entity __instance)
    {
      try
      {
        if (!IsScrollActive())
        {
          return;
        }

        int wanted = ScrollLevelSystem.WidthTiles * 10;
        int wantedH = ScrollLevelSystem.HeightTiles * 10;

        using var data = DynamicData.For(__instance);
        var tilemap = data.Get<Tilemap>("tilemap");
        if (tilemap == null || tilemap.Canvas == null)
        {
          return;
        }

        if (tilemap.Canvas.Width >= wanted && tilemap.Canvas.Height >= wantedH)
        {
          return;
        }

        __instance.Remove(tilemap);
        var big = new Tilemap(wanted, wantedH);
        __instance.Add(big);
        data.Set("tilemap", big);

        // LoadTiles est publique sur LevelTiles, privee sur LevelBGTiles : on passe
        // par DynamicData pour les deux, ce qui evite d'ecrire deux fois le meme
        // code pour deux classes qui ne partagent pas d'ancetre commun utile.
        data.Invoke("LoadTiles", big);

        Logger.Info($"[Tiles] {__instance.GetType().Name} : canevas agrandi a {wanted}x{wantedH}");
      }
      catch (Exception e)
      {
        Logger.Error("TilesAdded_patch: " + e);
      }
    }

    // Le pan audio est calcule en supposant un ecran de 0..320px. Dans notre grand
    // niveau, une entite au-dela de x=320 produit un pan hors [-1,1] -> crash
    // (SoundEffectInstance.set_Pan). On calcule le pan RELATIF a la fenetre camera,
    // borne a [-1,1], puis on re-encode ce pan dans panX pour que le
    // CalculatePan(panX) interne a Play (= Lerp(-0.5,0.5,panX/320)) le reproduise
    // exactement. Prefix void : Play s'execute ensuite normalement avec un panX sur.
    private static void Play_patch(ref float panX)
    {
      if (!IsScrollActive())
        return;

      float camX = 0f;
      if (Engine.Instance.Scene is Level lvl)
        camX = lvl.Camera.X;
      float winW = Engine.Instance.Screen != null ? (float)Engine.Instance.Screen.Width : 320f;
      float local = panX - camX;
      float pan = MathHelper.Lerp(-0.5f, 0.5f, local / winW);
      pan = MathHelper.Clamp(pan, -1f, 1f);

      // Inverse de CalculatePan : newPanX tel que Lerp(-0.5,0.5,newPanX/320) == pan.
      panX = (pan + 0.5f) * 320f;
    }
  }
}
