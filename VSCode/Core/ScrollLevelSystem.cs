using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TFModFortRiseScroll
{
  // Systeme de niveau du mode Loop Scroll : assemble plusieurs levels d'un monde
  // Versus en un seul grand level.
  //
  // Etape actuelle : BANDE HORIZONTALE (tous les levels concatenes de gauche a
  // droite, une seule rangee). Le parcours en anneau (droite/bas/gauche/haut)
  // sera ajoute ensuite en changeant ComputePlacements().
  public class ScrollLevelSystem : VersusLevelSystem
  {
    // Placement d'un bloc (level) dans la grande grille, en blocs.
    private struct Placement
    {
      public string Path;
      public int Col;
      public int Row;
      public bool KeepPlayerSpawns;
    }

    // Dimensions du niveau combine courant, en tuiles. Lues par les hooks de
    // rendu (ScrollRenderPatches : Calc.GetBitData/ReadCSVIntGrid, Tilemap.ctor).
    public static int WidthTiles = ScrollLevelBuilder.BLOCK_W;
    public static int HeightTiles = ScrollLevelBuilder.BLOCK_H;

    // Largeur/hauteur totales en pixels du niveau combine (pour le scroll camera).
    public static int TotalWidthPixels = 320;
    public static int TotalHeightPixels = 240;

    // Grille de blocs et forme du parcours (lus par ScrollRoundLogic pour
    // piloter la camera).
    public static int GridCols = 1;
    public static int GridRows = 1;
    public static bool IsLoop = false;

    private static readonly Random Rng = new Random();

    public ScrollLevelSystem(VersusTowerData tower) : base(tower)
    {
    }

    // Les tours procedurales (Cataclysm) generent leur geometrie au chargement
    // via LevelRandomGeometry, dont les tailles 32x24 sont codees en dur -> crash
    // sur notre grand niveau. On genere donc la geometrie NOUS-MEMES bloc par bloc
    // (voir BuildCombinedLevel) et on desactive le flag pour le loader.
    public override bool Procedural
    {
      get { return false; }
    }

    public override XmlElement GetNextRoundLevel(MatchSettings matchSettings, int roundIndex, out int randomSeed)
    {
      randomSeed = 0;

      // Levels du monde jouables au nombre de joueurs courant (garantit assez de
      // PlayerSpawn sur chaque bloc).
      List<string> paths = this.VersusTowerData.GetLevels(matchSettings);
      if (paths == null || paths.Count == 0)
      {
        // Securite : repli sur le comportement standard.
        return base.GetNextRoundLevel(matchSettings, roundIndex, out randomSeed);
      }

      // Depart aleatoire : on tourne la liste pour commencer a un level tire au
      // hasard, puis on concatene dans l'ordre des fichiers.
      int start = Rng.Next(paths.Count);
      List<string> ordered = new List<string>(paths.Count);
      for (int i = 0; i < paths.Count; i++)
        ordered.Add(paths[(start + i) % paths.Count]);

      // Limite au nombre de levels demande dans les settings.
      int maxLevels = TFModFortRiseScrollModule.Settings.ScrollMaxLevels;
      if (maxLevels > 0 && ordered.Count > maxLevels)
        ordered = ordered.GetRange(0, maxLevels);

      // La taille d'un bloc se lit dans le premier level : 32x24 pour une tour
      // ordinaire, 42x24 pour les levels larges du mod WiderSet. Tout ce qui suit -
      // dimensions du niveau combine, offsets de collage, decoupe des jointures - en
      // decoule, donc cette lecture doit venir en premier.
      ScrollLevelBuilder.Detect(Calc.LoadXML(ordered[0])["level"]);

      bool square = TFModFortRiseScrollModule.Settings.ScrollShape == TFModFortRiseScrollSettings.ShapeSquare
                    && ordered.Count >= 4;

      List<Placement> placements = square
        ? ComputeRingPlacements(ordered, out int gridCols, out int gridRows)
        : ComputeBandPlacements(ordered, out gridCols, out gridRows);

      GridCols = gridCols;
      GridRows = gridRows;
      IsLoop = square;
      WidthTiles = gridCols * ScrollLevelBuilder.BLOCK_W;
      HeightTiles = gridRows * ScrollLevelBuilder.BLOCK_H;
      TotalWidthPixels = WidthTiles * 10;
      TotalHeightPixels = HeightTiles * 10;

      // Le level "courant", tel que le jeu le retient.
      //
      // Le GetNextRoundLevel vanilla termine par "this.lastLevel = this.levels[0]",
      // et ce champ prive n'est pas decoratif : FortRise le relit au chargement de
      // CHAQUE niveau modde (Level.InitializeModdedLevel) pour savoir quel dossier
      // surveiller, et le passe tel quel comme cle de dictionnaire. Cette surcharge
      // ne rappelant jamais la version vanilla, le champ restait null - et la moindre
      // tour non officielle, celles du mod WiderSet par exemple, faisait tomber le
      // chargement sur un ArgumentNullException.
      //
      // Le niveau combine n'existe dans aucun fichier : on retient le premier des
      // levels assembles, qui est un vrai .oel du bon dossier.
      SetLastLevel(ordered[0]);

      // Seed aleatoire comme le fait le loader vanilla pour les tours procedurales.
      bool procedural = this.VersusTowerData.Procedural;
      if (procedural)
        Calc.PushRandom(matchSettings.RandomLevelSeed);
      XmlElement combined = BuildCombinedLevel(placements, gridCols, gridRows, square);
      if (procedural)
        Calc.PopRandom();
      return combined;
    }

    /// <summary>
    /// Renseigne le champ prive <c>lastLevel</c> de VersusLevelSystem.
    ///
    /// Prive et sans accesseur : DynamicData est le seul moyen d'y ecrire. Un echec
    /// est avale - on retomberait sur le comportement d'avant, pas pire - mais il
    /// est journalise, parce qu'il ramenerait le crash au chargement.
    /// </summary>
    private void SetLastLevel(string path)
    {
      if (string.IsNullOrEmpty(path))
      {
        return;
      }

      try
      {
        using var data = DynamicData.For(this);
        data.Set("lastLevel", path);
      }
      catch (Exception e)
      {
        Logger.Error("ScrollLevelSystem.SetLastLevel: " + e.Message);
      }
    }

    // Bande horizontale : une rangee, chaque level dans sa colonne.
    private List<Placement> ComputeBandPlacements(List<string> ordered, out int gridCols, out int gridRows)
    {
      List<Placement> placements = new List<Placement>();
      for (int i = 0; i < ordered.Count; i++)
      {
        placements.Add(new Placement
        {
          Path = ordered[i],
          Col = i,
          Row = 0,
          KeepPlayerSpawns = (i == 0)
        });
      }
      gridCols = ordered.Count;
      gridRows = 1;
      return placements;
    }

    // Anneau a 2 rangees (la descente/remontee ne fait qu'UN bloc) parcouru dans
    // le sens horaire : rangee du haut (->), descente d'1 bloc, rangee du bas (<-),
    // remontee d'1 bloc. Utilise 2*K blocs (K colonnes par rangee).
    private List<Placement> ComputeRingPlacements(List<string> ordered, out int gridCols, out int gridRows)
    {
      int k = ordered.Count / 2; // colonnes par rangee
      if (k < 2)
        k = 2;                   // minimum 2x2 (garanti car N>=4)

      List<Point> cells = new List<Point>();
      for (int col = 0; col < k; col++) cells.Add(new Point(col, 0));      // haut ->
      for (int col = k - 1; col >= 0; col--) cells.Add(new Point(col, 1)); // bas <-

      List<Placement> placements = new List<Placement>();
      for (int i = 0; i < cells.Count; i++)
      {
        placements.Add(new Placement
        {
          Path = ordered[i],
          Col = cells[i].X,
          Row = cells[i].Y,
          KeepPlayerSpawns = (i == 0)
        });
      }
      gridCols = k;
      gridRows = 2;
      return placements;
    }

    private XmlElement BuildCombinedLevel(List<Placement> placements, int gridCols, int gridRows, bool isLoop)
    {
      int rows = gridRows * ScrollLevelBuilder.BLOCK_H;
      int cols = gridCols * ScrollLevelBuilder.BLOCK_W;

      bool[][] solids = ScrollLevelBuilder.NewBoolGrid(rows, cols);
      bool[][] bg = ScrollLevelBuilder.NewBoolGrid(rows, cols);
      int[][] solidTiles = ScrollLevelBuilder.NewIntGrid(rows, cols, -1);
      int[][] bgTiles = ScrollLevelBuilder.NewIntGrid(rows, cols, -1);

      XmlDocument doc = new XmlDocument();
      XmlElement level = doc.CreateElement("level");
      level.SetAttribute("width", TotalWidthPixelsFor(cols).ToString());
      level.SetAttribute("height", TotalHeightPixelsFor(rows).ToString());
      level.SetAttribute("Darkness", "0");
      // Pas de wrap moteur : on gerera le wrap relatif-camera nous-memes.
      level.SetAttribute("WrapMode", "Both");
      level.SetAttribute("CanUnlockMoonstone", "False");
      level.SetAttribute("CanUnlockPurple", "False");

      XmlElement entities = doc.CreateElement("Entities");
      int id = 0;
      string bgTileset = null;
      string solidTileset = null;

      foreach (Placement p in placements)
      {
        XmlElement src = Calc.LoadXML(p.Path)["level"];
        int colOff = p.Col * ScrollLevelBuilder.BLOCK_W;
        int rowOff = p.Row * ScrollLevelBuilder.BLOCK_H;
        int dxPixels = p.Col * ScrollLevelBuilder.BLOCK_W * 10;
        int dyPixels = p.Row * ScrollLevelBuilder.BLOCK_H * 10;

        bool[][] blockSolids = ScrollLevelBuilder.ParseBits(src["Solids"]?.InnerText);
        bool[][] blockBG = ScrollLevelBuilder.ParseBits(src["BG"]?.InnerText);

        // Tour procedurale : generer la geometrie du bloc en coordonnees LOCALES
        // 32x24 avec les generateurs vanilla (impossible sur le grand niveau, ils
        // ont 32x24 code en dur).
        if (this.VersusTowerData.Procedural)
          GenerateProceduralBlock(src, ref blockSolids, ref blockBG);

        ScrollLevelBuilder.BlitBits(solids, blockSolids, colOff, rowOff);
        ScrollLevelBuilder.BlitBits(bg, blockBG, colOff, rowOff);
        ScrollLevelBuilder.BlitInts(solidTiles, ScrollLevelBuilder.ParseCSV(src["SolidTiles"]?.InnerText), colOff, rowOff);
        ScrollLevelBuilder.BlitInts(bgTiles, ScrollLevelBuilder.ParseCSV(src["BGTiles"]?.InnerText), colOff, rowOff);

        if (bgTileset == null && src["BGTiles"] != null)
          bgTileset = src["BGTiles"].GetAttribute("tileset");
        if (solidTileset == null && src["SolidTiles"] != null)
          solidTileset = src["SolidTiles"].GetAttribute("tileset");

        // (le carving des jointures se fait apres avoir place tous les blocs)

        XmlElement srcEntities = src["Entities"];
        if (srcEntities != null)
        {
          foreach (XmlNode node in srcEntities.ChildNodes)
          {
            if (!(node is XmlElement srcEntity))
              continue;
            // Un seul bloc conserve les points d'apparition des joueurs.
            if (!p.KeepPlayerSpawns && srcEntity.Name == "PlayerSpawn")
              continue;
            // Cobwebs valide un solide adjacent dans SceneBegin (throw sinon) ;
            // le carving des jointures peut lui retirer son support -> on saute
            // ces decorations fragiles.
            if (srcEntity.Name == "Cobwebs")
              continue;
            // Marqueurs proceduraux deja consommes par GenerateProceduralBlock.
            if (srcEntity.Name == "RandomBlock")
              continue;

            XmlElement copy = (XmlElement)doc.ImportNode(srcEntity, true);
            ScrollLevelBuilder.OffsetEntity(copy, dxPixels, dyPixels);
            copy.SetAttribute("id", id.ToString());
            entities.AppendChild(copy);
            id++;
          }
        }
      }

      // Bouche l'interieur de l'anneau (cellules non couvertes par un level) en
      // solide plein, pour empecher d'entrer au centre.
      if (isLoop)
      {
        HashSet<int> occupied = new HashSet<int>();
        foreach (Placement p in placements)
          occupied.Add(p.Row * gridCols + p.Col);
        for (int r = 0; r < gridRows; r++)
          for (int c = 0; c < gridCols; c++)
            if (!occupied.Contains(r * gridCols + c))
              ScrollLevelBuilder.FillBitsSolid(solids, c * ScrollLevelBuilder.BLOCK_W, r * ScrollLevelBuilder.BLOCK_H);
      }

      // Perce les jointures entre blocs consecutifs du parcours pour qu'ils
      // communiquent (sinon les colonnes de bord solides forment un mur).
      CarveSeams(placements, solids, solidTiles, isLoop);

      // Le cadre solide. Les cotes gauche et droit sont toujours fermes - le
      // parcours a un debut et une fin - mais le haut et le bas ne le sont qu'en
      // ANNEAU : en bande, ils restent ouverts et c'est le wrap vertical qui ramene
      // par le haut ce qui tombe par le bas (voir ScrollWrapPatches).
      ScrollLevelBuilder.FillSolidBorder(solids, BORDER_THICKNESS, true, isLoop);

      XmlElement solidsEl = doc.CreateElement("Solids");
      solidsEl.SetAttribute("exportMode", "Bitstring");
      solidsEl.InnerText = ScrollLevelBuilder.BitsToString(solids);

      XmlElement bgEl = doc.CreateElement("BG");
      bgEl.SetAttribute("exportMode", "Bitstring");
      bgEl.InnerText = ScrollLevelBuilder.BitsToString(bg);

      XmlElement solidTilesEl = doc.CreateElement("SolidTiles");
      solidTilesEl.SetAttribute("exportMode", "CSV");
      if (solidTileset != null)
        solidTilesEl.SetAttribute("tileset", solidTileset);
      solidTilesEl.InnerText = ScrollLevelBuilder.IntsToCSV(solidTiles);

      XmlElement bgTilesEl = doc.CreateElement("BGTiles");
      bgTilesEl.SetAttribute("exportMode", "CSV");
      if (bgTileset != null)
        bgTilesEl.SetAttribute("tileset", bgTileset);
      bgTilesEl.InnerText = ScrollLevelBuilder.IntsToCSV(bgTiles);

      level.AppendChild(entities);
      level.AppendChild(solidsEl);
      level.AppendChild(solidTilesEl);
      level.AppendChild(bgEl);
      level.AppendChild(bgTilesEl);
      doc.AppendChild(level);
      return level;
    }

    // Genere la geometrie d'un bloc procedural (Cataclysm) en coordonnees locales
    // 32x24, comme le ferait LevelLoaderXML pour un level seul : les entites
    // RandomBlock definissent des zones ou LevelRandomGeometry pose des plateformes
    // aleatoires, puis LevelRandomBGTiles derive le fond. (Les items/tresors
    // aleatoires de LevelRandomItems ne sont pas generes ici.)
    private static void GenerateProceduralBlock(XmlElement src, ref bool[][] blockSolids, ref bool[][] blockBG)
    {
      List<Rectangle> rects = new List<Rectangle>();
      XmlElement srcEntities = src["Entities"];
      if (srcEntities != null)
      {
        foreach (XmlNode node in srcEntities.ChildNodes)
        {
          if (!(node is XmlElement e) || e.Name != "RandomBlock")
            continue;
          int x, y, w, h;
          int.TryParse(e.GetAttribute("x"), out x);
          int.TryParse(e.GetAttribute("y"), out y);
          int.TryParse(e.GetAttribute("width"), out w);
          int.TryParse(e.GetAttribute("height"), out h);
          rects.Add(new Rectangle(x / 10, y / 10, w / 10, h / 10));
        }
      }
      if (rects.Count == 0)
        return;

      bool[,] baseData = ScrollLevelBuilder.ToXY(blockSolids);
      bool[,] generated = LevelRandomGeometry.GenerateData(rects, baseData);
      bool[,] bgData = LevelRandomBGTiles.GenerateBitData(generated);
      blockSolids = ScrollLevelBuilder.FromXY(generated);
      blockBG = ScrollLevelBuilder.FromXY(bgData);
    }

    // Largeur (colonnes) de la porte de chaque cote de la jointure, et rangees de
    // sol/plafond a conserver.
    private const int DOOR_HALF = 3;   // 2*3 = 6 colonnes de large
    private const int CEIL_KEEP = 1;   // garde 1 rangee de plafond
    private const int FLOOR_KEEP = 2;  // garde 2 rangees de sol
    private const int SIDE_KEEP = 10;  // pour portes horizontales : garde les cotes
    private const int BORDER_THICKNESS = 1; // cadre solide autour du grand niveau

    // Perce une porte entre chaque paire de blocs consecutifs du parcours. Deux
    // blocs consecutifs sont toujours adjacents (colonne OU rangee differe de 1).
    private void CarveSeams(List<Placement> placements, bool[][] solids, int[][] solidTiles, bool isLoop)
    {
      int count = placements.Count;
      int pairs = isLoop ? count : count - 1; // en boucle, on ferme dernier->premier
      for (int i = 0; i < pairs; i++)
      {
        Placement a = placements[i];
        Placement b = placements[(i + 1) % count];

        if (a.Row == b.Row && System.Math.Abs(a.Col - b.Col) == 1)
        {
          // Jointure verticale (blocs cote a cote) -> porte verticale.
          int boundaryCol = System.Math.Max(a.Col, b.Col) * ScrollLevelBuilder.BLOCK_W;
          int rowBlockTop = a.Row * ScrollLevelBuilder.BLOCK_H;
          ScrollLevelBuilder.CarveVerticalDoor(solids, solidTiles, boundaryCol, rowBlockTop, DOOR_HALF, CEIL_KEEP, FLOOR_KEEP);
        }
        else if (a.Col == b.Col && System.Math.Abs(a.Row - b.Row) == 1)
        {
          // Jointure horizontale (blocs empiles) -> porte horizontale.
          int boundaryRow = System.Math.Max(a.Row, b.Row) * ScrollLevelBuilder.BLOCK_H;
          int colBlockLeft = a.Col * ScrollLevelBuilder.BLOCK_W;
          ScrollLevelBuilder.CarveHorizontalDoor(solids, solidTiles, boundaryRow, colBlockLeft, DOOR_HALF, SIDE_KEEP);
        }
      }
    }

    private static int TotalWidthPixelsFor(int cols)
    {
      return cols * 10;
    }

    private static int TotalHeightPixelsFor(int rows)
    {
      return rows * 10;
    }
  }
}
