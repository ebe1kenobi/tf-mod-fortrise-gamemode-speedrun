using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Systeme de niveau du mode Loop Scroll : assemble plusieurs levels d'un monde
  // Versus en un seul grand level.
  //
  // Etape actuelle : BANDE HORIZONTALE (tous les levels concatenes de gauche a
  // droite, une seule rangee). Le parcours en anneau (droite/bas/gauche/haut)
  // sera ajoute ensuite en changeant ComputePlacements().
  public class SpeedRunLevelSystem : VersusLevelSystem
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
    // rendu (SpeedRunRenderPatches : Calc.GetBitData/ReadCSVIntGrid, Tilemap.ctor).
    public static int WidthTiles = SpeedRunLevelBuilder.BLOCK_W;
    public static int HeightTiles = SpeedRunLevelBuilder.BLOCK_H;

    // Largeur/hauteur totales en pixels du niveau combine (pour le scroll camera).
    public static int TotalWidthPixels = 320;
    public static int TotalHeightPixels = 240;

    // Grille de blocs et forme du parcours (lus par SpeedRunRoundLogic pour
    // piloter la camera).
    public static int GridCols = 1;
    public static int GridRows = 1;
    public static bool IsLoop = false;

    private static readonly Random Rng = new Random();

    public SpeedRunLevelSystem(VersusTowerData tower) : base(tower)
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
      int maxLevels = TFModFortRiseSpeedRunModule.Settings.SpeedRunMaxLevels;
      if (maxLevels > 0 && ordered.Count > maxLevels)
        ordered = ordered.GetRange(0, maxLevels);

      bool square = TFModFortRiseSpeedRunModule.Settings.SpeedRunShape == TFModFortRiseSpeedRunSettings.ShapeSquare
                    && ordered.Count >= 4;

      List<Placement> placements = square
        ? ComputeRingPlacements(ordered, out int gridCols, out int gridRows)
        : ComputeBandPlacements(ordered, out gridCols, out gridRows);

      GridCols = gridCols;
      GridRows = gridRows;
      IsLoop = square;
      WidthTiles = gridCols * SpeedRunLevelBuilder.BLOCK_W;
      HeightTiles = gridRows * SpeedRunLevelBuilder.BLOCK_H;
      TotalWidthPixels = WidthTiles * 10;
      TotalHeightPixels = HeightTiles * 10;

      // Seed aleatoire comme le fait le loader vanilla pour les tours procedurales.
      bool procedural = this.VersusTowerData.Procedural;
      if (procedural)
        Calc.PushRandom(matchSettings.RandomLevelSeed);
      XmlElement combined = BuildCombinedLevel(placements, gridCols, gridRows, square);
      if (procedural)
        Calc.PopRandom();
      return combined;
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
      int rows = gridRows * SpeedRunLevelBuilder.BLOCK_H;
      int cols = gridCols * SpeedRunLevelBuilder.BLOCK_W;

      bool[][] solids = SpeedRunLevelBuilder.NewBoolGrid(rows, cols);
      bool[][] bg = SpeedRunLevelBuilder.NewBoolGrid(rows, cols);
      int[][] solidTiles = SpeedRunLevelBuilder.NewIntGrid(rows, cols, -1);
      int[][] bgTiles = SpeedRunLevelBuilder.NewIntGrid(rows, cols, -1);

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
        int colOff = p.Col * SpeedRunLevelBuilder.BLOCK_W;
        int rowOff = p.Row * SpeedRunLevelBuilder.BLOCK_H;
        int dxPixels = p.Col * 320;
        int dyPixels = p.Row * 240;

        bool[][] blockSolids = SpeedRunLevelBuilder.ParseBits(src["Solids"]?.InnerText);
        bool[][] blockBG = SpeedRunLevelBuilder.ParseBits(src["BG"]?.InnerText);

        // Tour procedurale : generer la geometrie du bloc en coordonnees LOCALES
        // 32x24 avec les generateurs vanilla (impossible sur le grand niveau, ils
        // ont 32x24 code en dur).
        if (this.VersusTowerData.Procedural)
          GenerateProceduralBlock(src, ref blockSolids, ref blockBG);

        SpeedRunLevelBuilder.BlitBits(solids, blockSolids, colOff, rowOff);
        SpeedRunLevelBuilder.BlitBits(bg, blockBG, colOff, rowOff);
        SpeedRunLevelBuilder.BlitInts(solidTiles, SpeedRunLevelBuilder.ParseCSV(src["SolidTiles"]?.InnerText), colOff, rowOff);
        SpeedRunLevelBuilder.BlitInts(bgTiles, SpeedRunLevelBuilder.ParseCSV(src["BGTiles"]?.InnerText), colOff, rowOff);

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
            SpeedRunLevelBuilder.OffsetEntity(copy, dxPixels, dyPixels);
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
              SpeedRunLevelBuilder.FillBitsSolid(solids, c * SpeedRunLevelBuilder.BLOCK_W, r * SpeedRunLevelBuilder.BLOCK_H);
      }

      // Perce les jointures entre blocs consecutifs du parcours pour qu'ils
      // communiquent (sinon les colonnes de bord solides forment un mur).
      CarveSeams(placements, solids, solidTiles, isLoop);

      // Ferme le grand niveau par un cadre solide : plus de trous en haut/bas
      // (donc plus de wrap vertical necessaire) et murs de bordure fermes.
      SpeedRunLevelBuilder.FillSolidBorder(solids, BORDER_THICKNESS);

      XmlElement solidsEl = doc.CreateElement("Solids");
      solidsEl.SetAttribute("exportMode", "Bitstring");
      solidsEl.InnerText = SpeedRunLevelBuilder.BitsToString(solids);

      XmlElement bgEl = doc.CreateElement("BG");
      bgEl.SetAttribute("exportMode", "Bitstring");
      bgEl.InnerText = SpeedRunLevelBuilder.BitsToString(bg);

      XmlElement solidTilesEl = doc.CreateElement("SolidTiles");
      solidTilesEl.SetAttribute("exportMode", "CSV");
      if (solidTileset != null)
        solidTilesEl.SetAttribute("tileset", solidTileset);
      solidTilesEl.InnerText = SpeedRunLevelBuilder.IntsToCSV(solidTiles);

      XmlElement bgTilesEl = doc.CreateElement("BGTiles");
      bgTilesEl.SetAttribute("exportMode", "CSV");
      if (bgTileset != null)
        bgTilesEl.SetAttribute("tileset", bgTileset);
      bgTilesEl.InnerText = SpeedRunLevelBuilder.IntsToCSV(bgTiles);

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

      bool[,] baseData = SpeedRunLevelBuilder.ToXY(blockSolids);
      bool[,] generated = LevelRandomGeometry.GenerateData(rects, baseData);
      bool[,] bgData = LevelRandomBGTiles.GenerateBitData(generated);
      blockSolids = SpeedRunLevelBuilder.FromXY(generated);
      blockBG = SpeedRunLevelBuilder.FromXY(bgData);
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
          int boundaryCol = System.Math.Max(a.Col, b.Col) * SpeedRunLevelBuilder.BLOCK_W;
          int rowBlockTop = a.Row * SpeedRunLevelBuilder.BLOCK_H;
          SpeedRunLevelBuilder.CarveVerticalDoor(solids, solidTiles, boundaryCol, rowBlockTop, DOOR_HALF, CEIL_KEEP, FLOOR_KEEP);
        }
        else if (a.Col == b.Col && System.Math.Abs(a.Row - b.Row) == 1)
        {
          // Jointure horizontale (blocs empiles) -> porte horizontale.
          int boundaryRow = System.Math.Max(a.Row, b.Row) * SpeedRunLevelBuilder.BLOCK_H;
          int colBlockLeft = a.Col * SpeedRunLevelBuilder.BLOCK_W;
          SpeedRunLevelBuilder.CarveHorizontalDoor(solids, solidTiles, boundaryRow, colBlockLeft, DOOR_HALF, SIDE_KEEP);
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
