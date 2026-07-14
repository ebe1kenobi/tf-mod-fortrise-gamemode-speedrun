using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Popup de reglage du mode Loop Scroll, ouverte avec Y depuis le bouton de mode
  // (cf. MyLoopScrollModeButton). Edite directement les ModuleSettings.
  public class UILoopScrollPopup : Entity
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

    public static bool IsOpen { get; private set; }

    public UILoopScrollPopup(BorderButton ownerButton)
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
        Value = () => s.loopScrollShape == TFModFortRiseSpeedRunSettings.ShapeSquare ? "SQUARE" : "HORIZONTAL",
        Left = () => s.loopScrollShape = 1 - s.loopScrollShape,
        Right = () => s.loopScrollShape = 1 - s.loopScrollShape
      });
      fields.Add(IntField("SPEED", () => s.loopScrollSpeed, v => s.loopScrollSpeed = v, 1, 30));
      fields.Add(IntField("LEVELS", () => s.loopScrollMaxLevels, v => s.loopScrollMaxLevels = v, 2, 30));
      fields.Add(BoolField("FOLLOW LEADER", () => s.loopScrollFollowLeader, v => s.loopScrollFollowLeader = v));
      fields.Add(BoolField("LEAVE BEHIND", () => s.loopScrollLeaveBehind, v => s.loopScrollLeaveBehind = v));
      fields.Add(IntField("OFFSCREEN DEATH (s)", () => s.loopScrollOffscreenDeathDelay, v => s.loopScrollOffscreenDeathDelay = v, 1, 15));
      fields.Add(BoolField("SAME SPAWN (RACE)", () => s.loopScrollSameSpawn, v => s.loopScrollSameSpawn = v));
      fields.Add(BoolField("DISABLE ARROWS", () => s.loopScrollNoArrows, v => s.loopScrollNoArrows = v));
      fields.Add(BoolField("DISABLE STOMP", () => s.loopScrollNoStomp, v => s.loopScrollNoStomp = v));
      fields.Add(BoolField("INTRO ZOOM", () => s.loopScrollIntroZoom, v => s.loopScrollIntroZoom = v));
      fields.Add(BoolField("WIDE SCREEN", () => s.loopScrollWideScreen, v => s.loopScrollWideScreen = v));
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
      IsOpen = true;
      if (ownerButton != null)
        ownerButton.Selected = false;
      Sounds.ui_pause.Play(160f);
    }

    public override void Removed()
    {
      base.Removed();
      IsOpen = false;
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
      Draw.OutlineTextCentered(TFGame.Font, "LOOP SCROLL", Position + new Vector2(0f, -86f), Color.White, 2f);
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
