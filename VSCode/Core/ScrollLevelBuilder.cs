using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace TFModFortRiseScroll
{
  // Utilitaires de parsing/assemblage des grilles de tuiles d'un level TowerFall.
  //
  // Un level Versus ordinaire fait 32x24 tuiles :
  //   - Solids / BG      : bitstring (24 lignes de 32 caractères '0'/'1')
  //   - SolidTiles / BGTiles : CSV d'entiers (-1 = pas de tuile), potentiellement
  //     "TrimmedCSV" (lignes/colonnes de -1 en fin omises) -> on re-complete.
  //
  // Mais pas TOUS : les levels larges du mod WiderSet font 42x24 (420 px). La taille
  // d'un bloc n'est donc pas une constante, elle se LIT dans le level lui-meme (voir
  // Detect). Les blocs d'une meme tour ayant tous la meme taille, une seule lecture
  // suffit pour toute une manche.
  //
  // Ces helpers normalisent chaque level source en grille pleine de la taille d'un
  // bloc, puis assemblent une grande grille en plaçant les blocs a des offsets
  // (col,row).
  internal static class ScrollLevelBuilder
  {
    // Taille d'un bloc, en tuiles. Valeurs par defaut = level Versus ordinaire ;
    // Detect les remplace au debut de chaque assemblage.
    public static int BLOCK_W = 32;
    public static int BLOCK_H = 24;

    /// <summary>
    /// Lit la taille d'un bloc dans un level source.
    ///
    /// L'attribut <c>width</c> du level fait foi - c'est celui que l'editeur ecrit,
    /// en PIXELS - et le bitstring des solides sert de recours : la longueur de sa
    /// premiere ligne est la largeur en tuiles. Sans l'un ni l'autre, on garde
    /// 32x24, ce qui etait le seul cas gere jusqu'ici.
    /// </summary>
    public static void Detect(XmlElement level)
    {
      BLOCK_W = 32;
      BLOCK_H = 24;

      if (level == null)
      {
        return;
      }

      int width = AttrInt(level, "width");
      int height = AttrInt(level, "height");

      if (width > 0 && height > 0)
      {
        BLOCK_W = width / 10;
        BLOCK_H = height / 10;
        return;
      }

      string bits = level["Solids"]?.InnerText;
      if (string.IsNullOrEmpty(bits))
      {
        return;
      }

      string[] lines = bits.Replace("\r", "").Trim().Split('\n');
      if (lines.Length > 0 && lines[0].Trim().Length > 0)
      {
        BLOCK_W = lines[0].Trim().Length;
        BLOCK_H = lines.Length;
      }
    }

    private static int AttrInt(XmlElement element, string name)
    {
      string raw = element.GetAttribute(name);
      return int.TryParse(raw, out int value) ? value : 0;
    }

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

    /// <summary>
    /// Ferme le grand niveau par un cadre solide de <paramref name="thickness"/>
    /// tuiles.
    ///
    /// Les deux cotes ne se decident pas ensemble : en BANDE, le haut et le bas
    /// restent ouverts pour que le wrap vertical ait un sens - tomber par le bas doit
    /// ramener par le haut - alors qu'en ANNEAU tout est clos, wrap compris, et le
    /// moindre trou au plafond ferait sortir du parcours.
    /// </summary>
    /// <param name="sides">Fermer les bords gauche et droit.</param>
    /// <param name="topBottom">Fermer le plafond et le sol.</param>
    public static void FillSolidBorder(bool[][] dest, int thickness, bool sides, bool topBottom)
    {
      int rows = dest.Length;
      if (rows == 0 || thickness <= 0)
        return;

      int cols = dest[0].Length;
      for (int y = 0; y < rows; y++)
      {
        for (int x = 0; x < cols; x++)
        {
          bool onTopBottom = y < thickness || y >= rows - thickness;
          bool onSide = x < thickness || x >= cols - thickness;

          if ((topBottom && onTopBottom) || (sides && onSide))
            dest[y][x] = true;
        }
      }
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
