using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Popup de reglage du mode Loop Scroll, ouverte avec Y depuis le bouton de mode
  // (cf. MySpeedRunModeButton). Edite directement les ModuleSettings.
  public class UISpeedRunPopup : Entity
  {
    private class Field
    {
      public string Label;
      public Func<string> Value;
      public Action Left;
      public Action Right;
    }

    private readonly BorderButton ownerButton;
    private readonly List<Field> fields = new List<Field>();
    private int selected;

    // Instance courante de la popup (null si aucune).
    public static UISpeedRunPopup Current;

    // Ouverte seulement si l'instance courante appartient a la scene ACTIVE.
    // Auto-guerison : si la scene a ete remplacee alors que la popup etait ouverte
    // (ex: on lance le match en appuyant sur Start pendant que la popup est la),
    // son Removed() n'est jamais appele ; mais sa Scene ne correspond plus a la
    // scene courante -> elle ne compte plus comme ouverte, et le hint/Y reviennent.
    public static bool IsOpen => Current != null && Current.Scene == Engine.Instance.Scene;

    public UISpeedRunPopup(BorderButton ownerButton)
    {
      this.ownerButton = ownerButton;
      Position = new Vector2(160f, 120f);
      BuildFields();
    }

    private void BuildFields()
    {
      TFModFortRiseSpeedRunSettings s = TFModFortRiseSpeedRunModule.Settings;

      fields.Add(new Field
      {
        Label = "SHAPE",
        Value = () => s.SpeedRunShape == TFModFortRiseSpeedRunSettings.ShapeSquare ? "SQUARE" : "HORIZONTAL",
        Left = () => s.SpeedRunShape = 1 - s.SpeedRunShape,
        Right = () => s.SpeedRunShape = 1 - s.SpeedRunShape
      });
      fields.Add(IntField("SPEED", () => s.SpeedRunSpeed, v => s.SpeedRunSpeed = v, 1, 30));
      fields.Add(IntField("LEVELS", () => s.SpeedRunMaxLevels, v => s.SpeedRunMaxLevels = v, 2, 30));
      fields.Add(BoolField("FOLLOW LEADER", () => s.SpeedRunFollowLeader, v => s.SpeedRunFollowLeader = v));
      fields.Add(BoolField("LEAVE BEHIND", () => s.SpeedRunLeaveBehind, v => s.SpeedRunLeaveBehind = v));
      fields.Add(IntField("OFFSCREEN DEATH (s)", () => s.SpeedRunOffscreenDeathDelay, v => s.SpeedRunOffscreenDeathDelay = v, 1, 15));
      fields.Add(BoolField("SAME SPAWN (RACE)", () => s.SpeedRunSameSpawn, v => s.SpeedRunSameSpawn = v));
      fields.Add(BoolField("DISABLE ARROWS", () => s.SpeedRunNoArrows, v => s.SpeedRunNoArrows = v));
      fields.Add(BoolField("DISABLE STOMP", () => s.SpeedRunNoStomp, v => s.SpeedRunNoStomp = v));
      fields.Add(BoolField("INTRO ZOOM", () => s.SpeedRunIntroZoom, v => s.SpeedRunIntroZoom = v));
      fields.Add(BoolField("WIDE SCREEN", () => s.SpeedRunWideScreen, v => s.SpeedRunWideScreen = v));
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
      Sounds.ui_pause.Play(160f);
    }

    public override void Removed()
    {
      base.Removed();
      if (Current == this)
        Current = null;
      Sounds.ui_unpause.Play(160f);
      MenuInput.Clear();
      if (ownerButton != null)
        ownerButton.Selected = true;
    }

    public override void Update()
    {
      base.Update();
      MenuInput.Update();

      if (MenuInput.Up && selected > 0)
      {
        selected--;
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }
      if (MenuInput.Down && selected < fields.Count - 1)
      {
        selected++;
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }
      if (MenuInput.Left)
      {
        fields[selected].Left();
        Sounds.ui_click.Play(160f, 1f);
        return;
      }
      if (MenuInput.Right)
      {
        fields[selected].Right();
        Sounds.ui_click.Play(160f, 1f);
        return;
      }
      if (MenuInput.Confirm || MenuInput.Back)
        RemoveSelf();
    }

    public override void Render()
    {
      Draw.Rect(0f, 0f, 320f, 240f, Color.Black * 0.8f);
      Draw.OutlineTextCentered(TFGame.Font, "SPEED RUN", Position + new Vector2(0f, -86f), Color.White, 2f);
      Draw.TextCentered(TFGame.Font, "UP/DOWN: CHAMP   LEFT/RIGHT: AJUSTER", Position + new Vector2(0f, -72f), Color.Gray);

      float rowY = Position.Y - 56f;
      for (int i = 0; i < fields.Count; i++)
      {
        bool sel = i == selected;
        Color labelColor = sel ? Calc.HexToColor("F87858") : Color.White;
        Color valueColor = sel ? Calc.HexToColor("FFEC5E") : Color.Gray;
        string prefix = sel ? "> " : "  ";
        Draw.Text(TFGame.Font, prefix + fields[i].Label, new Vector2(Position.X - 100f, rowY), labelColor);
        Draw.Text(TFGame.Font, fields[i].Value(), new Vector2(Position.X + 50f, rowY), valueColor);
        rowY += 13f;
      }

      Draw.TextCentered(TFGame.Font, "CONFIRMER / RETOUR: FERMER", Position + new Vector2(0f, 92f), Color.Gray);
    }
  }
}
