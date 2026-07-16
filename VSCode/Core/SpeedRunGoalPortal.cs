using System;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Portail d'arrivee ("trou noir") du mode Speed Run, copie visuelle du
  // portail de fin de niveau du mode coop Dark World (NextLevelPortal) mais
  // autonome (aucune dependance a DarkWorldControl/DarkWorldState).
  //
  // Apparait au bout du parcours (horizontal) ou apres N tours (square), cf.
  // SpeedRunRoundLogic.UpdateGoalPortal. Le premier joueur qui le touche
  // declenche OnEnter (une seule fois) : la logique de round s'occupe de la
  // victoire / des morts. Meme timing que l'original : 60 frames d'apparition
  // avant d'etre actif ("GO!").
  public class SpeedRunGoalPortal : LevelEntity
  {
    private static readonly Color[] FlashColors = new Color[]
    {
      Calc.HexToColor("EAC831"),
      Calc.HexToColor("FF2B35")
    };

    private Sprite<int> sprite;
    private SineWave sine;
    private int colorIndex;
    private Wiggler wiggler;
    private float hudPercent;
    private bool closed;

    // Appele avec le premier joueur entre dans le portail.
    public Action<Player> OnEnter;

    public SpeedRunGoalPortal(Vector2 position) : base(position)
    {
      base.Collider = new Hitbox(16f, 16f, -8f, -8f);
      this.Collidable = false;
      base.Tag(GameTags.LightSource);
      this.LightRadius = 80f;
      this.sprite = TFGame.SpriteData.GetSpriteInt("NextLevelPortal");
      this.sprite.Play(0, false);
      this.sprite.Color = Color.White * 0.5f;
      this.sprite.OnAnimationComplete = delegate(Sprite<int> s)
      {
        if (s.CurrentAnimID == 1)
          base.RemoveSelf();
      };
      base.Add(this.sprite);
      Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BackOut, 30, true);
      tween.OnUpdate = delegate(Tween t)
      {
        this.sprite.Scale = Vector2.Lerp(Vector2.Zero, Vector2.One * 0.8f, t.Eased);
      };
      base.Add(tween);
      Alarm.Set(this, 60, delegate
      {
        if (this.closed)
          return;
        this.Collidable = true;
        Sounds.sfx_darkWorldGo.Play(160f, 1f);
        this.wiggler.Start();
        this.sprite.Color = Color.White;
      }, Alarm.AlarmMode.Oneshot);
      base.Add(this.sine = new SineWave(60));
      base.Add(this.wiggler = Wiggler.Create(60, 4f, null, delegate(float v)
      {
        this.sprite.Scale = Vector2.One * (1f - 0.1f * v);
      }, false, false));
      Sounds.sfx_darkWorldPortalAppear.Play(160f, 1f);
    }

    // Animation de fermeture (apres la victoire) ; l'entite se retire toute
    // seule a la fin de l'animation.
    public void Close()
    {
      if (this.closed)
        return;
      this.closed = true;
      this.Collidable = false;
      this.sprite.Play(1, false);
      Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BackIn, 40, true);
      tween.OnUpdate = delegate(Tween t)
      {
        this.sprite.Scale = Vector2.Lerp(Vector2.One, Vector2.Zero, t.Eased);
      };
      base.Add(tween);
      Sounds.sfx_darkWorldPortalEnter03.Play(160f, 1f);
    }

    public override void Update()
    {
      base.Update();
      if (base.Level.OnInterval(5))
        this.colorIndex = (this.colorIndex + 1) % SpeedRunGoalPortal.FlashColors.Length;

      if (this.Collidable)
      {
        foreach (Entity entity in base.Level.Players)
        {
          Player player = entity as Player;
          if (player != null && !player.Dead && base.CollideCheck(player))
          {
            this.wiggler.Start();
            base.Level.ParticlesFG.Emit(Particles.NextLevelPortalEnter, 20, player.Position, new Vector2(5f, 8f));
            Sounds.sfx_darkWorldPortalEnter01.Play(160f, 1f);
            if (this.OnEnter != null)
              this.OnEnter(player);
            break;
          }
        }
      }

      if (this.Collidable && base.Level.Players.Count > 0)
      {
        this.hudPercent = Calc.Approach(this.hudPercent, 1f, 0.1f * Engine.TimeMult);
        return;
      }
      this.hudPercent = Calc.Approach(this.hudPercent, 0f, 0.2f * Engine.TimeMult);
    }

    // "GO!" au-dessus du portail, dessine en espace monde (suit la camera).
    public override void Render()
    {
      base.Render();
      if (this.hudPercent > 0f)
      {
        float bob = this.sine.Value * 4f;
        Color color = SpeedRunGoalPortal.FlashColors[this.colorIndex];
        Draw.OutlineTextJustify(TFGame.Font, "GO!", this.Position + new Vector2(1f, -25f + bob), color, Color.Black, new Vector2(0.5f, 1f), new Vector2(2f * this.hudPercent, 2f));
      }
    }
  }
}
