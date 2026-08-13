using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseScroll
{
  // Popup de reglage du mode Loop Scroll, ouverte avec Y depuis le bouton de mode
  // (cf. MyScrollModeButton). Edite directement les ModuleSettings.
  public class UIScrollPopup : Entity
  {
    private class Field
    {
      public string Label;
      public Func<string> Value;
      public Action Left;
      public Action Right;
      // null = toujours visible. Un champ cache est saute par la navigation et
      // non dessine (options sans effet dans le mode camera courant).
      public Func<bool> Visible;
    }

    private readonly BorderButton ownerButton;
    private readonly List<Field> fields = new List<Field>();
    private int selected;

    // Instance courante de la popup (null si aucune).
    public static UIScrollPopup Current;

    // Ouverte seulement si l'instance courante appartient a la scene ACTIVE.
    // Auto-guerison : si la scene a ete remplacee alors que la popup etait ouverte
    // (ex: on lance le match en appuyant sur Start pendant que la popup est la),
    // son Removed() n'est jamais appele ; mais sa Scene ne correspond plus a la
    // scene courante -> elle ne compte plus comme ouverte, et le hint/Y reviennent.
    public static bool IsOpen => Current != null && Current.Scene == Engine.Instance.Scene;

    public UIScrollPopup(BorderButton ownerButton)
    {
      this.ownerButton = ownerButton;
      Position = new Vector2(160f, 120f);
      BuildFields();
    }

    private void BuildFields()
    {
      TFModFortRiseScrollSettings s = TFModFortRiseScrollModule.Settings;

      fields.Add(new Field
      {
        Label = "SHAPE",
        Value = () => s.ScrollShape == TFModFortRiseScrollSettings.ShapeSquare ? "SQUARE" : "HORIZONTAL",
        Left = () => s.ScrollShape = 1 - s.ScrollShape,
        Right = () => s.ScrollShape = 1 - s.ScrollShape
      });

      // Mode camera (3 valeurs) ; les options propres au scroll sont cachees en
      // FOLLOW PLAYERS (elles n'ont aucun effet dans ce mode).
      string[] camNames = { "AUTO SCROLL", "FOLLOW LEADER", "FOLLOW PLAYERS" };
      fields.Add(new Field
      {
        Label = "CAMERA",
        Value = () => camNames[Calc.Clamp(s.ScrollCamera, 0, camNames.Length - 1)],
        Left = () => s.ScrollCamera = (s.ScrollCamera + camNames.Length - 1) % camNames.Length,
        Right = () => s.ScrollCamera = (s.ScrollCamera + 1) % camNames.Length
      });
      Func<bool> scrollMode = () => s.ScrollCamera != TFModFortRiseScrollSettings.CameraFollowPlayers;

      Field speed = IntField("SPEED", () => s.ScrollSpeed, v => s.ScrollSpeed = v, 1, 30);
      speed.Visible = scrollMode;
      fields.Add(speed);

      // Acceleration progressive du scroll (0 = OFF) ; l'intervalle n'apparait
      // que si l'acceleration est active.
      Field accelAmount = IntField("ACCEL (+SPEED)", () => s.ScrollAccelAmount, v => s.ScrollAccelAmount = v, 0, 20);
      accelAmount.Value = () => s.ScrollAccelAmount == 0 ? "OFF" : "+" + s.ScrollAccelAmount;
      accelAmount.Visible = scrollMode;
      fields.Add(accelAmount);

      Field accelEvery = IntField("ACCEL EVERY (s)", () => s.ScrollAccelEvery, v => s.ScrollAccelEvery = v, 1, 60);
      accelEvery.Visible = () => scrollMode() && s.ScrollAccelAmount > 0;
      fields.Add(accelEvery);

      fields.Add(IntField("LEVELS", () => s.ScrollMaxLevels, v => s.ScrollMaxLevels = v, 2, 30));

      // Portail d'arrivee. Pas de notion de tour en follow players + square ->
      // le portail n'y existe pas, on cache l'option dans ce cas.
      Field goal = BoolField("GOAL PORTAL", () => s.ScrollGoalPortal, v => s.ScrollGoalPortal = v);
      goal.Visible = () => scrollMode() || s.ScrollShape == TFModFortRiseScrollSettings.ShapeHorizontal;
      fields.Add(goal);

      Field laps = IntField("LAPS (SQUARE)", () => s.ScrollLaps, v => s.ScrollLaps = v, 1, 10);
      laps.Visible = () => s.ScrollGoalPortal && s.ScrollShape == TFModFortRiseScrollSettings.ShapeSquare && scrollMode();
      fields.Add(laps);

      // Coffres : nombre + respawn. Le contenu (types de pickup) se regle dans
      // les settings du mod FortRise.
      fields.Add(IntField("TREASURES", () => s.ScrollTreasureCount, v => s.ScrollTreasureCount = v, 0, 20));

      Field tRespawn = IntField("CHEST RESPAWN (s)", () => s.ScrollTreasureRespawn, v => s.ScrollTreasureRespawn = v, 0, 60);
      tRespawn.Value = () => s.ScrollTreasureRespawn == 0 ? "OFF" : s.ScrollTreasureRespawn.ToString();
      tRespawn.Visible = () => s.ScrollTreasureCount > 0;
      fields.Add(tRespawn);

      Field leaveBehind = BoolField("LEAVE BEHIND", () => s.ScrollLeaveBehind, v => s.ScrollLeaveBehind = v);
      leaveBehind.Visible = scrollMode;
      fields.Add(leaveBehind);

      Field offscreenDeath = IntField("OFFSCREEN DEATH (s)", () => s.ScrollOffscreenDeathDelay, v => s.ScrollOffscreenDeathDelay = v, 1, 15);
      offscreenDeath.Visible = () => scrollMode() && s.ScrollLeaveBehind;
      fields.Add(offscreenDeath);

      fields.Add(BoolField("SAME SPAWN (RACE)", () => s.ScrollSameSpawn, v => s.ScrollSameSpawn = v));
      fields.Add(BoolField("DISABLE ARROWS", () => s.ScrollNoArrows, v => s.ScrollNoArrows = v));
      fields.Add(BoolField("DISABLE STOMP", () => s.ScrollNoStomp, v => s.ScrollNoStomp = v));
      fields.Add(BoolField("INTRO ZOOM", () => s.ScrollIntroZoom, v => s.ScrollIntroZoom = v));
    }

    private static Field BoolField(string label, Func<bool> get, Action<bool> set)
    {
      return new Field
      {
        Label = label,
        Value = () => get() ? "ON" : "OFF",
        Left = () => set(!get()),
        Right = () => set(!get())
      };
    }

    private static Field IntField(string label, Func<int> get, Action<int> set, int min, int max)
    {
      return new Field
      {
        Label = label,
        Value = () => get().ToString(),
        Left = () => set(Math.Max(min, get() - 1)),
        Right = () => set(Math.Min(max, get() + 1))
      };
    }

    public override void Added()
    {
      base.Added();
      Current = this;
      if (ownerButton != null)
        ownerButton.Selected = false;
      // Bloque les entrees du menu en arriere-plan tant que la popup est ouverte :
      // - MainMenu.Update ne traite plus MenuInput.Back (retour ecran precedent)
      // - VersusBeginButton.Update ne traite plus MenuInput.Start (lancement du match)
      // Meme pattern que le popup vanilla ClearAllData.
      MainMenu menu = Scene as MainMenu;
      if (menu != null)
        menu.CanAct = false;
      Sounds.ui_pause.Play(160f);
    }

    public override void Removed()
    {
      base.Removed();

      // Les reglages ne sont ecrits sur disque qu'en sortant du menu Options du
      // jeu : sans cet appel, une valeur changee ici serait perdue en quittant.
      TFModFortRiseScrollModule.SaveSettingsNow();

      if (Current == this)
        Current = null;
      Sounds.ui_unpause.Play(160f);
      MenuInput.Clear();
      MainMenu menu = Scene as MainMenu;
      if (menu != null)
        menu.CanAct = true;
      if (ownerButton != null)
        ownerButton.Selected = true;
    }

    // Champs actuellement visibles (les options sans effet dans le mode camera
    // courant sont cachees). `selected` indexe cette liste : les champs caches
    // sont donc sautes naturellement par la navigation.
    private List<Field> VisibleFields()
    {
      List<Field> vis = new List<Field>();
      for (int i = 0; i < fields.Count; i++)
        if (fields[i].Visible == null || fields[i].Visible())
          vis.Add(fields[i]);
      return vis;
    }

    public override void Update()
    {
      base.Update();
      MenuInput.Update();

      List<Field> vis = VisibleFields();
      if (selected >= vis.Count)
        selected = vis.Count - 1;

      if (MenuInput.Up && selected > 0)
      {
        selected--;
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }
      if (MenuInput.Down && selected < vis.Count - 1)
      {
        selected++;
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }
      if (MenuInput.Left)
      {
        vis[selected].Left();
        Sounds.ui_click.Play(160f, 1f);
        return;
      }
      if (MenuInput.Right)
      {
        vis[selected].Right();
        Sounds.ui_click.Play(160f, 1f);
        return;
      }
      if (MenuInput.Confirm || MenuInput.Back)
        RemoveSelf();
    }

    public override void Render()
    {
      Draw.Rect(0f, 0f, 320f, 240f, Color.Black * 0.8f);
      Draw.OutlineTextCentered(TFGame.Font, "SCROLL", Position + new Vector2(0f, -92f), Color.White, 2f);
      Draw.TextCentered(TFGame.Font, "UP/DOWN: CHAMP  LEFT/RIGHT: AJUSTER  CONFIRM: FERMER", Position + new Vector2(0f, -80f), Color.Gray);

      // Espacement 11px et depart remonte : jusqu'a 17 lignes visibles dans
      // l'ecran 240px.
      List<Field> vis = VisibleFields();
      float rowY = Position.Y - 68f;
      for (int i = 0; i < vis.Count; i++)
      {
        bool sel = i == selected;
        Color labelColor = sel ? Calc.HexToColor("F87858") : Color.White;
        Color valueColor = sel ? Calc.HexToColor("FFEC5E") : Color.Gray;
        string prefix = sel ? "> " : "  ";
        Draw.Text(TFGame.Font, prefix + vis[i].Label, new Vector2(Position.X - 100f, rowY), labelColor);
        Draw.Text(TFGame.Font, vis[i].Value(), new Vector2(Position.X + 50f, rowY), valueColor);
        rowY += 11f;
      }
    }
  }
}
