using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace TFModFortRiseSpeedRun
{
  // Utilitaires de parsing/assemblage des grilles de tuiles d'un level TowerFall.
  //
  // Un level Versus fait 32x24 tuiles :
  //   - Solids / BG      : bitstring (24 lignes de 32 caractères '0'/'1')
  //   - SolidTiles / BGTiles : CSV d'entiers (-1 = pas de tuile), potentiellement
  //     "TrimmedCSV" (lignes/colonnes de -1 en fin omises) -> on re-complete.
  //
  // Ces helpers normalisent chaque level source en grille pleine 32x24 puis
  // assemblent une grande grille en plaçant les blocs à des offsets (col,row).
  internal static class SpeedRunLevelBuilder
  {
    public const int BLOCK_W = 32; // tuiles par bloc (largeur)
    public const int BLOCK_H = 24; // tuiles par bloc (hauteur)

    // Parse un bitstring en grille bool[BLOCK_H][BLOCK_W], complétée par 'false'.
    public static bool[][] ParseBits(string data)
    {
      bool[][] grid = NewBoolGrid(BLOCK_H, BLOCK_W);
      if (string.IsNullOrEmpty(data))
        return grid;

      string[] lines = data.Replace("\r", "").Split('\n');
      int y = 0;
      foreach (string raw in lines)
      {
        string line = raw.Trim();
        if (line.Length == 0)
          continue;
        if (y >= BLOCK_H)
          break;
        for (int x = 0; x < BLOCK_W && x < line.Length; x++)
          grid[y][x] = line[x] == '1';
        y++;
      }
      return grid;
    }

    // Parse un CSV d'entiers en grille int[BLOCK_H][BLOCK_W], complétée par -1.
    public static int[][] ParseCSV(string data)
    {
      int[][] grid = NewIntGrid(BLOCK_H, BLOCK_W, -1);
      if (string.IsNullOrEmpty(data))
        return grid;

      string[] lines = data.Replace("\r", "").Split('\n');
      int y = 0;
      foreach (string raw in lines)
      {
        string line = raw.Trim();
        if (line.Length == 0)
          continue;
        if (y >= BLOCK_H)
          break;
        string[] cells = line.Split(',');
        for (int x = 0; x < BLOCK_W && x < cells.Length; x++)
        {
          int v;
          if (int.TryParse(cells[x].Trim(), out v))
            grid[y][x] = v;
        }
        y++;
      }
      return grid;
    }

    public static bool[][] NewBoolGrid(int rows, int cols)
    {
      bool[][] g = new bool[rows][];
      for (int y = 0; y < rows; y++)
        g[y] = new bool[cols];
      return g;
    }

    public static int[][] NewIntGrid(int rows, int cols, int fill)
    {
      int[][] g = new int[rows][];
      for (int y = 0; y < rows; y++)
      {
        g[y] = new int[cols];
        for (int x = 0; x < cols; x++)
          g[y][x] = fill;
      }
      return g;
    }

    // Copie un bloc BLOCK_H x BLOCK_W dans une grande grille à l'offset tuile (colOff,rowOff).
    public static void BlitBits(bool[][] dest, bool[][] block, int colOff, int rowOff)
    {
      for (int y = 0; y < BLOCK_H; y++)
        for (int x = 0; x < BLOCK_W; x++)
          dest[rowOff + y][colOff + x] = block[y][x];
    }

    public static void BlitInts(int[][] dest, int[][] block, int colOff, int rowOff)
    {
      for (int y = 0; y < BLOCK_H; y++)
        for (int x = 0; x < BLOCK_W; x++)
          dest[rowOff + y][colOff + x] = block[y][x];
    }

    // Remplit un bloc entier en solide (utilisé pour boucher l'intérieur de l'anneau).
    public static void FillBitsSolid(bool[][] dest, int colOff, int rowOff)
    {
      for (int y = 0; y < BLOCK_H; y++)
        for (int x = 0; x < BLOCK_W; x++)
          dest[rowOff + y][colOff + x] = true;
    }

    // Ferme le grand niveau par un cadre solide de `thickness` tuiles (haut, bas,
    // gauche, droite) : bouche les trous du haut/bas (plus de wrap vertical) et
    // ferme les murs qui delimitent le niveau.
    public static void FillSolidBorder(bool[][] dest, int thickness)
    {
      int rows = dest.Length;
      if (rows == 0 || thickness <= 0)
        return;
      int cols = dest[0].Length;
      for (int y = 0; y < rows; y++)
        for (int x = 0; x < cols; x++)
          if (y < thickness || y >= rows - thickness || x < thickness || x >= cols - thickness)
            dest[y][x] = true;
    }

    public static string BitsToString(bool[][] grid)
    {
      StringBuilder sb = new StringBuilder();
      for (int y = 0; y < grid.Length; y++)
      {
        for (int x = 0; x < grid[y].Length; x++)
          sb.Append(grid[y][x] ? '1' : '0');
        if (y < grid.Length - 1)
          sb.Append('\n');
      }
      return sb.ToString();
    }

    public static string IntsToCSV(int[][] grid)
    {
      StringBuilder sb = new StringBuilder();
      for (int y = 0; y < grid.Length; y++)
      {
        for (int x = 0; x < grid[y].Length; x++)
        {
          if (x > 0)
            sb.Append(',');
          sb.Append(grid[y][x]);
        }
        if (y < grid.Length - 1)
          sb.Append('\n');
      }
      return sb.ToString();
    }

    // Conversion vers le format bool[x,y] 32x24 attendu par les generateurs
    // proceduraux vanilla (LevelRandomGeometry, LevelRandomBGTiles).
    public static bool[,] ToXY(bool[][] rows)
    {
      bool[,] xy = new bool[BLOCK_W, BLOCK_H];
      for (int y = 0; y < BLOCK_H; y++)
        for (int x = 0; x < BLOCK_W; x++)
          xy[x, y] = rows[y][x];
      return xy;
    }

    public static bool[][] FromXY(bool[,] xy)
    {
      bool[][] rows = NewBoolGrid(BLOCK_H, BLOCK_W);
      for (int y = 0; y < BLOCK_H; y++)
        for (int x = 0; x < BLOCK_W; x++)
          rows[y][x] = xy[x, y];
      return rows;
    }

    // Perce une porte VERTICALE dans la jointure entre deux blocs cote a cote
    // (mouvement horizontal). boundaryCol = colonne-tuile de la frontiere ; on
    // vide les colonnes [boundaryCol-half, boundaryCol+half) sur la hauteur du
    // bloc, en gardant ceilKeep rangees de plafond et floorKeep rangees de sol.
    public static void CarveVerticalDoor(bool[][] solids, int[][] solidTiles, int boundaryCol, int rowBlockTop, int half, int ceilKeep, int floorKeep)
    {
      int top = rowBlockTop + ceilKeep;
      int bottom = rowBlockTop + BLOCK_H - floorKeep;
      int cols = solids.Length > 0 ? solids[0].Length : 0;
      for (int y = top; y < bottom; y++)
      {
        if (y < 0 || y >= solids.Length)
          continue;
        for (int x = boundaryCol - half; x < boundaryCol + half; x++)
        {
          if (x < 0 || x >= cols)
            continue;
          solids[y][x] = false;
          solidTiles[y][x] = -1;
        }
      }
    }

    // Perce une porte HORIZONTALE dans la jointure entre deux blocs empiles
    // (mouvement vertical). boundaryRow = rangee-tuile de la frontiere ; on vide
    // les rangees [boundaryRow-half, boundaryRow+half) sur la largeur du bloc, en
    // gardant sideKeep colonnes de mur de chaque cote.
    public static void CarveHorizontalDoor(bool[][] solids, int[][] solidTiles, int boundaryRow, int colBlockLeft, int half, int sideKeep)
    {
      int left = colBlockLeft + sideKeep;
      int right = colBlockLeft + BLOCK_W - sideKeep;
      int cols = solids.Length > 0 ? solids[0].Length : 0;
      for (int y = boundaryRow - half; y < boundaryRow + half; y++)
      {
        if (y < 0 || y >= solids.Length)
          continue;
        for (int x = left; x < right; x++)
        {
          if (x < 0 || x >= cols)
            continue;
          solids[y][x] = false;
          solidTiles[y][x] = -1;
        }
      }
    }

    // Décale récursivement les attributs x/y (pixels) d'un élément d'entité et de
    // ses descendants (ex: nodes de plateformes mobiles).
    public static void OffsetEntity(XmlElement e, int dxPixels, int dyPixels)
    {
      OffsetAttr(e, "x", dxPixels);
      OffsetAttr(e, "y", dyPixels);
      foreach (XmlNode child in e.ChildNodes)
      {
        if (child is XmlElement childEl)
          OffsetEntity(childEl, dxPixels, dyPixels);
      }
    }

    private static void OffsetAttr(XmlElement e, string attr, int delta)
    {
      if (delta == 0 || !e.HasAttribute(attr))
        return;
      int v;
      if (int.TryParse(e.GetAttribute(attr), out v))
        e.SetAttribute(attr, (v + delta).ToString());
    }
  }
}
